document.addEventListener('DOMContentLoaded', function () {
    var precoPagoInput = document.querySelector('#PrecoPago:not([type="hidden"])');
    var precoVendaInput = document.querySelector('#PrecoVenda:not([type="hidden"])');

    var precoMaskOptions = {
        mask: Number,
        scale: 2,
        thousandsSeparator: '',
        padFractionalZeros: true,
        normalizeZeros: true,
        radix: ','
    };

    if (precoPagoInput && precoVendaInput) {
        IMask(precoPagoInput, precoMaskOptions);
        IMask(precoVendaInput, precoMaskOptions);

        // Corrige os valores antes de enviar o formulário
        var form = document.querySelector('form');
        form.addEventListener('submit', function () {
            precoPagoInput.value = precoPagoInput.value.replace(/\./g, '').replace(',', '.');
            precoVendaInput.value = precoVendaInput.value.replace(/\./g, '').replace(',', '.');
        });
    }

    formatarCamposEdicao();
    configurarOrigemImagem();
});

function configurarOrigemImagem() {
    var opcoes = document.querySelectorAll('.tipo-imagem');
    var campoUpload = document.querySelector('.campo-imagem-upload');
    var campoUrl = document.querySelector('.campo-imagem-url');
    var arquivo = document.getElementById('FotoArquivo');
    var url = document.getElementById('FotoUrl');
    var preview = document.getElementById('imagemPreview');

    if (!opcoes.length || !campoUpload || !campoUrl) return;

    function atualizarCampos() {
        var selecionada = document.querySelector('.tipo-imagem:checked');
        var usarUrl = selecionada && selecionada.value === 'Url';
        campoUpload.classList.toggle('d-none', usarUrl);
        campoUrl.classList.toggle('d-none', !usarUrl);
        if (arquivo) arquivo.disabled = usarUrl;
        if (url) url.disabled = !usarUrl;
    }

    opcoes.forEach(function (opcao) {
        opcao.addEventListener('change', atualizarCampos);
    });

    if (arquivo) {
        arquivo.addEventListener('change', function () {
            var imagem = this.files && this.files[0];
            if (imagem && preview) preview.src = URL.createObjectURL(imagem);
        });
    }

    if (url && preview) {
        url.addEventListener('input', function () {
            if (this.value) preview.src = this.value;
        });
    }

    atualizarCampos();
}

function formatarCamposEdicao() {
    var precoPago = document.getElementById('PrecoPagoEdicao');
    var precoVenda = document.getElementById('PrecoVendaEdicao');

    if (precoPago && precoVenda) {
        var precoMaskOptions = {
            mask: Number,
            scale: 2,
            thousandsSeparator: '',
            padFractionalZeros: true,
            normalizeZeros: true,
            radix: ','
        };

        IMask(precoPago, precoMaskOptions);
        IMask(precoVenda, precoMaskOptions);
    }
}

$(document).ready(function () {
    var nomeProduto = $('#NomeHidden').val();
    $('#NomeProduto').val(nomeProduto);
});
