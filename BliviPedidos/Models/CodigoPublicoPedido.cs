using System.Security.Cryptography;

namespace BliviPedidos.Models;

public static class CodigoPublicoPedido
{
    public const int Tamanho = 22;
    private const string CaracteresPermitidos =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string Gerar()
    {
        Span<char> codigo = stackalloc char[Tamanho];
        for (var indice = 0; indice < codigo.Length; indice++)
        {
            codigo[indice] = CaracteresPermitidos[
                RandomNumberGenerator.GetInt32(CaracteresPermitidos.Length)];
        }

        return new string(codigo);
    }
}
