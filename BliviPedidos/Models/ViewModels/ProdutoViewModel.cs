using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public class ProdutoViewModel
{
    public int Id { get; set; }

    [Display(Name = "Código")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "O campo 'Nome' é obrigatório.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O campo 'Preco de venda' é obrigatório.")]
    [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
    [Display(Name = "Preço venda")]
    public decimal PrecoVenda { get; set; }

    [Required(ErrorMessage = "O campo 'Preco pago' é obrigatório.")]
    [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
    [Display(Name = "Preço pago")]
    public decimal PrecoPago { get; set; }

    public int Quantidade { get; set; }

    public string? Tamanho { get; set; }

    public string? CodeBar { get; set; }

    [Display(Name = "Imagem atual")]
    public string? Foto { get; set; } = string.Empty;

    [Display(Name = "Imagem do produto")]
    public IFormFile? FotoArquivo { get; set; }

    public bool IsAtivo { get; set; } = true;

    public ICollection<ProdutoMovimentacao>? ProdutoMovimentacao { get; set; }

    public List<Produto>? Produtos { get; set; }

    public int FiltroRegistros { get; set; }

    // Propriedade para vincular à categoria
    [Required(ErrorMessage = "O campo 'Categoria' é obrigatório.")]
    [Display(Name = "Categoria")]
    public int CategoriaId { get; set; }

    // Propriedade para exibir os dados da categoria
    public Categoria? Categoria { get; set; }

    public List<Categoria>? Categorias { get; set; }

}
