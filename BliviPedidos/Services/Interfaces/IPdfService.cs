namespace BliviPedidos.Services.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GerarPdfAsync(string html);
    }
}
