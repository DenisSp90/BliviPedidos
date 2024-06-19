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
    // Evento de clique no botão de logout
    $(".logout-btn").click(function (event) {
        // Evita o comportamento padrão do botão
        event.preventDefault();

        // Exibe o SweetAlert 2 para confirmar o logout
        Swal.fire({
            title: 'Deseja fazer logoff?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, Logout',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            // Se o usuário clicar em "Sim, Logout", envia o formulário de logout
            if (result.isConfirmed) {
                // Envia o formulário de logout
                $(this).closest("form").submit();
            }
        });
    });
});