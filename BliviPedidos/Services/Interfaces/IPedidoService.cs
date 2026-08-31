using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces
{
    public interface IPedidoService
    {
        Task AtualizarStatusPagamentoAsync(int pedidoId, StatusPagamento novoStatusPagamento);
        Task AtualizarStatusPedidoAsync(int pedidoId, StatusPedido novoStatus);
        void AddItem(int id);
        Task RegistrarCancelamentoPedido(int pedidoId, string? origem = null, string? ator = null);
        void ClearPedido();
        Pedido GetPedidoById(int id);
        Task<Pedido> GetPedidoByIdAsync(int id);
        Pedido GetPedido();
        UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido);
        Pedido UpdateCadastro(Cadastro cadastro);
        IList<Pedido> GetListaPedidos();
        IList<Pedido> GetListaPedidosRegistrados();
        IList<Pedido> GetListaPedidosRegistradosByEmail(string email);
        IList<Pedido> GetListaPedidosAtivos();
        Task<List<Pedido>> GetListaPedidosAtivosAsync();
        Task<int> ContarPedidosPendentesAsync(CancellationToken cancellationToken = default);
        Task<int> ContarPedidosRegistradosAsync(CancellationToken cancellationToken = default);
        IList<Pedido> GetListaPedidosAtivosByEmail(string email);
    }
}
