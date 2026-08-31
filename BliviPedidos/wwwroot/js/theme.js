(function () {
    'use strict';

    var chaveTema = 'blivi-tema';
    var raiz = document.documentElement;

    function temaEscuroAtivo() {
        return raiz.getAttribute('data-theme') === 'dark';
    }

    function atualizarBotao() {
        var botao = document.getElementById('alternarTema');
        if (!botao) return;

        var escuro = temaEscuroAtivo();
        var texto = escuro ? 'Ativar tema claro' : 'Ativar tema escuro';
        var icone = botao.querySelector('i');

        botao.setAttribute('aria-label', texto);
        botao.setAttribute('title', texto);
        botao.setAttribute('aria-pressed', escuro ? 'true' : 'false');
        if (icone) icone.className = escuro ? 'bi bi-sun' : 'bi bi-moon-stars';
    }

    function alternarTema() {
        var novoTema = temaEscuroAtivo() ? 'claro' : 'escuro';

        if (novoTema === 'escuro') {
            raiz.setAttribute('data-theme', 'dark');
        } else {
            raiz.removeAttribute('data-theme');
        }

        localStorage.setItem(chaveTema, novoTema);
        atualizarBotao();
        window.dispatchEvent(new CustomEvent('blivi:tema-alterado', { detail: { tema: novoTema } }));
    }

    document.addEventListener('DOMContentLoaded', function () {
        var botao = document.getElementById('alternarTema');
        if (botao) botao.addEventListener('click', alternarTema);
        atualizarBotao();
    });
})();
