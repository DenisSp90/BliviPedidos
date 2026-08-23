using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using QRCoder;

namespace BliviPedidos.Services.Implementations;

public sealed class PagamentoPixService : IPagamentoService
{
    public PagamentoPix GerarPix(Loja loja, decimal valor, string referencia)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor do pagamento deve ser maior que zero.");
        if (!loja.PixAtivo)
            throw new InvalidOperationException("O pagamento por PIX não está ativo para esta loja.");
        if (string.IsNullOrWhiteSpace(loja.PixResponsavel)
            || string.IsNullOrWhiteSpace(loja.PixChave)
            || string.IsNullOrWhiteSpace(loja.PixCidade))
            throw new InvalidOperationException("Os dados PIX desta loja não estão configurados.");

        // O txid do PIX aceita até 25 caracteres alfanuméricos. Os novos códigos
        // públicos já seguem essa regra e, portanto, chegam ao banco exatamente
        // como são exibidos para consumidor e balcão.
        var referenciaPix = new string(referencia.Where(char.IsLetterOrDigit).Take(25).ToArray());
        if (string.IsNullOrEmpty(referenciaPix))
            referenciaPix = BitConverter.ToUInt64(
                    SHA256.HashData(Encoding.UTF8.GetBytes(referencia)), 0)
                .ToString(CultureInfo.InvariantCulture);

        var pix = new Pix(
            loja.PixResponsavel,
            ObterTipo(loja.PixTipo),
            loja.PixChave,
            loja.PixCidade,
            referenciaPix,
            valor.ToString("F2", CultureInfo.GetCultureInfo("pt-BR")));
        var payload = pix.GetPayLoad();
        if (payload.Contains("INVÁLIDO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A chave PIX configurada é inválida.");

        using var dadosQrCode = QRCodeGenerator.GenerateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(dadosQrCode).GetGraphic(8);
        return new PagamentoPix("PIX", valor, payload, Convert.ToBase64String(png));
    }

    private static PixModel.PixType ObterTipo(string? tipo) => tipo?.Trim().ToUpperInvariant() switch
    {
        "CPF" => PixModel.PixType.cpf,
        "CNPJ" => PixModel.PixType.cnpj,
        "TELEFONE" => PixModel.PixType.celular,
        "EMAIL" => PixModel.PixType.email,
        _ => PixModel.PixType.chaveAleatoria
    };
}
