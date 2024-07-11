using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Services.Interfaces
{
    public interface IProdutoService
    {
        Task<List<Produto>> GetProdutosAsync();
        Task<ProdutoViewModel> ProcurarProdutoAsync(int id);
        Task<bool> RegistrarProdutoAsync(Produto produto);
        bool VerificarProdutoVinculadoPedido(int produtoId);
        bool UpdateQuantidade(List<ItemPedido> itens);
        Task<bool> VerificarExistenciaProdutoNoBanco(string nome, decimal precoPago);
        Task RegistrarCancelamentoProdutos(ItemPedido itemPedido);
    }
}
