using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface ILojaAtualService
{
    Task<Loja> ObterLojaAsync();
    Task<int> ObterLojaIdAsync();
}
