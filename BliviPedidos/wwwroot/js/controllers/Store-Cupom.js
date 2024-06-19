function abrirPopup() {
    var conteudoCupom = document.querySelector('.cupom').innerHTML;
    var popup = window.open('', 'CupomPopUp', 'width=600,height=400');
    popup.document.write('<html><head><title>Cupom Fiscal</title></head><body>');
    popup.document.write(conteudoCupom);
    popup.document.write('</body></html>');
    popup.document.close();
    popup.onload = function () {
        popup.print();
    };
}