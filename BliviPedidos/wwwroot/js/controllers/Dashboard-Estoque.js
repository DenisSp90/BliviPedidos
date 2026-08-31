(function () {
    'use strict';

    function obterDados() {
        var elemento = document.getElementById('dashboardEstoqueDados');
        if (!elemento) {
            return null;
        }

        try {
            return JSON.parse(elemento.textContent);
        } catch (erro) {
            console.error('Não foi possível interpretar os dados do estoque.', erro);
            return null;
        }
    }

    function exibirSemDados(elemento, mensagem) {
        elemento.innerHTML = '<div class="d-flex align-items-center justify-content-center text-muted h-100 py-5">'
            + mensagem
            + '</div>';
    }

    function renderizarProdutosMaisVendidos(itens) {
        var elemento = document.getElementById('graficoProdutosMaisVendidos');
        if (!elemento) {
            return;
        }

        if (!itens.length) {
            exibirSemDados(elemento, 'Ainda não existem vendas registradas nos últimos 30 dias.');
            return;
        }

        new ApexCharts(elemento, {
            chart: { type: 'bar', height: 340, toolbar: { show: false } },
            series: [{ name: 'Unidades vendidas', data: itens.map(function (item) { return item.valor; }) }],
            xaxis: { categories: itens.map(function (item) { return item.rotulo; }) },
            plotOptions: { bar: { borderRadius: 4, horizontal: true } },
            dataLabels: { enabled: true },
            colors: ['#4154f1'],
            tooltip: { y: { formatter: function (valor) { return valor + ' unidade(s)'; } } }
        }).render();
    }

    function renderizarEstoquePorCategoria(itens) {
        var elemento = document.getElementById('graficoEstoqueCategoria');
        if (!elemento) {
            return;
        }

        if (!itens.length) {
            exibirSemDados(elemento, 'Nenhum produto ativo foi encontrado.');
            return;
        }

        new ApexCharts(elemento, {
            chart: { type: 'donut', height: 340 },
            series: itens.map(function (item) { return item.valor; }),
            labels: itens.map(function (item) { return item.rotulo; }),
            legend: { position: 'bottom' },
            dataLabels: { enabled: true },
            tooltip: { y: { formatter: function (valor) { return valor + ' unidade(s)'; } } },
            responsive: [{
                breakpoint: 576,
                options: { chart: { height: 300 }, legend: { position: 'bottom' } }
            }]
        }).render();
    }

    document.addEventListener('DOMContentLoaded', function () {
        var dados = obterDados();
        if (!dados || typeof ApexCharts === 'undefined') {
            return;
        }

        renderizarProdutosMaisVendidos(dados.produtosMaisVendidos || []);
        renderizarEstoquePorCategoria(dados.estoquePorCategoria || []);
    });
})();
