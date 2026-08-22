using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models;

public sealed class DadosConsumidorCheckout
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120)]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu telefone.")]
    [RegularExpression(@"^\+?[0-9 ()-]{10,20}$", ErrorMessage = "Informe um telefone válido.")]
    public string Telefone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(9)]
    [RegularExpression(@"^$|^[0-9]{5}-?[0-9]{3}$", ErrorMessage = "Informe um CEP válido.")]
    public string? CEP { get; set; }

    [StringLength(180)]
    [Display(Name = "Endereço")]
    public string? Endereco { get; set; }

    [StringLength(80)]
    public string? Complemento { get; set; }

    [StringLength(80)]
    public string? Bairro { get; set; }

    [StringLength(100)]
    [Display(Name = "Cidade")]
    public string? Municipio { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "Informe a UF com duas letras.")]
    [Display(Name = "UF")]
    public string? UF { get; set; }
}
