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


    public PedidoService(IHttpContextAccessor contextAccessor,
        ApplicationDbContext context,
        IItemPedidoService itemPedidoService,
        ICadastroService cadastroService,
        IProdutoService produtoService,
        IHttpContextAccessor httpContextAccessor) : base(context)
    {
        this.contextAccessor = contextAccessor;
        _context = context;
        _itemPedidoService = itemPedidoService;
        _cadastroService = cadastroService;
        _produtoService = produtoService;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task AtualizarStatusPagamentoAsync(int pedidoId, bool novoStatusPagamento)
    {
        var pedido = await GetPedidoByIdAsync(pedidoId);

        if (pedido != null)
        {
            pedido.DataPagamento = DateTime.Now;
            pedido.Pago = novoStatusPagamento;
            await _context.SaveChangesAsync();
        }
        else
        {
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
                .OrderByDescending(p => p.Ativo)
                .ThenBy(p => p.Id)
                .ToList();
    }

    public IList<Pedido> GetListaPedidosAtivos()
    {
        return dbSet.Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
                .Include(c => c.Cadastro.Cliente)
                .Where(p => p.Ativo == true)
                .ToList();
    }

    public async Task<List<Pedido>> GetListaPedidosAtivosAsync()
    {
        return await dbSet.Include(p => p.Itens)
                          .ThenInclude(i => i.Produto)
                          .Include(p => p.Cadastro)
                          .ThenInclude(c => c.Cliente)
                          .Where(p => p.Ativo == true)
                          .ToListAsync();
    }

    public IList<Pedido> GetListaPedidosAtivosByEmail(string email)
    {
        return dbSet.Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .Include(p => p.Cadastro)
            .Where(p => p.Ativo && p.EmailResponsavel == contextAccessor.HttpContext.User.Identity.Name)
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

    public async Task RegistrarCancelamentoPedido(int pedidoId)
    {
        var usuario = _httpContextAccessor.HttpContext.User.Identity.Name;

        // Iniciando a transação
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var pedido = await GetPedidoByIdAsync(pedidoId);

                if (pedido == null)
                    throw new Exception("Pedido não encontrado");

                pedido.ValorTotalPedido = 0;
                pedido.Pago = false;
                pedido.Ativo = false;

                foreach (var item in pedido.Itens)
                {
                    var produto = item.Produto;

                    if (produto != null)
                    {
                        // Atualizar a quantidade do produto no estoque
                        produto.Quantidade += item.Quantidade;

                        // Criar a movimentação de entrada para o cancelamento
                        var movimentacao = new ProdutoMovimentacao
                        {
                            ProdutoId = produto.Id,
                            Quantidade = item.Quantidade,
                            Tipo = "Entrada",
                            Observacao = $"[ENTRADA] | [PEDIDO-CANCELAMENTO] | [{usuario.ToUpper()}] | PEDIDO: [{pedido.Id}]",
                            Data = DateTime.Now
                        };

                        // Registrar a movimentação
                        await _produtoService.RegistrarMovimentacaoAsync(movimentacao);

                        // Atualizar o estado do produto no contexto
                        _context.Entry(produto).State = EntityState.Modified;
                    }
                }

                // Atualizar o estado do pedido no contexto
                _context.Entry(pedido).State = EntityState.Modified;

                // Salvar as alterações no banco de dados
                await _context.SaveChangesAsync();

                // Confirmar a transação
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // Reverter a transação em caso de erro
                await transaction.RollbackAsync();
                throw;
            }
        }
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
