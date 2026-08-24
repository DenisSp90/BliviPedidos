using System.ComponentModel.DataAnnotations;
using BliviPedidos.Dtos.Publico;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BliviPedidos.Models.ViewModels;

public sealed class MinhaContaConsumidorViewModel
{
    [ValidateNever]
    public LojaPublicaDto Loja { get; set; } = null!;
    public EditarDadosConsumidorViewModel Dados { get; set; } = new();
    public AlterarSenhaConsumidorViewModel Senha { get; set; } = new();
}

public sealed class EditarDadosConsumidorViewModel
{
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
}

public sealed class AlterarSenhaConsumidorViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "A confirmação não corresponde à nova senha.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
