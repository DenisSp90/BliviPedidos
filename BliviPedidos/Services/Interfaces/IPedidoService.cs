using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces
{
    public interface IPedidoService
    {
        Task AtualizarStatusPagamentoAsync(int pedidoId, bool novoStatusPagamento);
        void AddItem(int id);
        Task RegistrarCancelamentoPedido(int pedidoId);
        void ClearPedido();
        Pedido GetPedidoById(int id);
        Task<Pedido> GetPedidoByIdAsync(int id);
        Pedido GetPedido();
        UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido);
        Pedido UpdateCadastro(Cadastro cadastro);
        IList<Pedido> GetListaPedidos();
        IList<Pedido> GetListaPedidosAtivos();
        Task<List<Pedido>> GetListaPedidosAtivosAsync();
        IList<Pedido> GetListaPedidosAtivosByEmail(string email);
    }
}
