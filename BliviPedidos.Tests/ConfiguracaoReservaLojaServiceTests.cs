using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class ConfiguracaoReservaLojaServiceTests : IDisposable
{
    private readonly string _pastaTemporaria = Path.Combine(
        Path.GetTempPath(),
        $"blivi-config-reserva-{Guid.NewGuid():N}");

    [Fact]
    public async Task SemArquivo_DeveUsarPadraoDeDozeHoras()
    {
        var service = CriarService();

        var minutos = await service.ObterExpiracaoMinutosAsync(1);

        Assert.Equal(720, minutos);
    }

    [Fact]
    public async Task Configuracoes_DevemFicarSeparadasPorLoja()
    {
        var service = CriarService();

        await service.SalvarExpiracaoMinutosAsync(1, 60);
        await service.SalvarExpiracaoMinutosAsync(2, 1440);

        Assert.Equal(60, await service.ObterExpiracaoMinutosAsync(1));
        Assert.Equal(1440, await service.ObterExpiracaoMinutosAsync(2));
        Assert.True(File.Exists(Path.Combine(_pastaTemporaria, "App_Data", "configuracoes-lojas", "1.json")));
        Assert.True(File.Exists(Path.Combine(_pastaTemporaria, "App_Data", "configuracoes-lojas", "2.json")));
    }

    [Theory]
    [InlineData(29)]
    [InlineData(4321)]
    public async Task PrazoForaDoIntervalo_DeveSerRejeitado(int minutos)
    {
        var service = CriarService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SalvarExpiracaoMinutosAsync(1, minutos));
    }

    private ConfiguracaoReservaLojaService CriarService()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath).Returns(_pastaTemporaria);
        return new ConfiguracaoReservaLojaService(
            environment.Object,
            Options.Create(new ReservaEstoqueOptions { ExpiracaoMinutos = 720 }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pastaTemporaria))
            Directory.Delete(_pastaTemporaria, recursive: true);
    }
}
