// Quando o modal for acionado
var itemModal = document.getElementById('itemModal');

itemModal.addEventListener('show.bs.modal', function (event) {
    // Botão que disparou o modal
    var button = event.relatedTarget;

    // Extrair os dados do botão (data-* attributes)
    var itemPedidoId = button.getAttribute('data-itempedido');
    var produtoId = button.getAttribute('data-produtoId');

    var nome = button.getAttribute('data-nome');
    var quantidade = button.getAttribute('data-quantidade');
    var preco = button.getAttribute('data-preco');
    var subtotal = button.getAttribute('data-subtotal');

    // Atualizar o conteúdo do modal com os dados extraídos
    document.getElementById('modalItemPedidoId').textContent = itemPedidoId;
    document.getElementById('modalProdutoId').textContent = produtoId;
    document.getElementById('modalProdutoNome').textContent = nome;
    document.getElementById('modalProdutoQuantidade').value = quantidade;
    document.getElementById('modalProdutoPreco').textContent = preco;
    document.getElementById('modalProdutoSubtotal').textContent = subtotal;
});

// Função para buscar itens
document.getElementById('searchButton').addEventListener('click', function () {
    var query = document.getElementById('searchInput').value;

    // Chamar uma função para buscar os itens (exemplo fictício)
    searchItems(query);
});

// Função fictícia para buscar itens
function searchItems(query) {
    $.ajax({
        url: '/Store/ItemsSearch',  // Altere conforme necessário
        type: 'GET',
        data: { query: query },
        success: function (data) {

            var resultsList = document.getElementById('resultsList');
            resultsList.innerHTML = '';  // Limpa os resultados anteriores

            data.forEach(function (item) {

                var listItem = document.createElement('li');
                listItem.className = 'list-group-item';
                listItem.textContent = item.nome + ' - Quantidade: ' + item.quantidade;

                // Adiciona um botão para selecionar o novo item
                var selectButton = document.createElement('button');
                selectButton.className = 'btn btn-primary btn-sm float-end';
                selectButton.textContent = 'Selecionar';
                selectButton.addEventListener('click', function () {
                    // Atualiza os campos do modal com o novo item selecionado

                    document.getElementById('modalProdutoId').textContent = item.id;
                    document.getElementById('modalProdutoNome').textContent = item.nome;
                    document.getElementById('modalProdutoQuantidade').textContent = '1';
                    document.getElementById('modalProdutoPreco').textContent = item.precoVenda.toFixed(2);;  // Exemplo de preço
                    document.getElementById('modalProdutoSubtotal').textContent = 'R$ ' + (1 * item.precoVenda).toFixed(2);

                    // Oculta os resultados da pesquisa
                    document.getElementById('searchResults').style.display = 'none';
                });

                listItem.appendChild(selectButton);
                resultsList.appendChild(listItem);
            });

            // Exibir a seção de resultados da pesquisa
            document.getElementById('searchResults').style.display = 'block';
        }
    });
}

// Atualizar subtotal ao editar a quantidade
document.getElementById('modalProdutoQuantidade').addEventListener('input', function () {
    var quantidade = this.value;
    var preco = parseFloat(document.getElementById('modalProdutoPreco').textContent.replace('R$', '').replace(',', '.'));

    var subtotal = quantidade * preco;
    document.getElementById('modalProdutoSubtotal').textContent = 'R$ ' + subtotal.toFixed(2);
});

// Função para salvar alterações (exemplo)
document.getElementById('saveButton').addEventListener('click', function () {

    var itemPedidoId = document.getElementById('modalItemPedidoId').textContent;
    var produtoId = document.getElementById('modalProdutoId').textContent;

    var produtoNome = document.getElementById('modalProdutoNome').textContent;
    var quantidade = document.getElementById('modalProdutoQuantidade').value;
    var preco = document.getElementById('modalProdutoPreco').textContent;
    var subtotal = document.getElementById('modalProdutoSubtotal').textContent;

    Swal.fire({
        title: 'Você tem certeza?',
        text: "Esta operação será registrada na movimentação de estoque e pedidos.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sim, alterar!',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            // Executa o AJAX caso o usuário confirme a ação

            $.ajax({
                url: '/Store/UpdateQuantidade2',  // Altere para sua rota
                type: 'POST',
                data: {
                    itemPedidoId: itemPedidoId,
                    produtoId: produtoId,
                    quantidade: quantidade,
                    preco: preco
                },
                success: function (response) {
                    Swal.fire(
                        'Alterado!',
                        'A quantidade foi alterada com sucesso.',
                        'success'
                    );
                    $('#itemModal').modal('hide');
                    location.reload();
                },
                error: function (error) {
                    Swal.fire(
                        'Erro!',
                        'Ocorreu um erro ao tentar atualizar a quantidade.',
                        'error'
                    );
                    console.error('Erro ao atualizar quantidade: ', error);
                }
            });
        }
    });

});
