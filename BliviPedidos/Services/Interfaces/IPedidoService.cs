﻿using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces
{
    public interface IPedidoService
    {
        void AddItem(int id);
        void ClearPedido();
        Pedido GetPedidoById(int id);
        Task<Pedido> GetPedidoByIdAsync(int id);
        Pedido GetPedido();
        UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido);
        Pedido UpdateCadastro(Cadastro cadastro);
        IList<Pedido> GetListaPedidos();
        IList<Pedido> GetListaPedidosAtivos();
        IList<Pedido> GetListaPedidosAtivosByEmail(string email);
    }
}
