namespace BliviPedidos.Models;

public sealed class ConfirmacaoConsumidorOptions
{
    public const string Secao = "ConfirmacaoConsumidor";
    public bool ExigirEmail { get; set; } = true;
    public bool ExigirTelefone { get; set; }
    public string? SmsEndpoint { get; set; }
    public string? SmsApiKey { get; set; }
}
