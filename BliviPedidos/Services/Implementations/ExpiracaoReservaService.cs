using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BliviPedidos.Services.Implementations;

public sealed class ExpiracaoReservaService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiracaoReservaService> _logger;
    private readonly TimeSpan _intervalo;

    public ExpiracaoReservaService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReservaEstoqueOptions> options,
        ILogger<ExpiracaoReservaService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervalo = TimeSpan.FromSeconds(Math.Max(10, options.Value.IntervaloVerificacaoSegundos));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_intervalo);
        do
        {
            await ProcessarAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task ProcessarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
            var agora = DateTime.UtcNow;
            var reservas = await context.Pedido
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(pedido => pedido.Status == StatusPedido.Confirmado
                    && pedido.StatusPagamento == StatusPagamento.AguardandoPagamento
                    && pedido.ReservaExpiraEm != null
                    && pedido.ReservaExpiraEm <= agora)
                .Select(pedido => new { pedido.Id, pedido.LojaId })
                .ToListAsync(cancellationToken);

            foreach (var reserva in reservas)
            {
                try
                {
                    context.DefinirLojaAtual(reserva.LojaId);
                    await pedidoService.RegistrarCancelamentoPedido(
                        reserva.Id,
                        OrigemMovimentacaoEstoque.ExpiracaoReserva,
                        "SISTEMA");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao expirar reserva. PedidoId: {PedidoId}, LojaId: {LojaId}", reserva.Id, reserva.LojaId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao procurar reservas de estoque expiradas.");
        }
    }
}
