using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations
{
    public class PedidoService : BaseService<Pedido>, IPedidoService
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IItemPedidoService _itemPedidoService;
        private readonly ICadastroService _cadastroService;
        private readonly IProdutoService _produtoService;

        public PedidoService(IHttpContextAccessor contextAccessor,
            ApplicationDbContext context,
            IItemPedidoService itemPedidoService,
            ICadastroService cadastroService,
            IProdutoService produtoService) : base(context)
        {
            this.contextAccessor = contextAccessor;
            _context = context;
            _itemPedidoService = itemPedidoService;
            _cadastroService = cadastroService;
            _produtoService = produtoService;
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
                    .Where(p => p.Ativo == true)
                    .ToList();
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

            return pedido;
        }

        public async Task<Pedido> GetPedidoByIdAsync(int id)
        {
            return await dbSet
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.Cadastro)
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();
        }

        public async Task RegistrarCancelamentoPedido(int pedidoId)
        {
            var pedido = await GetPedidoByIdAsync(pedidoId);


            if (pedido == null)
            {
                throw new Exception("Pedido não encontrado");
            }

            pedido.ValorTotalPedido = 0;
            pedido.Pago = false;
            pedido.Ativo = false;

            foreach (var item in pedido.Itens)
            {
                var produto = item.Produto;

                if (produto != null)
                {
                    produto.Quantidade += item.Quantidade;
                    _context.Entry(produto).State = EntityState.Modified;
                }
            }
            _context.Entry(pedido).State = EntityState.Modified;
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
}
