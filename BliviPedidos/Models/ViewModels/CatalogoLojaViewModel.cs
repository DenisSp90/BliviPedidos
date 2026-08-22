using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public class CatalogoLojaViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public IReadOnlyCollection<ProdutoPublicoDto> Produtos { get; init; } = [];
    public IReadOnlyCollection<CategoriaPublicaDto> Categorias { get; init; } = [];
    public string? Busca { get; init; }
    public int? CategoriaId { get; init; }
}

public sealed class ProdutoDetalheLojaViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public required ProdutoPublicoDto Produto { get; init; }
}
