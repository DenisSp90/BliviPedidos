using System.Net.Http.Headers;
using System.Net.Http.Json;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BliviPedidos.Services.Implementations;

public sealed class EnvioSmsService : IEnvioSmsService
{
    private readonly HttpClient _httpClient;
    private readonly ConfirmacaoConsumidorOptions _options;

    public EnvioSmsService(HttpClient httpClient, IOptions<ConfirmacaoConsumidorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task EnviarCodigoAsync(
        string telefone, string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmsEndpoint))
            throw new InvalidOperationException(
                "A confirmação de telefone está ativa, mas o provedor de SMS não foi configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.SmsEndpoint)
        {
            Content = JsonContent.Create(new
            {
                destino = telefone,
                mensagem = $"Seu código de confirmação Blivi é: {codigo}"
            })
        };
        if (!string.IsNullOrWhiteSpace(_options.SmsApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SmsApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
