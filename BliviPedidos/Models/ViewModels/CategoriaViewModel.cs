using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public class CategoriaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O campo 'Nome' é obrigatório.")]
    [Display(Name = "Nome da Categoria")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Ativo")]
    public bool IsAtivo { get; set; }

    // Relacionamento 1:N (uma categoria para muitos produtos)
    public ICollection<Produto>? Produtos { get; set; } = new List<Produto>();
}
