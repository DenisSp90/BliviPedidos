$(document).ready(function () {
    $('.adicionar-produto').click(function (e) {
        e.preventDefault();
        var produtoId = $(this).data('produto-id');

        $.ajax({
            url: '/Store/Carrinho/' + produtoId,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ produto: produtoId }),
            success: function (response) {
                $('#tblPedidos tbody').empty();
                var valoresDoCampo;
                var total = 0;
                var quantidadeItensPedido = 0;

                var listaItens = response.listaItens;
                var carrinhoViewModel = response.carrinhoViewModel;

                debugger;

                $.each(listaItens, function (index, item) {
                    valoresDoCampo = item.pedido.id;
                    total += parseFloat(item.subtotal);
                    
                    var row = '<tr item-id=' + item.id + ' class="table-primary">' +
                        '<td>' + item.produto.nome + '</td>' +
                        '<td>' +
                        '<input type="text" value="' + item.quantidade + '" style="width: 4em; text-align: center;" class="form-control text-center col-md-4 update-quantidade" onblur="updateQuantidade(this)" oninput="this.value = this.value.replace(/[^0-9]/g, \'\');" pattern="\d*"/>' +
                        '</td>' +
                        '<td class="subtotal">' + parseFloat(item.subtotal).toFixed(2) + '</td>' +
                        '</tr>';

                    $('#tblPedidos tbody').append(row);
                    quantidadeItensPedido++;
                });

                $('#quantidadeItens').text(quantidadeItensPedido);
                $('#numeroPedido').text(valoresDoCampo);
                $('#total').text(carrinhoViewModel.total.toFixed(2));

                Swal.fire({
                    position: "top-end",
                    icon: "success",
                    title: "Produto adicionado ao pedido com sucesso"
                });
            },
            error: function (xhr, status, error) {
                var mensagemErro = xhr.responseJSON ? xhr.responseJSON : "Ocorreu um erro ao adicionar o produto ao carrinho. Por favor, tente novamente mais tarde.";

                Swal.fire({
                    icon: 'error',
                    title: 'Oops...',
                    text: mensagemErro
                });
            }
        });
    });

    $(document).on('blur', '.update-quantidade', function () {
        var valor = parseFloat($(this).val());
        updateQuantidade(this);
    });
});

function updateQuantidade(input) {
    let data = this.getData(input);
    this.postQuantidade(data);
}

function getData(elemento) {
    debugger;

    var linhaDoItem = $(elemento).closest('[item-id]'); // Alterado para closest() para encontrar o ancestral mais próximo com o atributo 'item-id'
    var itemId = linhaDoItem.attr('item-id');
    var novaQuantidade = linhaDoItem.find('input').val();

    return {
        Id: itemId,
        Quantidade: novaQuantidade
    };
}

function postQuantidade(data) {
    $.ajax({
        url: '/Store/UpdateQuantidade',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            debugger;
            var itemPedido = response.itemPedido;
            var carrinhoViewModel = response.carrinhoViewModel;
            var linhaDoItem = $('[item-id="' + itemPedido.id + '"]');

            linhaDoItem.find('input').val(itemPedido.quantidade);
            linhaDoItem.find('.subtotal').text(itemPedido.subtotal.toFixed(2));
            $('[numero-itens]').html('Total: ' + carrinhoViewModel.itens.length + ' itens');
            $('#quantidadeItens').text(carrinhoViewModel.itens.length);
            $('#total').text(carrinhoViewModel.total.toFixed(2));

            if (itemPedido.quantidade === 0) {
                linhaDoItem.remove();
            }
        },
        error: function (xhr, status, error) {
            console.error(xhr.responseText); // Se houver um erro, exibe no console
        }
    });
}
