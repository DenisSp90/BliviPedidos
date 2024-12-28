using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Services.Interfaces;

public interface IProdutoService
{
    Task AtualizarImagemProdutoAsync(int produtoId, string nomeArquivoNovo);
    Task<List<Produto>> GetProdutosAsync();
    Task<List<Produto>> GetProdutosAtivosAsync();
    Task<List<Produto>> GetProdutosDesativadosAsync();

    Task<ProdutoViewModel> ProcurarProdutoAsync(int id);
    Task<bool> RegistrarProdutoAsync(Produto produto);
    bool VerificarProdutoVinculadoPedido(int produtoId);
    bool UpdateQuantidade(List<ItemPedido> itens);
    Task<bool> VerificarExistenciaProdutoNoBanco(string nome, decimal precoPago);
    List<ProdutoMovimentacao> GetMovimentacaoEstoque();

    Task<int> RegistrarMovimentacaoAsync(ProdutoMovimentacao movimentacao);
    Task RegistrarCancelamentoProdutos(ItemPedido itemPedido);
    Task<IEnumerable<Produto>> GetProdutosEmEstoqueByQuantidadeAsync(int quantidade);
}

