using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public class PedidoService : BaseService<Pedido>, IPedidoService
{
    private readonly IHttpContextAccessor contextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly IItemPedidoService _itemPedidoService;
    private readonly ICadastroService _cadastroService;
    private readonly IProdutoService _produtoService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PedidoService> _logger;


    public PedidoService(IHttpContextAccessor contextAccessor,
        ApplicationDbContext context,
        IItemPedidoService itemPedidoService,
        ICadastroService cadastroService,
        IProdutoService produtoService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PedidoService> logger) : base(context)
    {
        this.contextAccessor = contextAccessor;
        _context = context;
        _itemPedidoService = itemPedidoService;
        _cadastroService = cadastroService;
        _produtoService = produtoService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public void AddItem(int id)
    {
        var produto = _context.Set<Produto>()
                        .Where(p => p.Id == id)
                        .SingleOrDefault();

        if (produto == null)
            throw new ArgumentException("Produto não encontrado");

        var pedido = GetPedido();

        var itemPedido = _context.Set<ItemPedido>()
                            .Where(i => i.Produto.Id == id
                                    && i.Pedido.Id == pedido.Id)
                            .SingleOrDefault();

        if (itemPedido == null)
        {
            itemPedido = new ItemPedido(pedido, produto, 1, produto.PrecoVenda);
            _context.Set<ItemPedido>()
                .Add(itemPedido);

            _context.SaveChanges();
        }
    }

    public async Task AtualizarStatusPagamentoAsync(int pedidoId, StatusPagamento novoStatusPagamento)
    {
        if (!Enum.IsDefined(novoStatusPagamento))
            throw new ArgumentOutOfRangeException(
                nameof(novoStatusPagamento), "Situação do pagamento inválida.");

        var pedido = await GetPedidoByIdAsync(pedidoId);

        if (pedido != null)
        {
            if (pedido.Status == StatusPedido.Cancelado
                && novoStatusPagamento != StatusPagamento.Cancelado)
                throw new InvalidOperationException(
                    "O pagamento de um pedido cancelado não pode ser alterado.");

            pedido.StatusPagamento = novoStatusPagamento;
            pedido.DataPagamento = novoStatusPagamento == StatusPagamento.Pago
                ? DateTime.Now
                : null;
            if (novoStatusPagamento == StatusPagamento.Pago)
                pedido.ReservaExpiraEm = null;
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning(
                "Tentativa de atualizar pagamento de pedido inexistente. PedidoId: {PedidoId}",
                pedidoId);
            throw new Exception("Pedido não encontrado.");
        }
    }

    public void ClearPedido()
    {
        contextAccessor.HttpContext.Session.SetInt32("pedidoId", 0);
    }

    public IList<Pedido> GetListaPedidos()
    {
        return dbSet.Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
                .OrderBy(p => p.Status == StatusPedido.Cancelado || p.Status == StatusPedido.Concluido)
                .ThenBy(p => p.Status)
                .ThenBy(p => p.Id)
                .ToList();
    }

    public IList<Pedido> GetListaPedidosRegistrados()
    {
        return ConsultaPedidosRegistrados()
            .OrderByDescending(p => p.DataPedido)
            .ThenByDescending(p => p.Id)
            .ToList();
    }

    public IList<Pedido> GetListaPedidosRegistradosByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new List<Pedido>();

        return ConsultaPedidosRegistrados()
            .Where(p => p.EmailResponsavel == email)
            .OrderByDescending(p => p.DataPedido)
            .ThenByDescending(p => p.Id)
            .ToList();
    }

    private IQueryable<Pedido> ConsultaPedidosRegistrados()
    {
        return dbSet
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .Include(p => p.Cadastro)
                .ThenInclude(c => c.Cliente)
            .Where(p => p.Status != StatusPedido.Carrinho);
    }

    public IList<Pedido> GetListaPedidosAtivos()
    {
        return dbSet.Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
                .Include(c => c.Cadastro.Cliente)
                .Where(p => p.Status != StatusPedido.Carrinho &&
                            p.Status != StatusPedido.Concluido &&
                            p.Status != StatusPedido.Cancelado)
                .ToList();
    }

    public async Task<List<Pedido>> GetListaPedidosAtivosAsync()
    {
        return await dbSet.Include(p => p.Itens)
                          .ThenInclude(i => i.Produto)
                          .Include(p => p.Cadastro)
                          .ThenInclude(c => c.Cliente)
                          .Where(p => p.Status != StatusPedido.Carrinho &&
                                      p.Status != StatusPedido.Concluido &&
                                      p.Status != StatusPedido.Cancelado)
                          .ToListAsync();
    }

    public Task<int> ContarPedidosPendentesAsync(CancellationToken cancellationToken = default)
    {
        return dbSet.CountAsync(pedido =>
            pedido.Status != StatusPedido.Carrinho &&
            pedido.Status != StatusPedido.Concluido &&
            pedido.Status != StatusPedido.Cancelado,
            cancellationToken);
    }

    public IList<Pedido> GetListaPedidosAtivosByEmail(string email)
    {
        return dbSet.Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .Include(p => p.Cadastro)
            .Where(p => p.Status != StatusPedido.Carrinho &&
                        p.Status != StatusPedido.Concluido &&
                        p.Status != StatusPedido.Cancelado &&
                        p.EmailResponsavel == contextAccessor.HttpContext.User.Identity.Name)
            .ToList();
    }

    public Pedido GetPedido()
    {
        var pedidoId = GetPedidoId();
        var pedido = dbSet
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
            .Where(p => p.Id == pedidoId)
            .SingleOrDefault();

        if (pedido == null)
        {
            pedido = new Pedido();
            dbSet.Add(pedido);
            _context.SaveChanges();
            SetPedidoId(pedido.Id);
        }

        return pedido;
    }

    public Pedido GetPedidoById(int id)
    {
        var pedido = dbSet
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
            .Where(p => p.Id == id)
            .SingleOrDefault();

        pedido.ValorTotalPedido = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        dbSet.Update(pedido);

        _context.SaveChangesAsync();


        return pedido;
    }

    public async Task<Pedido> GetPedidoByIdAsync(int id)
    {
        var pedido = await dbSet
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .Include(p => p.Cadastro)
                .ThenInclude(c => c.Cliente)  // Inclui o cliente vinculado ao cadastro
            .FirstOrDefaultAsync(p => p.Id == id);


        if (pedido == null)
            return null;

        pedido.ValorTotalPedido = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        dbSet.Update(pedido);
        await _context.SaveChangesAsync();

        return pedido;
    }

    public async Task RegistrarCancelamentoPedido(int pedidoId, string? origem = null, string? ator = null)
    {
        var usuario = ator ?? _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "SISTEMA";
        origem ??= OrigemMovimentacaoEstoque.CancelamentoPedido;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var pedido = await _context.Pedido
                .AsNoTracking()
                .Include(item => item.Itens)
                .SingleOrDefaultAsync(item => item.Id == pedidoId);

            if (pedido == null)
                throw new Exception("Pedido não encontrado");

            if (pedido.Status == StatusPedido.Cancelado)
            {
                await transaction.RollbackAsync();
                return;
            }

            if (pedido.Status == StatusPedido.Concluido)
                throw new InvalidOperationException("Um pedido concluído não pode ser cancelado.");

            var cancelamentoAdquirido = await _context.Pedido
                .Where(item => item.Id == pedidoId
                    && item.Status != StatusPedido.Cancelado
                    && item.Status != StatusPedido.Concluido)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.ValorTotalPedido, 0m)
                    .SetProperty(item => item.StatusPagamento, StatusPagamento.Cancelado)
                    .SetProperty(item => item.Status, StatusPedido.Cancelado)
                    .SetProperty(item => item.ReservaExpiraEm, (DateTime?)null));

            if (cancelamentoAdquirido != 1)
            {
                await transaction.RollbackAsync();
                return;
            }

            foreach (var item in pedido.Itens)
            {
                var produtoRestaurado = await _context.Produto
                    .Where(produto => produto.Id == item.ProdutoId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        produto => produto.Quantidade,
                        produto => produto.Quantidade + item.Quantidade));

                if (produtoRestaurado != 1)
                    throw new InvalidOperationException(
                        $"O produto {item.ProdutoId} do pedido não foi encontrado para restaurar o estoque.");

                _context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
                {
                    ProdutoId = item.ProdutoId,
                    LojaId = pedido.LojaId,
                    PedidoId = pedido.Id,
                    Quantidade = item.Quantidade,
                    Tipo = "Entrada",
                    Ator = usuario,
                    Origem = origem,
                    Observacao = $"[ENTRADA] | [PEDIDO-CANCELAMENTO] | [{usuario.ToUpperInvariant()}] | PEDIDO: [{pedido.Id}]",
                    Data = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao cancelar pedido e restaurar estoque. PedidoId: {PedidoId}",
                pedidoId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AtualizarStatusPedidoAsync(int pedidoId, StatusPedido novoStatus)
    {
        if (!Enum.IsDefined(novoStatus))
            throw new ArgumentOutOfRangeException(nameof(novoStatus), "Situação do pedido inválida.");

        var pedido = await GetPedidoByIdAsync(pedidoId)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (pedido.Status == StatusPedido.Cancelado)
            throw new InvalidOperationException("Um pedido cancelado não pode ser reaberto.");

        if (novoStatus == StatusPedido.Carrinho)
            throw new InvalidOperationException("Um pedido confirmado não pode voltar para o carrinho.");

        if (novoStatus == StatusPedido.Cancelado)
        {
            await RegistrarCancelamentoPedido(pedidoId);
            return;
        }

        pedido.Status = novoStatus;
        await _context.SaveChangesAsync();
    }

    public Pedido UpdateCadastro(Cadastro cadastro)
    {
        var pedido = GetPedido();
        pedido.EmailResponsavel = contextAccessor.HttpContext.User.Identity.Name;
        _cadastroService.Update(pedido.Cadastro.Id, cadastro);
        return pedido;
    }

    public UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido)
    {
        var itemPedidoDB =
            _itemPedidoService
            .GetItemPedido(itemPedido.Id);

        if (itemPedidoDB != null)
        {
            itemPedidoDB.AtualizaQuantidade(itemPedido.Quantidade);

            if (itemPedido.Quantidade == 0)
                _itemPedidoService.RemoveItemPedido(itemPedido.Id);



            _context.SaveChanges();

            var carrinhoViewModel = new CarrinhoViewModel(GetPedido().Itens);

            return new UpdateQuantidadeResponse(itemPedidoDB, carrinhoViewModel);
        }

        throw new ArgumentException("ItemPedido não encontrado");
    }

    private int? GetPedidoId()
    {
        return contextAccessor.HttpContext.Session.GetInt32("pedidoId");
    }

    private void SetPedidoId(int pedidoId)
    {
        contextAccessor.HttpContext.Session.SetInt32("pedidoId", pedidoId);
    }

}
