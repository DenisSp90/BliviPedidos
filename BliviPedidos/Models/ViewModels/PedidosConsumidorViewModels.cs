using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public sealed class MeusPedidosConsumidorViewModel
{
    public LojaPublicaDto Loja { get; set; } = null!;
    public IReadOnlyCollection<ResumoPedidoConsumidorViewModel> Pedidos { get; init; } = [];
}

public sealed class ResumoPedidoConsumidorViewModel
{
    public int PedidoId { get; init; }
    public string CodigoPublico { get; init; } = string.Empty;
    public DateTime? DataPedido { get; init; }
    public StatusPedido Status { get; init; }
    public StatusPagamento StatusPagamento { get; init; }
    public decimal Total { get; init; }
}

public sealed class PedidoConsumidorDetalheViewModel
{
    public LojaPublicaDto Loja { get; set; } = null!;
    public int PedidoId { get; init; }
    public string CodigoPublico { get; init; } = string.Empty;
    public DateTime? DataPedido { get; init; }
    public StatusPedido Status { get; init; }
    public StatusPagamento StatusPagamento { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyCollection<ItemPedidoConsumidorViewModel> Itens { get; init; } = [];
    public string? PixCopiaECola { get; set; }
    public string? PixQrCodeBase64 { get; set; }
    public string? PagamentoErro { get; set; }
}

public sealed class ItemPedidoConsumidorViewModel
{
    public string Produto { get; init; } = string.Empty;
    public string? Foto { get; init; }
    public int Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}
