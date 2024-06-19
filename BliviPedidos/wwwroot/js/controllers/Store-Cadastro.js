function atualizarEstadoPedido(idPedido, novoEstado) {

    $.ajax({
        type: "POST",
        url: "/Store/AtualizarEstadoPagamento",
        data: { idPedido: idPedido, pago: novoEstado },
        success: function (data) {
            debugger;

            console.log("Estado do pedido atualizado com sucesso.");

            var estadoAtivo = data.ativo;
            var estadoPagamento = data.pago;

            if (estadoPagamento === true) {
                console.log("O pedido está pago.");
                $('.widget:eq(1)').removeClass('yellow-bg red-bg').addClass('navy-bg');
            } else {
                console.log("O pedido não está pago.");
                $('.widget:eq(1)').removeClass('navy-bg red-bg').addClass('yellow-bg');
            }
        },
        error: function (xhr, status, error) {
            console.error("Erro ao atualizar estado do pedido:", error);
        }
    });
}
function cancelarPedido(idPedido, novoEstado) {
    Swal.fire({
        title: "Deseja cancelar o pedido #" + idPedido + "?",
        text: "Você não vai conseguir acessar o registro posteriormente!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Sim, Cancelar Pedido!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: "POST",
                url: "/Store/CancelarPedido",
                data: { idPedido: idPedido, ativo: novoEstado },
                success: function (data) {
                    var estadoAtivo = data.ativo;
                    var estadoPagamento = data.pago;
                    if (estadoAtivo === false) {
                        $('.widget:eq(1)').removeClass('navy-bg yellow-bg').addClass('red-bg');
                        window.location.href = '/Store/PedidoLista?filtro=1'; 
                    } else {
                        console.log("O pedido está ativo.");
                        if (estadoPagamento === true) {
                            console.log("O pedido está pago.");
                            $('.widget:eq(1)').removeClass('yellow-bg red-bg').addClass('navy-bg');
                        } else {
                            console.log("O pedido não está pago.");
                            $('.widget:eq(1)').removeClass('navy-bg red-bg').addClass('yellow-bg');
                        }
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Erro ao atualizar estado do pedido:", error);
                    // Aqui você pode lidar com o erro de acordo com sua lógica de aplicativo
                }
            });            
        }
    });    
}
$(document).ready(function () {
    var elements = $('[data-imask]');

    elements.each(function () {
        var maskOptions = {
            mask: $(this).data('imask')
        };
        var mask = IMask(this, maskOptions);
    });
});
$('#cep').blur(function () {
    var cep = $('#cep').val();

    if (!cep.trim()) {

        $('#enderecoId').val('');
        $('#logradouro').val('');
        $('#complemento').val('');
        $('#bairro').val('');
        $('#cidade').val('');
        $('#uf').val('');

        return;
    }

    cep = cep.replace(/\s|-/g, '');

    if (cep.length !== 8) {

        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: 'Por favor, digite um CEP válido!',
        });

        $('#enderecoId').val('');
        $('#cep').val('');
        $('#logradouro').val('');
        $('#complemento').val('');
        $('#bairro').val('');
        $('#cidade').val('');
        $('#uf').val('');

        return;
    }

    var url = 'https://viacep.com.br/ws/' + cep + '/json/';

    fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.erro) {

                Swal.fire({
                    icon: 'error',
                    title: 'Oops...',
                    text: 'Por favor, digite um CEP válido!',
                });

                $('#enderecoId').val('');
                $('#cep').val('');
                $('#logradouro').val('');
                $('#complemento').val('');
                $('#bairro').val('');
                $('#cidade').val('');
                $('#uf').val('');

                return;
            } else {
                $('#logradouro').val(data.logradouro);
                $('#bairro').val(data.bairro);
                $('#cidade').val(data.localidade);
                $('#uf').val(data.uf);
            }
        })
        .catch(error => {
            console.error('Erro:', error);
            $('#enderecoId').val('');
            $('#cep').val('');
            $('#logradouro').val('');
            $('#complemento').val('');
            $('#bairro').val('');
            $('#cidade').val('');
            $('#uf').val('');
        });
});