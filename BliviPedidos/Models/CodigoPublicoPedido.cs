using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace BliviPedidos.Models;

public static class CodigoPublicoPedido
{
    public const int Tamanho = 22;

    public static string Gerar() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
}
