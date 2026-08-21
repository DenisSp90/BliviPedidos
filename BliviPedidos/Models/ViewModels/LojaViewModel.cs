using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public class LojaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da loja é obrigatório.")]
    [MaxLength(120)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O slug é obrigatório.")]
    [MaxLength(80)]
    [RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$", ErrorMessage = "Use apenas letras, números e hífens.")]
    [Display(Name = "Slug da URL")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Domínio personalizado")]
    public string? Dominio { get; set; }

    [MaxLength(500)]
    [Url(ErrorMessage = "Informe uma URL válida para o logo.")]
    [Display(Name = "URL do logo")]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Informe uma cor hexadecimal no formato #RRGGBB.")]
    [Display(Name = "Cor primária")]
    public string CorPrimaria { get; set; } = "#0d6efd";

    [Display(Name = "Loja ativa")]
    public bool Ativa { get; set; } = true;
}
