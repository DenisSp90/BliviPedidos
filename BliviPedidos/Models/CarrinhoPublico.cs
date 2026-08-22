namespace BliviPedidos.Models;

public sealed class CarrinhoPublico
{
    public required string Identificador { get; init; }
    public int LojaId { get; init; }
    public List<ItemCarrinhoPublico> Itens { get; init; } = [];
}

public sealed class ItemCarrinhoPublico
{
    public int ProdutoId { get; init; }
    public int Quantidade { get; set; }
}
