using System.Text.Json;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;

namespace BliviPedidos.Services.Implementations;

public sealed class DadosConsumidorCheckoutService : IDadosConsumidorCheckoutService
{
    private const string PrefixoChave = "checkout-consumidor:loja:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DadosConsumidorCheckoutService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DadosConsumidorCheckout? Obter(int lojaId)
    {
        ValidarLoja(lojaId);
        var json = Sessao.GetString(ObterChave(lojaId));
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<DadosConsumidorCheckout>(json, JsonOptions);
    }

    public void Salvar(int lojaId, DadosConsumidorCheckout dados)
    {
        ValidarLoja(lojaId);
        ArgumentNullException.ThrowIfNull(dados);
        Sessao.SetString(ObterChave(lojaId), JsonSerializer.Serialize(Normalizar(dados), JsonOptions));
    }

    public void Limpar(int lojaId)
    {
        ValidarLoja(lojaId);
        Sessao.Remove(ObterChave(lojaId));
    }

    private ISession Sessao => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Não existe uma sessão HTTP ativa para o checkout.");

    private static string ObterChave(int lojaId) => $"{PrefixoChave}{lojaId}";

    private static DadosConsumidorCheckout Normalizar(DadosConsumidorCheckout dados) => new()
    {
        Nome = dados.Nome.Trim(),
        Telefone = dados.Telefone.Trim(),
        Email = NormalizarOpcional(dados.Email)?.ToLowerInvariant(),
        CEP = NormalizarOpcional(dados.CEP),
        Endereco = NormalizarOpcional(dados.Endereco),
        Complemento = NormalizarOpcional(dados.Complemento),
        Bairro = NormalizarOpcional(dados.Bairro),
        Municipio = NormalizarOpcional(dados.Municipio),
        UF = NormalizarOpcional(dados.UF)?.ToUpperInvariant()
    };

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static void ValidarLoja(int lojaId)
    {
        if (lojaId <= 0) throw new ArgumentOutOfRangeException(nameof(lojaId));
    }
}
