using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

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
                MailMessage mail = new MailMessage()
                {
                    From = new MailAddress(remetente, "Blivi Pedidos")
                };

                mail.To.Add(new MailAddress(ToEmail));
                if (!string.IsNullOrWhiteSpace(_emailSettings.CcEmail))
                    mail.CC.Add(new MailAddress(_emailSettings.CcEmail));

                mail.Subject = "Blivi Pedidos - " + subject;
                mail.Body = message;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;

                using (SmtpClient smtp = new SmtpClient(_emailSettings.PrimaryDomain, _emailSettings.PrimaryPort))
                {
                    smtp.Credentials = new NetworkCredential(_emailSettings.UsernameEmail, _emailSettings.UsernamePassword);
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
