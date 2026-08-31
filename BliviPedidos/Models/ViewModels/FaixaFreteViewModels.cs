using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class ConfiguracaoFreteLojaViewModel
{
    public int LojaId { get; init; }
    public string LojaNome { get; init; } = string.Empty;
    public string LojaSlug { get; init; } = string.Empty;
    public IReadOnlyCollection<FaixaFreteLoja> Faixas { get; init; } = [];
    public FaixaFreteViewModel NovaFaixa { get; init; } = new();
}

public sealed class EntregaLojaViewModel
{
    public string LojaNome { get; init; } = string.Empty;
    public bool RetiradaAtiva { get; set; }
    public bool EntregaAtiva { get; set; }
    public string? CepOrigem { get; set; }
    public string? EnderecoOrigem { get; set; }
    public string? NumeroOrigem { get; set; }
    public string? ComplementoOrigem { get; set; }
    public string? BairroOrigem { get; set; }
    public string? MunicipioOrigem { get; set; }
    public string? UfOrigem { get; set; }
    public decimal PercentualConsumoEntrega { get; init; }
    public DateTime? AssinaturaInicioEm { get; init; }
    public DateTime? AssinaturaTerminoEm { get; init; }
    public IReadOnlyCollection<FaixaFreteLoja> Faixas { get; init; } = [];
    public FaixaFreteViewModel NovaFaixa { get; init; } = new();
}

public sealed class FaixaFreteViewModel
{
    public int Id { get; set; }

    public int LojaId { get; set; }

    [Range(typeof(decimal), "0", "99999", ErrorMessage = "A distância inicial deve ser positiva.")]
    [Display(Name = "De (km)")]
    public decimal DistanciaInicialKm { get; set; }

    [Range(typeof(decimal), "0.001", "99999", ErrorMessage = "A distância final deve ser maior que zero.")]
    [Display(Name = "Até (km)")]
    public decimal DistanciaFinalKm { get; set; }

    [Range(typeof(decimal), "0", "99999999", ErrorMessage = "O frete não pode ser negativo.")]
    [Display(Name = "Valor do frete")]
    public decimal ValorFrete { get; set; }

    [Display(Name = "Faixa ativa")]
    public bool Ativa { get; set; } = true;
}
