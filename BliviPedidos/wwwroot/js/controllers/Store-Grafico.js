$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart'] });
    google.charts.setOnLoadCallback(CarregaDados);

    function CarregaDados() {
        var totalValorPedidos = parseFloat($("#total-valor-pedidos").text().replace('$', '').replace(',', '').trim()).toFixed(2);
        var valorPedidosPagos = parseFloat($("#valor-pedidos-pagos").text().replace('$', '').replace(',', '').trim()).toFixed(2);
        var valorPedidosNaoPagos = parseFloat($("#valor-pedidos-nao-pagos").text().replace('$', '').replace(',', '').trim()).toFixed(2);

        var data1 = [
            ['Status de Pedido', 'Valor', { role: 'style' }],
            ['Total', parseFloat(totalValorPedidos), '#36A2EB33'],
            ['Pagos', parseFloat(valorPedidosPagos), '#48c3aa'],
            ['Não Pagos', parseFloat(valorPedidosNaoPagos), '#FF6384']
        ];

        GraficoParticipacaoComercio(data1);

        // Atualização dos valores exibidos
        var totalValorPedidos = parseFloat($("#total-valor-pedidos").text().replace('R$', '').replace(',', '').trim()).toFixed(2);
        var valorPedidosPagos = parseFloat($("#valor-pedidos-pagos").text().replace('R$', '').replace(',', '').trim()).toFixed(2);
        var valorPedidosNaoPagos = parseFloat($("#valor-pedidos-nao-pagos").text().replace('R$', '').replace(',', '').trim()).toFixed(2);
    }

    function GraficoParticipacaoComercio(dataValues) {
        var data = google.visualization.arrayToDataTable(dataValues);

        var formatter = new google.visualization.NumberFormat({
            pattern: '#,##0.00' // Define o padrão de formatação da moeda
        });
        formatter.format(data, 1); // Aplica a formatação à segunda coluna (o eixo vertical)

        var options = {
            title: 'Resumo dos Pedidos',
            chartArea: { width: '50%' },
            hAxis: {
                title: 'Pedidos',
                minValue: 0
            },
            vAxis: {
                title: 'Status'
            }
        };

        var chart = new google.visualization.BarChart(document.getElementById('bar_chart'));
        chart.draw(data, options);
    }

});
