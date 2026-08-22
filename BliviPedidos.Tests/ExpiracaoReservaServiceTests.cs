using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public class ExpiracaoReservaServiceTests
{
    [Fact]
    public async Task Processar_DeveExpirarSomenteReservaVencidaENaoPaga()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        var pedidoService = new Mock<IPedidoService>();
        services.AddScoped(_ => pedidoService.Object);
        await using var provider = services.BuildServiceProvider();

        int pedidoVencidoId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            var vencido = CriarPedido(DateTime.UtcNow.AddMinutes(-1));
            var vigente = CriarPedido(DateTime.UtcNow.AddMinutes(10));
            var pago = CriarPedido(DateTime.UtcNow.AddMinutes(-1));
            pago.StatusPagamento = StatusPagamento.Pago;
            context.Pedido.AddRange(vencido, vigente, pago);
            await context.SaveChangesAsync();
            pedidoVencidoId = vencido.Id;
        }

        var worker = new ExpiracaoReservaService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ReservaEstoqueOptions()),
            NullLogger<ExpiracaoReservaService>.Instance);

        await worker.ProcessarAsync(CancellationToken.None);

        pedidoService.Verify(service => service.RegistrarCancelamentoPedido(
            pedidoVencidoId,
            OrigemMovimentacaoEstoque.ExpiracaoReserva,
            "SISTEMA"), Times.Once);
        pedidoService.Verify(service => service.RegistrarCancelamentoPedido(
            It.Is<int>(id => id != pedidoVencidoId),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
    }

    private static Pedido CriarPedido(DateTime expiraEm) => new()
    {
        Status = StatusPedido.Confirmado,
        StatusPagamento = StatusPagamento.AguardandoPagamento,
        ReservaExpiraEm = expiraEm,
        Cadastro = new Cadastro { Nome = "Cliente", Telefone = "11 99999-9999" }
    };
}
