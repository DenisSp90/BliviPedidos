using Microsoft.AspNetCore.Identity.UI.Services;

namespace BliviPedidos.Services.Implementations;

public sealed class IdentityEmailSender : IEmailSender
{
    private readonly Interfaces.IEmailEnviarService _email;

    public IdentityEmailSender(Interfaces.IEmailEnviarService email) => _email = email;

    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        _email.SendEmailAsync(email, subject, htmlMessage);
}
