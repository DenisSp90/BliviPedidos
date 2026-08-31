(function () {
    'use strict';

    var numeroPedidosRegistradosConhecido = null;
    var contextoAudio = null;

    function prepararAudio() {
        if (contextoAudio) {
            if (contextoAudio.state === 'suspended') {
                contextoAudio.resume().catch(function () { });
            }
            return;
        }

        var AudioContext = window.AudioContext || window.webkitAudioContext;
        if (AudioContext) {
            contextoAudio = new AudioContext();
        }
    }

    function tocarSomNovoPedido() {
        if (!contextoAudio || contextoAudio.state !== 'running') {
            return;
        }

        var inicio = contextoAudio.currentTime;
        [880, 1175].forEach(function (frequencia, indice) {
            var oscilador = contextoAudio.createOscillator();
            var ganho = contextoAudio.createGain();
            var momento = inicio + (indice * 0.18);

            oscilador.type = 'sine';
            oscilador.frequency.setValueAtTime(frequencia, momento);
            ganho.gain.setValueAtTime(0.0001, momento);
            ganho.gain.exponentialRampToValueAtTime(0.18, momento + 0.02);
            ganho.gain.exponentialRampToValueAtTime(0.0001, momento + 0.16);

            oscilador.connect(ganho);
            ganho.connect(contextoAudio.destination);
            oscilador.start(momento);
            oscilador.stop(momento + 0.17);
        });
    }

    function atualizarNumeroPedidosPendentes() {
    $.ajax({
        url: "/Store/GetInfoPedidos",
        method: "GET",
        success: function (data) {
            $('#numeroPedidos').text(data.numeroTotalPedidos);

            var numeroPedidosRegistrados = Number(data.numeroPedidosRegistrados || 0);
            if (numeroPedidosRegistradosConhecido !== null
                && numeroPedidosRegistrados > numeroPedidosRegistradosConhecido) {
                tocarSomNovoPedido();
            }

            numeroPedidosRegistradosConhecido = numeroPedidosRegistrados;
        },
        error: function (xhr, status, error) {
            console.error("Não foi possível atualizar o contador de pedidos.", error);
        }
    });
    }

    $(document).ready(function () {
        atualizarNumeroPedidosPendentes();
        window.setInterval(atualizarNumeroPedidosPendentes, 30000);

        if ($('#dashboardPedidos').length) {
            document.addEventListener('pointerdown', prepararAudio, { once: true });
            document.addEventListener('keydown', prepararAudio, { once: true });
        }
    });
})();
