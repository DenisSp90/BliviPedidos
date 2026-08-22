using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class LojaPixViewModel
{
    public int LojaId { get; set; }
    public string LojaNome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Aceitar pagamentos por PIX")]
    public bool Ativo { get; set; }

    [MaxLength(120)]
    [Display(Name = "Nome do favorecido")]
    public string? Responsavel { get; set; }

    [MaxLength(20)]
    [Display(Name = "Tipo da chave")]
    public string? Tipo { get; set; }

    [MaxLength(255)]
    [Display(Name = "Chave PIX")]
    public string? Chave { get; set; }

    [MaxLength(80)]
    [Display(Name = "Cidade do favorecido")]
    public string? Cidade { get; set; }
}
