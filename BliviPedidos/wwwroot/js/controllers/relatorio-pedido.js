function carregarDetalhesPedido(pedidoId) {

    $.ajax({
        url: '/api/StoreApi/pedido-detalhe/' + pedidoId,
        type: 'GET',
        dataType: 'json',
        success: function (pedido) {
            $('#pedidoId').text(pedido.id);
            $('#pedidoNome').text(pedido.nomeCliente);
            $('#pedidoCelularCliente').text(pedido.celularCliente);
            $('#pedidoEmailCliente').text(pedido.emailCliente);
            $('#valorTotalPedido').text(pedido.valorTotalPedido.toFixed(2).replace('.', ','));

            $('#pedidoItens tbody').empty();

            pedido.itens.forEach(function (item) {
                $('#pedidoItens tbody').append(`
                    <tr>
                        <td>${item.nomeProduto}</td>
                        <td>${item.quantidade}</td>
                        <td>R$ ${item.precoUnitario.toFixed(2).replace('.', ',')}</td>
                    </tr>
                `);
            });

            if (pedido.pago) {
                $('#pedidoPagamento').html('<span class="badge bg-success">Pago</span>');
            } else {
                $('#pedidoPagamento').html('<span class="badge bg-warning text-dark">Pendente</span>');
            }

            $('#pedidoData').text(new Date(pedido.dataPedido).toLocaleDateString());
            $('#pedidoDataPagamento').text(pedido.dataPagamento ? new Date(pedido.dataPagamento).toLocaleDateString() : 'N/A');
            $('#pedidoResponsavel').text(pedido.emailResponsavel);

            $('#pedidoDetalheModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error(error);
            alert('Ocorreu um erro ao carregar os detalhes do pedido.');
        }
    });

}
