using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class ConfiguracaoReservaEstoqueViewModel
{
    [Display(Name = "Prazo para pagamento (minutos)")]
    [Range(30, 4320, ErrorMessage = "Informe um prazo entre 30 minutos e 72 horas.")]
    public int ExpiracaoMinutos { get; set; } = 720;
}
