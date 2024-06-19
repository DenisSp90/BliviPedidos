$(document).ready(function () {
    $.ajax({
        url: "/Store/GetInfoPedidos", 
        method: "GET",
        success: function (data) {
            $('#numeroPedidos').text(data.numeroTotalPedidos);
            $('#numeroPedidosNaoPagos').text(data.numeroPedidosNaoPagos);
        },
        error: function (xhr, status, error) {
            // Lidar com erros, se houver
            console.error(error);
        }
    });
});

$(document).ready(function () {
    // Evento de clique no bot�o de logout
    $(".logout-btn").click(function (event) {
        // Evita o comportamento padr�o do bot�o
        event.preventDefault();

        // Exibe o SweetAlert 2 para confirmar o logout
        Swal.fire({
            title: 'Deseja fazer logoff?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, Logout',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            // Se o usu�rio clicar em "Sim, Logout", envia o formul�rio de logout
            if (result.isConfirmed) {
                // Envia o formul�rio de logout
                $(this).closest("form").submit();
            }
        });
    });
});