using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public sealed class PedidoConfirmadoViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public int PedidoId { get; init; }
    public required string CodigoPublico { get; init; }
    public decimal Total { get; init; }
    public string? PixCopiaECola { get; init; }
    public string? PixQrCodeBase64 { get; init; }
    public string? PagamentoErro { get; init; }
}
