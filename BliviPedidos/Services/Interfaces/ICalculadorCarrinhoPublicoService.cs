using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Services.Interfaces;

public interface ICalculadorCarrinhoPublicoService
{
    Task<ResultadoValidacaoCarrinhoPublico> ValidarERecalcularAsync(
        CarrinhoPublico carrinho,
        CancellationToken cancellationToken = default);
}
