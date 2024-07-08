namespace BliviPedidos.Models.ViewModels
{
    public class StoreViewModel
    {
        public StoreViewModel() { }

        public List<Produto>? Produtos { get; set; }
        public List<ClienteViewModel>? Clientes { get; set; }
        public CarrinhoViewModel? CarrinhoViewModel { get; set; }
        public Pedido? Pedido { get; set; }
        public IList<Pedido>? Pedidos { get; set; }

        public ClienteViewModel? Cliente { get; set; }

        public decimal TotalValorPedidos { get; set; }
        public decimal ValorPedidosPagos { get; set; }
        public decimal ValorPedidosNaoPagos { get; set; }

        public int FiltroRegistros { get; set; }

        //public ControleInternoWeb.Areas.Identity.Pages.Account.Manage IndexModel { get; set; }   
    }

    public class CarrinhoViewModel
    {
        public CarrinhoViewModel(IList<ItemPedido> itens)
        {
            Itens = itens;
        }

        public IList<ItemPedido> Itens { get; }

        public decimal Total => Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    }
}
