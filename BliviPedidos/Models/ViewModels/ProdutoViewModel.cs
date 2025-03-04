﻿using System.ComponentModel.DataAnnotations;

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

    public string? Foto { get; set; } = string.Empty;

    public bool IsAtivo { get; set; }

    public ICollection<ProdutoMovimentacao>? ProdutoMovimentacao { get; set; }

    public List<Produto>? Produtos { get; set; }

    public int FiltroRegistros { get; set; }
}
