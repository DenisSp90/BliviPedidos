var custoInput = $('#Preco');
var custoMask = new IMask(custoInput[0], {
    mask: Number,
    scale: 2,
    signed: false,
    thousandsSeparator: '.',
    padFractionalZeros: true,
    normalizeZeros: true,
    radix: ',',
    mapToRadix: [','],
    mapToNumber: {
        ',': '',
        '.': ''
    },
    pattern: '$num'
});
$(document).ready(function () {
    $('#inputFoto').change(function () {
        debugger;
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