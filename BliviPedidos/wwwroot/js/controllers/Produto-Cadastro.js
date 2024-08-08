document.addEventListener('DOMContentLoaded', function () {
    var precoPagoInput = document.getElementById('PrecoPago');
    var precoVendaInput = document.getElementById('PrecoVenda');

    var precoMaskOptions = {
        mask: Number,
        scale: 2,
        thousandsSeparator: '',
        padFractionalZeros: true,
        normalizeZeros: true,
        radix: ','
    };

    var precoPagoMask = IMask(precoPagoInput, precoMaskOptions);
    var precoVendaMask = IMask(precoVendaInput, precoMaskOptions);

    // Corrige os valores antes de enviar o formulário
    var form = document.querySelector('form');
    form.addEventListener('submit', function () {
        // Remove a formatação e substitui ',' por '.' para garantir que o ASP.NET interprete corretamente como número decimal
        precoPagoInput.value = precoPagoInput.value.replace(/\./g, '').replace(',', '.');
        precoVendaInput.value = precoVendaInput.value.replace(/\./g, '').replace(',', '.');
    });

    formatarCamposEdicao();
});

function formatarCamposEdicao() {
    var precoPagoInput = document.querySelector('.PrecoPagoHidden');
    var precoVendaInput = document.querySelector('.PrecoVendaHidden');

    var precoPagoFormatted = document.getElementById('PrecoPagoFormatted');
    var precoVendaFormatted = document.getElementById('PrecoVendaFormatted');

    if (precoPagoInput && precoVendaInput && precoPagoFormatted && precoVendaFormatted) {
        // Formatar os valores para exibição
        precoPagoFormatted.value = precoPagoInput.value.replace('.', ',');
        precoVendaFormatted.value = precoVendaInput.value.replace('.', ',');

        // Corrige os valores antes de enviar o formulário
        var form = document.querySelector('form');
        form.addEventListener('submit', function () {
            // Remover a formatação e substituir ',' por '.' para garantir que o ASP.NET interprete corretamente como número decimal
            precoPagoInput.value = precoPagoFormatted.value.replace(/\./g, '').replace(',', '.');
            precoVendaInput.value = precoVendaFormatted.value.replace(/\./g, '').replace(',', '.');
        });
    }
}

$(document).ready(function () {
    debugger;

    var nomeProduto = $('#NomeHidden').val();
    $('#NomeProduto').val(nomeProduto);

    var caminhoImagemServidor = $('#imagemPreview').attr('src');
    $('#caminhoImagem').val(caminhoImagemServidor);

    $('#openModal').click(function () {
        $('#imageModal').modal('show');
    });

    $('#inputFoto').change(function () {
        if (this.files && this.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $('#modalPreview').attr('src', e.target.result).show();
            }
            reader.readAsDataURL(this.files[0]);
        }
    });

    $('#saveImage').click(function () {
        var formData = new FormData();
        var file = $('#inputFoto')[0].files[0];
        var produtoId = $('#hiddenProdutoId').val(); // Pega o ID do produto do campo oculto

        if (file) {
            formData.append('Foto', file);

            $.ajax({
                url: '/Store/UploadImagem/' + produtoId, // Envia o ID do produto na URL
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    $('#imagemPreview').attr('src', response.imagemUrl);
                    $('#imageModal').modal('hide');
                    location.reload();
                },
                error: function () {
                    alert('Erro ao salvar a imagem.');
                }
            });
        }
    });
    
});