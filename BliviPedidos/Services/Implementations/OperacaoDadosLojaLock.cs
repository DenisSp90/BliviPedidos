namespace BliviPedidos.Services.Implementations;

internal static class OperacaoDadosLojaLock
{
    internal static SemaphoreSlim Global { get; } = new(1, 1);
}
