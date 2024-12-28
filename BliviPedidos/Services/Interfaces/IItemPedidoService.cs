using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces
{
    public interface IItemPedidoService
    {
        ItemPedido GetItemPedido(int itemPedidoId);
        void RemoveItemPedido(int itemPedidoId);
        List<ItemPedido> GetAllItensPedidos();
        Task UpdateItemPedidoAsync(int itemPedidoId, int produtoId, int quantidade, decimal preco);
    }
}
