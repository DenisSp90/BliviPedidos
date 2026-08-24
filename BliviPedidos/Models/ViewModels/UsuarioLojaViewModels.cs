using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class UsuarioLojaListaViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public bool PodeEditar { get; init; }
    public bool UsuarioAtual { get; init; }
}

public sealed class CriarUsuarioLojaViewModel
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+?[0-9 ()-]{10,20}$", ErrorMessage = "Informe um celular válido.")]
    [Display(Name = "Celular")]
    public string Telefone { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Perfil")]
    public string Perfil { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha inicial")]
    public string Senha { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Senha), ErrorMessage = "A confirmação não corresponde à senha.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public sealed class EditarUsuarioLojaViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+?[0-9 ()-]{10,20}$", ErrorMessage = "Informe um celular válido.")]
    [Display(Name = "Celular")]
    public string Telefone { get; set; } = string.Empty;

    [Display(Name = "Perfil")]
    public string? Perfil { get; set; }

    [Display(Name = "Usuário ativo")]
    public bool Ativo { get; set; }

    public bool Interno { get; set; }
    public bool UsuarioAtual { get; set; }
}
