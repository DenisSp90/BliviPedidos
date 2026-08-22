namespace BliviPedidos.Models;

public sealed class ReservaEstoqueOptions
{
    public const string Secao = "ReservasEstoque";

    public int ExpiracaoMinutos { get; set; } = 30;
    public int IntervaloVerificacaoSegundos { get; set; } = 60;
}
