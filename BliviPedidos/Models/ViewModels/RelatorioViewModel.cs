﻿namespace BliviPedidos.Models.ViewModels;

public class RelatorioViewModel
{
    public IEnumerable<Produto> Produtos { get; set; } = new List<Produto>();
    public IEnumerable<ProdutoMovimentacao> Movimentacoes { get; set; } = new List<ProdutoMovimentacao>();
    public IEnumerable<Pedido> Pedidos{ get; set; } = new List<Pedido>();

}
