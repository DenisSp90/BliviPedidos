namespace BliviPedidos.Services.Interfaces;

public interface IConfirmacaoCheckoutService
{
    Task<ResultadoConfirmacaoCheckout> ConfirmarAsync(int lojaId, CancellationToken cancellationToken = default);
}

public sealed record ResultadoConfirmacaoCheckout(
    bool Sucesso,
    int? PedidoId,
    string? CodigoPublico,
    string? Erro)
{
    public static ResultadoConfirmacaoCheckout Confirmado(int pedidoId, string codigoPublico) =>
        new(true, pedidoId, codigoPublico, null);
    public static ResultadoConfirmacaoCheckout Falhou(string erro) => new(false, null, null, erro);
}
