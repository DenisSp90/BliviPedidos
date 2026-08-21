namespace BliviPedidos.Models.ViewModels;

/// <summary>
/// Contrato seguro para exibir produtos fora da camada administrativa.
/// Campos de custo, estoque exato, codigo de barras e movimentacoes nao fazem
/// parte deste contrato por decisao de seguranca.
/// </summary>
public sealed class ProdutoPublicoViewModel
{
    public int Id { get; init; }
    public string? Codigo { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal PrecoVenda { get; init; }
    public string? Tamanho { get; init; }
    public string? Foto { get; init; }
    public bool IsAtivo { get; init; }
    public bool Disponivel { get; init; }
    public int? CategoriaId { get; init; }
    public string? CategoriaNome { get; init; }
}
