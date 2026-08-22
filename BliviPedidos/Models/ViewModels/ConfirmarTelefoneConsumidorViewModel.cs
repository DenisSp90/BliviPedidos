using System.ComponentModel.DataAnnotations;
using BliviPedidos.Dtos.Publico;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BliviPedidos.Models.ViewModels;

public sealed class ConfirmarTelefoneConsumidorViewModel
{
    [ValidateNever]
    public LojaPublicaDto Loja { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o código recebido.")]
    [Display(Name = "Código de confirmação")]
    public string Codigo { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
