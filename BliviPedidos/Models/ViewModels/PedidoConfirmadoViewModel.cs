using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public sealed class PedidoConfirmadoViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public required string CodigoPublico { get; init; }
}
