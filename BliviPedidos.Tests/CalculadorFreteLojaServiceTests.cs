using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class CalculadorFreteLojaServiceTests
{
    private readonly CalculadorFreteLojaService _service = new();

    [Theory]
    [InlineData(0, 7)]
    [InlineData(3000, 7)]
    [InlineData(3001, 10)]
    [InlineData(6000, 10)]
    public void Calcular_DeveAplicarFaixaPorDistanciaEmMetros(int metros, decimal esperado)
    {
        var valor = _service.Calcular(CriarFaixas(), metros);

        Assert.Equal(esperado, valor);
    }

    [Fact]
    public void Calcular_ForaDaArea_DeveRetornarNulo()
    {
        Assert.Null(_service.Calcular(CriarFaixas(), 6001));
    }

    [Fact]
    public void Validar_DeveRejeitarLacunasESobreposicoes()
    {
        var comLacuna = CriarFaixas();
        comLacuna[1].DistanciaInicialKm = 3.1m;
        var comSobreposicao = CriarFaixas();
        comSobreposicao[1].DistanciaInicialKm = 2.9m;

        Assert.Contains(_service.Validar(comLacuna), erro => erro.Contains("intervalos"));
        Assert.Contains(_service.Validar(comSobreposicao), erro => erro.Contains("sobrepor"));
    }

    private static List<FaixaFreteLoja> CriarFaixas() =>
    [
        new() { DistanciaInicialKm = 0m, DistanciaFinalKm = 3m, ValorFrete = 7m },
        new() { DistanciaInicialKm = 3m, DistanciaFinalKm = 6m, ValorFrete = 10m }
    ];
}
