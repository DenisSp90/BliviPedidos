using System.Data;
using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BliviPedidos.Services.Implementations;

public sealed class ConfirmacaoCheckoutService : IConfirmacaoCheckoutService
{
    private readonly ApplicationDbContext _context;
    private readonly ICarrinhoPublicoService _carrinhoService;
    private readonly IDadosConsumidorCheckoutService _dadosService;
    private readonly ILogger<ConfirmacaoCheckoutService> _logger;
    private readonly ReservaEstoqueOptions _reservaOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguracaoReservaLojaService? _configuracaoReservaLojaService;
    private readonly INotificacaoPedidoService? _notificacaoPedido;

    public ConfirmacaoCheckoutService(
        ApplicationDbContext context,
        ICarrinhoPublicoService carrinhoService,
        IDadosConsumidorCheckoutService dadosService,
        ILogger<ConfirmacaoCheckoutService> logger,
        IOptions<ReservaEstoqueOptions> reservaOptions,
        IHttpContextAccessor httpContextAccessor,
        IConfiguracaoReservaLojaService? configuracaoReservaLojaService = null,
        INotificacaoPedidoService? notificacaoPedido = null)
    {
        _context = context;
        _carrinhoService = carrinhoService;
        _dadosService = dadosService;
        _logger = logger;
        _reservaOptions = reservaOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _configuracaoReservaLojaService = configuracaoReservaLojaService;
        _notificacaoPedido = notificacaoPedido;
    }

    public async Task<ResultadoConfirmacaoCheckout> ConfirmarAsync(
        int lojaId,
        CancellationToken cancellationToken = default)
    {
        var carrinho = _carrinhoService.Obter(lojaId);
        var dados = _dadosService.Obter(lojaId);
        var consumidorUsuarioId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(consumidorUsuarioId))
            return ResultadoConfirmacaoCheckout.Falhou("Entre em sua conta para confirmar o pedido.");
        if (carrinho.Itens.Count == 0)
            return ResultadoConfirmacaoCheckout.Falhou("O carrinho está vazio.");
        if (dados == null)
            return ResultadoConfirmacaoCheckout.Falhou("Informe os dados do consumidor antes de confirmar.");

        var grupos = carrinho.Itens.GroupBy(item => item.ProdutoId).ToArray();
        if (grupos.Any(grupo => grupo.Key <= 0 || grupo.Count() != 1 || grupo.Single().Quantidade <= 0))
            return ResultadoConfirmacaoCheckout.Falhou("O carrinho contém itens inválidos.");

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var quantidades = grupos.ToDictionary(grupo => grupo.Key, grupo => grupo.Single().Quantidade);
            var produtosIds = quantidades.Keys.ToArray();
            var produtos = await _context.Produto
                .AsNoTracking()
                .Where(produto => produto.LojaId == lojaId && produtosIds.Contains(produto.Id))
                .ToListAsync(cancellationToken);

            if (produtos.Count != produtosIds.Length)
                return await ReverterAsync(transaction, "Um ou mais produtos não existem nesta loja.", cancellationToken);

            foreach (var produto in produtos)
            {
                if (!produto.IsAtivo)
                    return await ReverterAsync(transaction, $"O produto “{produto.Nome}” não está disponível.", cancellationToken);
                if (quantidades[produto.Id] > produto.Quantidade)
                    return await ReverterAsync(transaction, $"Não há estoque suficiente de “{produto.Nome}”.", cancellationToken);
            }

            var cliente = await _context.Cliente
                .SingleOrDefaultAsync(item => item.LojaId == lojaId && item.Telefone == dados.Telefone, cancellationToken);
            if (cliente == null)
            {
                cliente = new Cliente { LojaId = lojaId };
                _context.Cliente.Add(cliente);
            }
            AtualizarCliente(cliente, dados);

            var expiracaoMinutos = _configuracaoReservaLojaService is null
                ? Math.Max(1, _reservaOptions.ExpiracaoMinutos)
                : await _configuracaoReservaLojaService.ObterExpiracaoMinutosAsync(lojaId, cancellationToken);
            var cadastro = CriarCadastro(dados, cliente);
            var pedido = new Pedido(cadastro)
            {
                LojaId = lojaId,
                ConsumidorUsuarioId = consumidorUsuarioId,
                Status = StatusPedido.Confirmado,
                StatusPagamento = StatusPagamento.AguardandoPagamento,
                DataPedido = DateTime.UtcNow,
                ReservaExpiraEm = DateTime.UtcNow.AddMinutes(expiracaoMinutos),
                EmailResponsavel = dados.Email,
                CodigoPublico = CodigoPublicoPedido.Gerar()
            };

            foreach (var produto in produtos)
            {
                var quantidade = quantidades[produto.Id];
                var linhasAfetadas = await _context.Produto
                    .Where(item => item.Id == produto.Id
                        && item.LojaId == lojaId
                        && item.IsAtivo
                        && item.Quantidade >= quantidade)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            item => item.Quantidade,
                            item => item.Quantidade - quantidade),
                        cancellationToken);

                if (linhasAfetadas != 1)
                    return await ReverterAsync(
                        transaction,
                        $"Não há estoque suficiente de “{produto.Nome}”.",
                        cancellationToken);

                pedido.Itens.Add(new ItemPedido(pedido, produto.Id, quantidade, produto.PrecoVenda));
            }
            pedido.ValorTotalPedido = pedido.Itens.Sum(item => item.Subtotal);
            _context.Pedido.Add(pedido);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var item in pedido.Itens)
            {
                _context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
                {
                    ProdutoId = item.ProdutoId,
                    LojaId = lojaId,
                    PedidoId = pedido.Id,
                    Quantidade = item.Quantidade,
                    Tipo = "Saída",
                    Ator = dados.Email ?? "CONSUMIDOR",
                    Origem = OrigemMovimentacaoEstoque.CheckoutPublico,
                    Observacao = $"[SAIDA] | [CHECKOUT-PUBLICO] | PEDIDO: [{pedido.Id}]",
                    Data = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (_notificacaoPedido is not null)
                await _notificacaoPedido.NotificarPedidoCriadoAsync(pedido.Id, cancellationToken);

            _carrinhoService.Limpar(lojaId);
            _dadosService.Limpar(lojaId);
            return ResultadoConfirmacaoCheckout.Confirmado(pedido.Id, pedido.CodigoPublico!);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Falha ao confirmar checkout público. LojaId: {LojaId}", lojaId);
            return ResultadoConfirmacaoCheckout.Falhou("Não foi possível confirmar o pedido. Tente novamente.");
        }
    }

    private static async Task<ResultadoConfirmacaoCheckout> ReverterAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        string erro,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return ResultadoConfirmacaoCheckout.Falhou(erro);
    }

    private static void AtualizarCliente(Cliente cliente, DadosConsumidorCheckout dados)
    {
        cliente.Nome = dados.Nome;
        cliente.Telefone = dados.Telefone;
        cliente.Email = dados.Email;
        cliente.CEP = dados.CEP;
        cliente.Endereco = dados.Endereco;
        cliente.Complemento = dados.Complemento;
        cliente.Bairro = dados.Bairro;
        cliente.Municipio = dados.Municipio;
        cliente.UF = dados.UF;
    }

    private static Cadastro CriarCadastro(DadosConsumidorCheckout dados, Cliente cliente) => new()
    {
        Cliente = cliente,
        Nome = dados.Nome,
        Telefone = dados.Telefone,
        Email = dados.Email,
        CEP = dados.CEP,
        Endereco = dados.Endereco,
        Complemento = dados.Complemento,
        Bairro = dados.Bairro,
        Municipio = dados.Municipio,
        UF = dados.UF
    };
}
