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
    $('#inputFoto').change(function () {
        var reader = new FileReader();
        reader.onload = function (e) {
            $('#imagemPreview').attr('src', e.target.result).show();
        }
        reader.readAsDataURL(this.files[0]);

        // Preencher o campo ProdutoViewModel.Foto
        var file = $(this)[0].files[0];
        var formData = new FormData();
        formData.append('Foto', file);
    });
});