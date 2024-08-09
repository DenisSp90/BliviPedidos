$(document).ready(function () {
    debugger;

    $('#imprimirEtiquetaModal').on('show.bs.modal', function (event) {
        debugger;
        var button = $(event.relatedTarget); // Bot�o que acionou o modal
        var produtoId = button.data('id'); // Extrai informa��o dos atributos data-*
        var modal = $(this);
        modal.find('#confirmarImpressao').data('id', produtoId);
        modal.find('#produtoNome').text('Nome do Produto ' + produtoId);

        // Se necess�rio, voc� pode fazer uma requisi��o AJAX para obter os detalhes do produto
        $.ajax({
            url: '/api/StoreApi/produto/' + produtoId,
            method: 'GET',
            success: function (data) {
                modal.find('#produtoNome').text(data.nome);
            },
            error: function () {
                modal.find('#produtoNome').text('Erro ao carregar o produto');
            }
        });
    });

    $('#confirmarImpressao').click(function () {
        debugger;
        var produtoId = $(this).data('id');
        var url = '/api/StoreApi/etiqueta/pdf/' + produtoId;
        window.open(url, '_blank');
    });
});