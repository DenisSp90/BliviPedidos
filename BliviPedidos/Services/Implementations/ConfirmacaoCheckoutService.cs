using System.Data;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class ConfirmacaoCheckoutService : IConfirmacaoCheckoutService
{
    private readonly ApplicationDbContext _context;
    private readonly ICarrinhoPublicoService _carrinhoService;
    private readonly IDadosConsumidorCheckoutService _dadosService;
    private readonly ILogger<ConfirmacaoCheckoutService> _logger;

    public ConfirmacaoCheckoutService(
        ApplicationDbContext context,
        ICarrinhoPublicoService carrinhoService,
        IDadosConsumidorCheckoutService dadosService,
        ILogger<ConfirmacaoCheckoutService> logger)
    {
        _context = context;
        _carrinhoService = carrinhoService;
        _dadosService = dadosService;
        _logger = logger;
    }

    public async Task<ResultadoConfirmacaoCheckout> ConfirmarAsync(
        int lojaId,
        CancellationToken cancellationToken = default)
    {
        var carrinho = _carrinhoService.Obter(lojaId);
        var dados = _dadosService.Obter(lojaId);
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

            var cadastro = CriarCadastro(dados, cliente);
            var pedido = new Pedido(cadastro)
            {
                LojaId = lojaId,
                Ativo = true,
                Pago = false,
                DataPedido = DateTime.UtcNow,
                EmailResponsavel = dados.Email,
                CodigoPublico = CodigoPublicoPedido.Gerar()
            };

            foreach (var produto in produtos)
            {
                var quantidade = quantidades[produto.Id];
                pedido.Itens.Add(new ItemPedido(pedido, produto, quantidade, produto.PrecoVenda));
                produto.Quantidade -= quantidade;
            }
            pedido.ValorTotalPedido = pedido.Itens.Sum(item => item.Subtotal);
            _context.Pedido.Add(pedido);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var item in pedido.Itens)
            {
                _context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    Tipo = "Saída",
                    Observacao = $"[SAIDA] | [CHECKOUT-PUBLICO] | PEDIDO: [{pedido.Id}]",
                    Data = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
