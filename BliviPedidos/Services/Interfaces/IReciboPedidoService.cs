using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface IReciboPedidoService
{
    byte[] GerarWordA4(Pedido pedido, Loja loja);
}
