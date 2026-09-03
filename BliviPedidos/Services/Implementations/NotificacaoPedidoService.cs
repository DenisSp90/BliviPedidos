using System.Net.Mail;
using System.Text;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class NotificacaoPedidoService : INotificacaoPedidoService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailEnviarService _email;
    private readonly ILogger<NotificacaoPedidoService> _logger;

    public NotificacaoPedidoService(ApplicationDbContext context, IEmailEnviarService email, ILogger<NotificacaoPedidoService> logger)
    {
        _context = context;
        _email = email;
        _logger = logger;
    }

    public Task NotificarPedidoCriadoAsync(int pedidoId, CancellationToken cancellationToken = default) =>
        NotificarAsync(pedidoId, "Pedido recebido", "Seu pedido foi recebido e está aguardando processamento.", cancellationToken);

    public Task NotificarStatusPedidoAsync(int pedidoId, StatusPedido status, CancellationToken cancellationToken = default) =>
        NotificarAsync(pedidoId, $"Pedido: {Descrever(status)}", $"A situação do pedido foi alterada para <strong>{Descrever(status)}</strong>.", cancellationToken);

    public Task NotificarStatusPagamentoAsync(int pedidoId, StatusPagamento status, CancellationToken cancellationToken = default) =>
        NotificarAsync(pedidoId, $"Pagamento: {Descrever(status)}", $"A situação do pagamento foi alterada para <strong>{Descrever(status)}</strong>.", cancellationToken);

    private async Task NotificarAsync(int pedidoId, string assuntoEvento, string textoEvento, CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await _context.Pedido.AsNoTracking()
                .Include(p => p.Loja).Include(p => p.Cadastro)
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .SingleOrDefaultAsync(p => p.Id == pedidoId, cancellationToken);
            if (pedido is null) return;

            var destinatarios = new[] { pedido.Cadastro?.Email, pedido.Loja?.EmailContato }
                .Where(EmailValido).Select(e => e!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (destinatarios.Length == 0) return;

            var identificador = pedido.CodigoPublico ?? pedido.Id.ToString();
            var corpo = new StringBuilder()
                .Append($"<h2>{pedido.Loja?.Nome ?? "Blivi Pedidos"}</h2>")
                .Append($"<p>{textoEvento}</p><p>Pedido: <strong>{identificador}</strong></p>")
                .Append($"<p>Cliente: {pedido.Cadastro?.Nome ?? "Não informado"}</p><ul>");
            foreach (var item in pedido.Itens)
                corpo.Append($"<li>{item.Produto?.Nome ?? "Produto"} — {item.Quantidade} × {item.PrecoUnitario:C}</li>");
            corpo.Append($"</ul><p>Total: <strong>{pedido.ValorTotalPedido:C}</strong></p>");

            foreach (var destinatario in destinatarios)
                await _email.SendEmailAsync(destinatario, $"{assuntoEvento} — pedido {identificador}", corpo.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar notificação do pedido {PedidoId}: {Evento}", pedidoId, assuntoEvento);
        }
    }

    private static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { _ = new MailAddress(email); return true; }
        catch (FormatException) { return false; }
    }

    private static string Descrever(StatusPedido status) => status switch
    {
        StatusPedido.EmPreparacao => "Em preparação", StatusPedido.Pronto => "Pronto",
        StatusPedido.Enviado => "Enviado", StatusPedido.Concluido => "Concluído",
        StatusPedido.Cancelado => "Cancelado", StatusPedido.Confirmado => "Confirmado", _ => status.ToString()
    };

    private static string Descrever(StatusPagamento status) => status switch
    {
        StatusPagamento.AguardandoPagamento => "Aguardando pagamento", StatusPagamento.Pago => "Pago",
        StatusPagamento.Falhou => "Falhou", StatusPagamento.Estornado => "Estornado",
        StatusPagamento.Cancelado => "Cancelado", _ => status.ToString()
    };
}
