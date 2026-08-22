namespace BliviPedidos.Dtos.Publico;

/// <summary>
/// Contrato público sem custo, quantidade exata, código de barras,
/// movimentações de estoque, loja proprietária ou entidades de domínio.
/// </summary>
public sealed class ProdutoPublicoDto
{
    public int Id { get; init; }
    public string? Codigo { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal PrecoVenda { get; init; }
    public string? Tamanho { get; init; }
    public string? Foto { get; init; }
    public int? CategoriaId { get; init; }
    public string? CategoriaNome { get; init; }
}
