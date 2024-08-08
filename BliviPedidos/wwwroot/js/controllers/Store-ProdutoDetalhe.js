$(document).ready(function () {
    debugger;

    $('#imprimirEtiquetaModal').on('show.bs.modal', function (event) {
        debugger;
        var button = $(event.relatedTarget); // Botão que acionou o modal
        var produtoId = button.data('id'); // Extrai informação dos atributos data-*

        // Carregar os dados do produto via AJAX ou de outra forma
        // Por exemplo, você pode definir o nome do produto dinamicamente no modal
        var modal = $(this);
        modal.find('#produtoNome').text('Nome do Produto ' + produtoId);

        // Se necessário, você pode fazer uma requisição AJAX para obter os detalhes do produto
        $.ajax({
            url: '/api/StoreApi/produto/' + produtoId,
            method: 'GET',
            success: function (data) {
                debugger;
                modal.find('#produtoNome').text(data.nome);
            },
            error: function () {
                modal.find('#produtoNome').text('Erro ao carregar o produto');
            }
        });
    });

    $('#confirmarImpressao').click(function () {
        var produtoId = $('#imprimirEtiquetaModal').find('button[data-id]').data('id');
        var url = '/api/StoreApi/etiqueta/pdf/' + produtoId;
        window.open(url, '_blank');
    });
});