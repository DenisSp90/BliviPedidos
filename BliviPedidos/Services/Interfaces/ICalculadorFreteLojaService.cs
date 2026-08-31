using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface ICalculadorFreteLojaService
{
    decimal? Calcular(IEnumerable<FaixaFreteLoja> faixas, int distanciaMetros);
    IReadOnlyCollection<string> Validar(IEnumerable<FaixaFreteLoja> faixas);
}
