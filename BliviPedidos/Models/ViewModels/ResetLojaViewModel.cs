using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public sealed class ResetLojaViewModel
{
    public int LojaId { get; set; }
    public string LojaNome { get; set; } = string.Empty;
    public int Categorias { get; set; }
    public int Produtos { get; set; }
    public int Clientes { get; set; }
    public int Pedidos { get; set; }
    public int Movimentacoes { get; set; }
    public int Imagens { get; set; }

    [Required(ErrorMessage = "Digite o nome da loja para confirmar.")]
    [Display(Name = "Nome da loja")]
    public string Confirmacao { get; set; } = string.Empty;
}
