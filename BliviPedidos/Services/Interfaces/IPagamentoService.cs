namespace BliviPedidos.Services.Interfaces;

using BliviPedidos.Models;

public interface IPagamentoService
{
    PagamentoPix GerarPix(Loja loja, decimal valor, string referencia);
}

public sealed record PagamentoPix(string Tipo, decimal Valor, string CopiaECola, string QrCodeBase64);
