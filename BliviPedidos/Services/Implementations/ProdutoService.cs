using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations
{
    public class ProdutoService : BaseService<ItemPedido>, IProdutoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProdutoService(ApplicationDbContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<Produto>> GetProdutosAsync()
        {
            return await _context.Produto.ToListAsync();
        }

        public async Task<ProdutoViewModel> ProcurarProdutoAsync(int id)
        {
            var produto = await _context.Produto.FindAsync(id);

            if (produto == null)
            {
                return null;
            }

            return _mapper.Map<ProdutoViewModel>(produto);
        }

        public async Task<bool> RegistrarProdutoAsync(Produto produto)
        {
            try
            {
                if (produto.Id > 0)
                    _context.Produto.Update(produto);
                else
                    _context.Produto.Add(produto);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public bool UpdateQuantidade(List<ItemPedido> itens)
        {
            try
            {
                foreach (var item in itens)
                {
                    Produto produto = _context.Produto.FirstOrDefault(p => p.Id == item.Produto.Id);

                    if (produto != null)
                    {
                        if (item.Quantidade > produto.Quantidade)
                        {
                            throw new InvalidOperationException($"A quantidade de '{produto.Nome}' disponível em estoque é insuficiente.");
                        }

                        produto.Quantidade -= item.Quantidade;

                        _context.SaveChanges();
                    }
                    else
                    {
                        throw new InvalidOperationException("Produto não encontrado.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VerificarProdutoVinculadoPedido(int produtoId)
        {
            return dbSet.Any(item => item.Produto.Id == produtoId);
        }
    }
}
