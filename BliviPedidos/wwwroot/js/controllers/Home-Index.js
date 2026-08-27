function atualizarNumeroPedidosPendentes() {
    $.ajax({
        url: "/Store/GetInfoPedidos",
        method: "GET",
        success: function (data) {
            $('#numeroPedidos').text(data.numeroTotalPedidos);
        },
        error: function (xhr, status, error) {
            console.error("Não foi possível atualizar o contador de pedidos.", error);
        }
    });
}

$(document).ready(function () {
    atualizarNumeroPedidosPendentes();
    window.setInterval(atualizarNumeroPedidosPendentes, 30000);
});
