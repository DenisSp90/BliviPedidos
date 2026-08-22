using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface ICarrinhoPublicoService
{
    CarrinhoPublico Obter(int lojaId);
    CarrinhoPublico Adicionar(int lojaId, int produtoId, int quantidade = 1);
    CarrinhoPublico Remover(int lojaId, int produtoId);
    void Limpar(int lojaId);
}
