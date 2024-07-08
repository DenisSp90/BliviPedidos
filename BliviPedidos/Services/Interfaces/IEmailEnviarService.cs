namespace BliviPedidos.Services.Interfaces
{
    public interface IEmailEnviarService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
