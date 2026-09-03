using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BliviPedidos.Services.Implementations
{
    public class EmailEnviarService : IEmailEnviarService
    {
        private readonly EmailSettings _emailSettings;

        public EmailEnviarService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public Task SendEmailAsync(string? email, string? subject, string? message) =>
            Execute(email!, subject!, message!);

        public async Task Execute(string email, string subject, string message)
        {
            try
            {
                string ToEmail = email;

                var remetente = string.IsNullOrWhiteSpace(_emailSettings.FromEmail)
                    ? _emailSettings.UsernameEmail
                    : _emailSettings.FromEmail;
                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress("Blivi Pedidos", remetente));
                mail.To.Add(MailboxAddress.Parse(ToEmail));
                if (!string.IsNullOrWhiteSpace(_emailSettings.CcEmail))
                    mail.Cc.Add(MailboxAddress.Parse(_emailSettings.CcEmail));

                var emailSuporte = _emailSettings.UsernameEmail?.Trim();
                var suporteJaIncluido = string.Equals(ToEmail, emailSuporte, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(_emailSettings.CcEmail?.Trim(), emailSuporte, StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(emailSuporte) && !suporteJaIncluido)
                    mail.Bcc.Add(MailboxAddress.Parse(emailSuporte));

                mail.Subject = "Blivi Pedidos - " + subject;
                mail.Body = new BodyBuilder { HtmlBody = message }.ToMessageBody();
                mail.Priority = MessagePriority.Urgent;

                using var smtp = new SmtpClient();
                var seguranca = _emailSettings.PrimaryPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;
                await smtp.ConnectAsync(_emailSettings.PrimaryDomain, _emailSettings.PrimaryPort, seguranca);
                await smtp.AuthenticateAsync(_emailSettings.UsernameEmail, _emailSettings.UsernamePassword);
                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
