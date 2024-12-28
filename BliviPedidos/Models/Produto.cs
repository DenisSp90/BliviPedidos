using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BliviPedidos.Models;

public class Produto
{
    public Produto()
    {        
    }

    public Produto(int id, string codigo, string nome, decimal precoVenda, decimal precoPago, int quantidade, string? tamanho, string? codeBar)
    {
        Id = id;
        Codigo = codigo;
        Nome = nome;
        PrecoVenda = precoVenda;
        PrecoPago = precoPago;
        Quantidade = quantidade;
        Tamanho = tamanho;
        CodeBar = codeBar;           
    }

    public int Id { get; protected set; }

    public string? Codigo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
    [Display(Name = "Preço Venda")]
    public decimal PrecoVenda { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
    [Display(Name = "Preço Pago")]
    public decimal PrecoPago { get; set; }

    [Display(Name = "Quantidade")]
    public int Quantidade { get; set; }

    [Display(Name = "Tamanho")]
    public string? Tamanho { get; set; }

    public string? CodeBar { get; set; }

    public string? Foto { get; set; }

    public bool IsAtivo { get; set; }

    // Relacionamento 1:N com MovimentacaoEstoque
    public ICollection<ProdutoMovimentacao>? ProdutoMovimentacao { get; set; }
}

public class ProdutoMovimentacao
{
    public int Id { get; set; }
    public int? ProdutoId { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;
    public int Quantidade { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada", "Saída" , "Cancelado", "Troca"
    public string? Observacao { get; set; }

    public Produto Produto { get; set; }
}
