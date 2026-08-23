using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class UsuarioAdminListaViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string LojaNome { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public bool UsuarioAtual { get; init; }
}

public sealed class UsuarioAdminEdicaoViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma loja.")]
    [Display(Name = "Loja")]
    public int LojaId { get; set; }

    [Required(ErrorMessage = "Selecione um perfil.")]
    [Display(Name = "Perfil")]
    public string Perfil { get; set; } = string.Empty;

    [Display(Name = "Usuário ativo")]
    public bool Ativo { get; set; } = true;

    public bool UsuarioAtual { get; set; }
}
