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

        public Task SendEmailAsync(string? email, string? subject, string? message)
        {
            try
            {
                Execute(email, subject, message).Wait();
                return Task.FromResult(0);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task Execute(string email, string subject, string message)
        {
            try
            {
                string ToEmail = email;

                MailMessage mail = new MailMessage()
                {
                    From = new MailAddress(_emailSettings.UsernameEmail, "Blivi Pedidos")
                };

                mail.To.Add(new MailAddress(ToEmail));
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
