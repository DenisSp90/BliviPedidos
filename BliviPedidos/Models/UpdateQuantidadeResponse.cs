using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Models
{
    public class UpdateQuantidadeResponse
    {
        public UpdateQuantidadeResponse(ItemPedido itemPedido, CarrinhoViewModel carrinhoViewModel)
        {
            ItemPedido = itemPedido;
            CarrinhoViewModel = carrinhoViewModel;
        }

        public UpdateQuantidadeResponse(List<ItemPedido> itemPedidos, CarrinhoViewModel carrinhoViewModel)
        {
            ListaItens = itemPedidos;
            CarrinhoViewModel = carrinhoViewModel;
        }

        public List<ItemPedido> ListaItens { get; }
        public ItemPedido ItemPedido { get; }
        public CarrinhoViewModel CarrinhoViewModel { get; }
    }
}
