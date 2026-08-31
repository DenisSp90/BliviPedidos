namespace BliviPedidos.Models.ViewModels;

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

    public int QuantidadeProdutosAtivos { get; set; }
    public int QuantidadeProdutosEstoqueCritico { get; set; }
    public decimal CustoTotalEstoque { get; set; }
    public decimal ValorVendaPotencialEstoque { get; set; }
    public decimal MargemPotencialEstoque => ValorVendaPotencialEstoque - CustoTotalEstoque;
    public List<ProdutoEstoqueCriticoViewModel> ProdutosEstoqueCritico { get; set; } = [];
    public List<DashboardGraficoItemViewModel> ProdutosMaisVendidos { get; set; } = [];
    public List<DashboardGraficoItemViewModel> EstoquePorCategoria { get; set; } = [];

    public int FiltroRegistros { get; set; }
    public string? BuscaPedido { get; set; }
    public string PixKey { get; set; }
    public string PixQRCodeUrl { get; set; }

    public IEnumerable<ProdutoMovimentacao> Movimentacoes { get; set; } = new List<ProdutoMovimentacao>();

    //public ControleInternoWeb.Areas.Identity.Pages.Account.Manage IndexModel { get; set; }   
}

public sealed class ProdutoEstoqueCriticoViewModel
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public int Quantidade { get; init; }
}

public sealed class DashboardGraficoItemViewModel
{
    public string Rotulo { get; init; } = string.Empty;
    public decimal Valor { get; init; }
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

public class PedidoViewModel
{
    public int Id { get; set; }
    public string NomeCliente { get; set; }
    public string CelularCliente { get; set; }
    public string EmailCliente { get; set; }
    public List<ItemPedidoViewModel> Itens { get; set; }
    public decimal ValorTotalPedido { get; set; }
    public StatusPagamento StatusPagamento { get; set; }
    public DateTime? DataPedido { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? EmailResponsavel { get; set; } = string.Empty;
}

public class ItemPedidoViewModel
{
    public string NomeProduto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
}
