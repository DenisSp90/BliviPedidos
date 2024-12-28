using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface IRelatorioService
{
    byte[] GerarRelatorioProdutosEmEstoque(IEnumerable<Produto> produtos, string tituloRelatorio, string[] configuracoesRelatorio);
    byte[] GerarRelatorioProdutosComEstoqueBaixo(IEnumerable<Produto> produtos, string tituloRelatorio);
    byte[] GerarRelatorioMovimentacaoEstoque(IEnumerable<ProdutoMovimentacao> movimentacoes, string tituloRelatorio);
    byte[] GerarRelatorioProdutosParados(IEnumerable<Produto> produtos, string tituloRelatorio);
}