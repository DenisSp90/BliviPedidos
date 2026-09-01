namespace BliviPedidos.Services.Interfaces;

public interface IConfiguracaoReservaLojaService
{
    Task<int> ObterExpiracaoMinutosAsync(int lojaId, CancellationToken cancellationToken = default);
    Task SalvarExpiracaoMinutosAsync(int lojaId, int expiracaoMinutos, CancellationToken cancellationToken = default);
}
