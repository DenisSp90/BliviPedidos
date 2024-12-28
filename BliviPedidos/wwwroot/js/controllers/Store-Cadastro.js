$(document).ready(function () {

    $('#telefoneInput').mask('00 00 00000-0000');

    $('#telefoneInput').val('55 11 '); // Definindo o valor inicial

    $('#telefoneInput').blur(function () {
        var telefone = $(this).val().replace(/\D/g, ''); // Remover todos os caracteres não numéricos

        var regex = /^\d{10}$/;
        if (!regex.test(telefone)) {
            if (!telefone.startsWith('55')) {
                telefone = '55' + telefone;
            }
            if (!telefone.startsWith('55')) {
                telefone = '55' + telefone.substring(2);
            }
        }

        telefone = telefone.replace(/^(\d{2})(\d{2})(\d{5})(\d{4})$/, '$1 $2 $3-$4');
        $(this).val(telefone);

        $.ajax({
            url: '/Store/ClienteGetByTelefone', // Ajuste a URL conforme necessário
            type: 'GET',
            data: { telefone: telefone },
            success: function (data) {
                $('#nomeInput').val(data.nome);
                $('#emailInput').val(data.email);
                $('#cep').val(data.cep);

                console.log(data);
            },
            error: function (error) {
                console.error('Erro ao buscar cliente:', error);
            }
        });
    });

    if ($('#avulsoCheckbox').is(':checked')) {
        $('#nomeInput').prop('disabled', true).val('AVULSO');
        $('#emailInput').prop('disabled', true).val('email@email.com.br');
        $('#telefoneInput').prop('disabled', true).val('00 00 00000-0000');
    } else {
        $('#nomeInput').prop('disabled', false).val('');
        $('#emailInput').prop('disabled', false).val('');
        $('#telefoneInput').prop('disabled', false).val('55 11 ');
    }

    $('#avulsoCheckbox').change(function () {
        if (this.checked) {
            $('#nomeInput').prop('disabled', true).val('AVULSO');
            $('#emailInput').prop('disabled', true).val('email@email.com.br');
            $('#telefoneInput').prop('disabled', true).val('00 00 00000-0000');

            $('#nomeInputHidden').val('AVULSO');
            $('#emailInputHidden').val('email@email.com.br');
            $('#telefoneInputHidden').val('00 00 00000-0000');

        } else {
            $('#nomeInput').prop('disabled', false).val('');
            $('#emailInput').prop('disabled', false).val('');
            $('#telefoneInput').prop('disabled', false).val('55 11 ');

            $('#nomeInputHidden').val('AVULSO');
            $('#emailInputHidden').val('email@email.com.br');
            $('#telefoneInputHidden').val('00 00 00000-0000');

        }
    });
});
function atualizarEstadoPedido(idPedido, novoEstado) {
    // Exibir mensagem de confirmação usando SweetAlert
    Swal.fire({
        title: 'Confirmar pagamento do pedido?',
        text: 'Esta ação não poderá ser desfeita. Deseja prosseguir?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sim, pagar pedido!',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            // Realizar requisição AJAX
            debugger;

            $.ajax({
                type: "POST",
                url: "/Store/AtualizarEstadoPagamento",
                data: { idPedido: idPedido, pago: novoEstado },
                success: function (data) {
                    debugger;

                    Swal.fire({
                        icon: 'success',
                        title: 'Pagamento do pedido atualizado com sucesso.',
                        confirmButtonText: 'OK'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            console.log("Estado do pedido atualizado com sucesso.");
                            location.reload();
                        }
                    });
                },
                error: function (xhr, status, error) {
                    debugger;

                    console.error("Erro ao atualizar estado do pedido:", error);
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro ao atualizar estado do pedido.',
                        text: 'Por favor, tente novamente mais tarde.'
                    });
                }
            });
        } else if (result.dismiss === Swal.DismissReason.cancel) {
            Swal.fire('Operação cancelada', 'Você cancelou o pagamento do pedido.', 'info');
        }
    });
}
function cancelarPedido(idPedido) {
    debugger;
    var novoEstado = $('#PedidoAtivo').val() === 'True' ? false : true; // Inverte o estado atual
    var acao = novoEstado ? "cancelar" : "reativar";

    Swal.fire({
        title: "Deseja " + acao + " o pedido #" + idPedido + "?",
        text: "Esta ação não poderá ser desfeita!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Sim, " + acao.charAt(0).toUpperCase() + acao.slice(1) + " Pedido!"
    }).then((result) => {
        if (result.isConfirmed) {

            $.ajax({
                type: "POST",
                url: "/Store/CancelarPedido",
                data: { idPedido: idPedido, ativo: novoEstado },
                success: function (data) {
                    var estadoAtivo = data.ativo;
                    var estadoPagamento = data.pago;

                    debugger;

                    if (estadoAtivo === false) {
                        Swal.fire({
                            title: 'Pedido Cancelado!',
                            text: 'O pedido foi cancelado com sucesso.',
                            icon: 'success',
                            confirmButtonText: 'OK'
                        }).then((result) => {
                            if (result.isConfirmed) {
                                $('.widget:eq(1)').removeClass('navy-bg yellow-bg').addClass('red-bg');
                                window.location.href = '/Store/PedidoLista?filtro=1';
                            }
                        });
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
                }
            });
        }
    });
}



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