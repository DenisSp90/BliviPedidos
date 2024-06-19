using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;

namespace BliviPedidos.Services.Implementations
{
    public class ItemPedidoService : BaseService<ItemPedido>, IItemPedidoService
    {
        public ItemPedidoService(ApplicationDbContext context) : base(context)
        {
        }

        public List<ItemPedido> GetAllItensPedidos()
        {
            return dbSet.ToList();
        }

        public ItemPedido GetItemPedido(int itemPedidoId)
        {
            return
                dbSet
                    .Where(ip => ip.Id == itemPedidoId)
                    .SingleOrDefault();
        }

        public void RemoveItemPedido(int itemPedidoId)
        {
            dbSet.Remove(GetItemPedido(itemPedidoId));
        }
    }
}
