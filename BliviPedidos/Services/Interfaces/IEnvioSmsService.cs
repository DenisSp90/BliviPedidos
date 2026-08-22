namespace BliviPedidos.Services.Interfaces;

public interface IEnvioSmsService
{
    Task EnviarCodigoAsync(string telefone, string codigo, CancellationToken cancellationToken = default);
}
