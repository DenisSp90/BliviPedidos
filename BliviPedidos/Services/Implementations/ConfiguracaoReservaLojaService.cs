using System.Text.Json;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BliviPedidos.Services.Implementations;

public sealed class ConfiguracaoReservaLojaService : IConfiguracaoReservaLojaService
{
    public const int ExpiracaoMinimaMinutos = 30;
    public const int ExpiracaoMaximaMinutos = 4320;

    private static readonly SemaphoreSlim BloqueioEscrita = new(1, 1);
    private readonly string _pastaConfiguracoes;
    private readonly int _expiracaoPadraoMinutos;

    public ConfiguracaoReservaLojaService(
        IWebHostEnvironment environment,
        IOptions<ReservaEstoqueOptions> options)
    {
        _pastaConfiguracoes = Path.Combine(environment.ContentRootPath, "App_Data", "configuracoes-lojas");
        _expiracaoPadraoMinutos = Normalizar(options.Value.ExpiracaoMinutos);
    }

    public async Task<int> ObterExpiracaoMinutosAsync(
        int lojaId,
        CancellationToken cancellationToken = default)
    {
        ValidarLojaId(lojaId);
        var caminho = ObterCaminho(lojaId);
        if (!File.Exists(caminho))
            return _expiracaoPadraoMinutos;

        try
        {
            await using var arquivo = File.OpenRead(caminho);
            var configuracao = await JsonSerializer.DeserializeAsync<ConfiguracaoArquivo>(
                arquivo,
                cancellationToken: cancellationToken);
            return configuracao is null
                ? _expiracaoPadraoMinutos
                : Normalizar(configuracao.ExpiracaoReservaMinutos);
        }
        catch (JsonException)
        {
            return _expiracaoPadraoMinutos;
        }
    }

    public async Task SalvarExpiracaoMinutosAsync(
        int lojaId,
        int expiracaoMinutos,
        CancellationToken cancellationToken = default)
    {
        ValidarLojaId(lojaId);
        if (expiracaoMinutos is < ExpiracaoMinimaMinutos or > ExpiracaoMaximaMinutos)
            throw new ArgumentOutOfRangeException(nameof(expiracaoMinutos));

        Directory.CreateDirectory(_pastaConfiguracoes);
        var caminho = ObterCaminho(lojaId);
        var temporario = caminho + ".tmp";

        await BloqueioEscrita.WaitAsync(cancellationToken);
        try
        {
            await using (var arquivo = new FileStream(
                temporario,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    arquivo,
                    new ConfiguracaoArquivo(expiracaoMinutos),
                    new JsonSerializerOptions { WriteIndented = true },
                    cancellationToken);
            }

            File.Move(temporario, caminho, overwrite: true);
        }
        finally
        {
            BloqueioEscrita.Release();
        }
    }

    private string ObterCaminho(int lojaId) => Path.Combine(_pastaConfiguracoes, $"{lojaId}.json");

    private static void ValidarLojaId(int lojaId)
    {
        if (lojaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(lojaId));
    }

    private static int Normalizar(int minutos) =>
        Math.Clamp(minutos, ExpiracaoMinimaMinutos, ExpiracaoMaximaMinutos);

    private sealed record ConfiguracaoArquivo(int ExpiracaoReservaMinutos);
}
