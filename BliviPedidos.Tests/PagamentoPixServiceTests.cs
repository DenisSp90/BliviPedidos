using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class PagamentoPixServiceTests
{
    [Fact]
    public void GerarPix_ComConfiguracaoValida_DeveGerarPayloadEImagem()
    {
        var service = new PagamentoPixService();
        var loja = new Loja
        {
            PixAtivo = true,
            PixResponsavel = "LOJA TESTE",
            PixTipo = "Email",
            PixChave = "pix@loja.test",
            PixCidade = "SAO PAULO"
        };

        var codigoPublico = CodigoPublicoPedido.Gerar();
        var pagamento = service.GerarPix(loja, 49.90m, codigoPublico);

        Assert.Equal("PIX", pagamento.Tipo);
        Assert.Equal(49.90m, pagamento.Valor);
        Assert.Contains("br.gov.bcb.pix", pagamento.CopiaECola);
        Assert.Contains($"05{CodigoPublicoPedido.Tamanho:00}{codigoPublico}", pagamento.CopiaECola);
        Assert.NotEmpty(Convert.FromBase64String(pagamento.QrCodeBase64));
    }

    [Fact]
    public void GerarPix_SemConfiguracao_DeveFalharDeFormaControlada()
    {
        var service = new PagamentoPixService();

        var erro = Assert.Throws<InvalidOperationException>(() =>
            service.GerarPix(new Loja { PixAtivo = true }, 10m, "123"));

        Assert.Contains("não estão configurados", erro.Message);
    }
}
