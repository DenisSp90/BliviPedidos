using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface INotificacaoPedidoService
{
    Task NotificarPedidoCriadoAsync(int pedidoId, CancellationToken cancellationToken = default);
    Task NotificarStatusPedidoAsync(int pedidoId, StatusPedido status, CancellationToken cancellationToken = default);
    Task NotificarStatusPagamentoAsync(int pedidoId, StatusPagamento status, CancellationToken cancellationToken = default);
}
