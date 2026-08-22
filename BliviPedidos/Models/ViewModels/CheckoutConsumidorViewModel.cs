using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public sealed class CheckoutConsumidorViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public required DadosConsumidorCheckout Dados { get; init; }
    public decimal Total { get; init; }
    public int QuantidadeItens { get; init; }
    public bool DadosSalvos { get; init; }
}
