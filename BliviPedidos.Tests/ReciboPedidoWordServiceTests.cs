using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xceed.Words.NET;
using Xunit;

namespace BliviPedidos.Tests;

public class ReciboPedidoWordServiceTests
{
    [Fact]
    public void GerarWordA4_CriaDocumentoComDadosDaLojaEDoPedido()
    {
        var ambiente = new Mock<IWebHostEnvironment>();
        ambiente.SetupGet(item => item.WebRootPath).Returns(Path.GetTempPath());
        var servico = new ReciboPedidoWordService(ambiente.Object);
        var loja = new Loja
        {
            Id = 7,
            Nome = "Loja Demonstração",
            EmailContato = "contato@loja.test",
            Whatsapp = "(11) 99999-0000"
        };
        var pedido = new Pedido
        {
            LojaId = loja.Id,
            CodigoPublico = "PEDIDO42TESTE",
            DataPedido = new DateTime(2026, 9, 2, 14, 30, 0),
            Status = StatusPedido.Confirmado,
            StatusPagamento = StatusPagamento.AguardandoPagamento,
            EmailResponsavel = "vendedor@loja.test",
            Cadastro = new Cadastro
            {
                Nome = "Cliente Demonstração",
                Email = "cliente@teste.com",
                Telefone = "(11) 98888-0000",
                ResponsavelCerimar = "Responsável Teste",
                Turma = "Turma A"
            },
            Itens =
            [
                new ItemPedido
                {
                    Quantidade = 2,
                    PrecoUnitario = 35.50m,
                    Produto = new Produto { Nome = "Camiseta Azul", Tamanho = "M" }
                },
                new ItemPedido
                {
                    Quantidade = 1,
                    PrecoUnitario = 18m,
                    Produto = new Produto { Nome = "Caneca Personalizada", Tamanho = "Único" }
                }
            ]
        };
        pedido.ValorTotalPedido = pedido.Itens.Sum(item => item.Subtotal);

        var conteudo = servico.GerarWordA4(pedido, loja);

        Assert.NotEmpty(conteudo);
        using var stream = new MemoryStream(conteudo);
        using var documento = DocX.Load(stream);
        Assert.Contains(loja.Nome, documento.Text);
        Assert.Contains(pedido.CodigoPublico, documento.Text);
        Assert.Contains("Camiseta Azul", documento.Text);

    }
}
