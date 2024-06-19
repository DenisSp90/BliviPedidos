﻿using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Services.Interfaces
{
    public interface IProdutoService
    {
        Task<List<Produto>> GetProdutosAsync();
        Task<ProdutoViewModel> ProcurarProdutoAsync(int id);
        Task<bool> RegistrarProdutoAsync(Produto produto);
        bool VerificarProdutoVinculadoPedido(int produtoId);
        bool UpdateQuantidade(List<ItemPedido> itens);
    }
}
