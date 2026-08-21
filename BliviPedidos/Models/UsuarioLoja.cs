using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BliviPedidos.Models;

public class UsuarioLoja
{
    [Key]
    public string UsuarioId { get; set; } = string.Empty;

    public IdentityUser Usuario { get; set; } = null!;

    public int LojaId { get; set; } = Loja.PadraoId;

    public Loja Loja { get; set; } = null!;
}
