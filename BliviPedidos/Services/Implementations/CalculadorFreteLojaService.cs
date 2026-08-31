using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;

namespace BliviPedidos.Services.Implementations;

public sealed class CalculadorFreteLojaService : ICalculadorFreteLojaService
{
    public decimal? Calcular(IEnumerable<FaixaFreteLoja> faixas, int distanciaMetros)
    {
        if (distanciaMetros < 0)
            throw new ArgumentOutOfRangeException(nameof(distanciaMetros));

        var distanciaKm = distanciaMetros / 1000m;
        var faixa = faixas
            .Where(item => item.Ativa)
            .OrderBy(item => item.DistanciaInicialKm)
            .FirstOrDefault(item =>
                (item.DistanciaInicialKm == 0m
                    ? distanciaKm >= 0m
                    : distanciaKm > item.DistanciaInicialKm)
                && distanciaKm <= item.DistanciaFinalKm);

        return faixa?.ValorFrete;
    }

    public IReadOnlyCollection<string> Validar(IEnumerable<FaixaFreteLoja> faixas)
    {
        var erros = new List<string>();
        var ordenadas = faixas
            .Where(item => item.Ativa)
            .OrderBy(item => item.DistanciaInicialKm)
            .ThenBy(item => item.DistanciaFinalKm)
            .ToArray();

        for (var indice = 0; indice < ordenadas.Length; indice++)
        {
            var atual = ordenadas[indice];
            if (atual.DistanciaInicialKm < 0m)
                erros.Add("A distância inicial não pode ser negativa.");
            if (atual.DistanciaFinalKm <= atual.DistanciaInicialKm)
                erros.Add("A distância final deve ser maior que a distância inicial.");
            if (atual.ValorFrete < 0m)
                erros.Add("O valor do frete não pode ser negativo.");

            if (indice == 0)
            {
                if (atual.DistanciaInicialKm != 0m)
                    erros.Add("A primeira faixa ativa deve começar em 0 km.");
                continue;
            }

            var anterior = ordenadas[indice - 1];
            if (atual.DistanciaInicialKm < anterior.DistanciaFinalKm)
                erros.Add("As faixas de frete não podem se sobrepor.");
            else if (atual.DistanciaInicialKm > anterior.DistanciaFinalKm)
                erros.Add("As faixas de frete não podem possuir intervalos sem preço.");
        }

        return erros.Distinct().ToArray();
    }
}
