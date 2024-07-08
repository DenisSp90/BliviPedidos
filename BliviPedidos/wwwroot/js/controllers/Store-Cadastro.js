$(document).ready(function () {

    $('#telefoneInput').val('+55 11 ');
    $('#telefoneInput').mask('+00 00 00000-0000');

    $('#telefoneInput').blur(function () {
        var telefone = $(this).val();

        // Fazendo a requisição AJAX para buscar o cliente pelo telefone
        $.ajax({
            url: '/Store/ClienteGetByTelefone', // Ajuste a URL conforme necessário
            type: 'GET',
            data: { telefone: telefone },
            success: function (data) {
                debugger;
                $('#nomeInput').val(data.nome);
                $('#emailInput').val(data.email);
                $('#cep').val(data.cep);




                // Faça algo com os dados retornados
                console.log(data);
                // Você pode atualizar o formulário ou exibir os dados do cliente
            },
            error: function (error) {
                debugger;
                console.error('Erro ao buscar cliente:', error);
            }
        });
    });

    // Quando a página carregar, verifique o estado inicial da checkbox
    if ($('#avulsoCheckbox').is(':checked')) {
        $('#nomeInput').prop('disabled', true).val('AVULSO');
        $('#emailInput').prop('disabled', true).val('email@email.com.br');
        $('#telefoneInput').prop('disabled', true).val('+00 00 00000-0000');
    } else {
        $('#nomeInput').prop('disabled', false).val('');
        $('#emailInput').prop('disabled', false).val('');
        $('#telefoneInput').prop('disabled', false).val('+55 11 ');
    }

    // Adiciona um ouvinte de evento de mudança à checkbox
    $('#avulsoCheckbox').change(function () {
        if (this.checked) {
            $('#nomeInput').prop('disabled', true).val('AVULSO');
            $('#emailInput').prop('disabled', true).val('email@email.com.br');
            $('#telefoneInput').prop('disabled', true).val('+00 00 00000-0000');

            $('#nomeInputHidden').val('AVULSO');
            $('#emailInputHidden').val('email@email.com.br');
            $('#telefoneInputHidden').val('+00 00 00000-0000');           

        } else {
            $('#nomeInput').prop('disabled', false).val('');
            $('#emailInput').prop('disabled', false).val('');
            $('#telefoneInput').prop('disabled', false).val('+55 11 ');

            $('#nomeInputHidden').val('AVULSO');
            $('#emailInputHidden').val('email@email.com.br');
            $('#telefoneInputHidden').val('+00 00 00000-0000');

        }
    });
});

function atualizarEstadoPedido(idPedido, novoEstado) {

    $.ajax({
        type: "POST",
        url: "/Store/AtualizarEstadoPagamento",
        data: { idPedido: idPedido, pago: novoEstado },
        success: function (data) {

            Swal.fire({
                icon: 'success',
                title: 'Pagamento do pedido atualizado com sucesso.',
            });

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

            location.reload();
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
        else {
            location.reload();
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