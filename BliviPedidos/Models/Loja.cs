using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models;

public class Loja
{
    public const int PadraoId = 1;

    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Dominio { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? CorPrimaria { get; set; }

    public bool Ativa { get; set; } = true;

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<UsuarioLoja> Usuarios { get; set; } = new List<UsuarioLoja>();
}
