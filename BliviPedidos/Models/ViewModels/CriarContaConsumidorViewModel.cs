using System.ComponentModel.DataAnnotations;
using BliviPedidos.Dtos.Publico;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BliviPedidos.Models.ViewModels;

public sealed class CriarContaConsumidorViewModel
{
    [ValidateNever]
    public LojaPublicaDto Loja { get; set; } = null!;

    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120)]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu celular.")]
    [RegularExpression(@"^\+?[0-9 ()-]{10,20}$", ErrorMessage = "Informe um celular válido.")]
    [Display(Name = "Celular")]
    public string Celular { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Senha), ErrorMessage = "A confirmação não corresponde à senha.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
