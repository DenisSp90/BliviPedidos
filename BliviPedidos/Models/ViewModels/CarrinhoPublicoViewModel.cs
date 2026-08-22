using BliviPedidos.Dtos.Publico;

namespace BliviPedidos.Models.ViewModels;

public sealed class CarrinhoPublicoViewModel
{
    public required LojaPublicaDto Loja { get; init; }
    public IReadOnlyCollection<ItemCarrinhoPublicoViewModel> Itens { get; init; } = [];
    public IReadOnlyCollection<string> Erros { get; init; } = [];
    public bool Valido => Erros.Count == 0;
    public decimal Total => Itens.Sum(item => item.Subtotal);
}

public sealed class ResultadoValidacaoCarrinhoPublico
{
    public IReadOnlyCollection<ItemCarrinhoPublicoViewModel> Itens { get; init; } = [];
    public IReadOnlyCollection<string> Erros { get; init; } = [];
    public bool Valido => Erros.Count == 0;
}

public sealed class ItemCarrinhoPublicoViewModel
{
    public required ProdutoPublicoDto Produto { get; init; }
    public int Quantidade { get; init; }
    public decimal Subtotal => Produto.PrecoVenda * Quantidade;
}
