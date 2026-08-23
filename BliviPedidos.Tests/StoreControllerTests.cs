using System.Security.Claims;
using AutoMapper;
using BliviPedidos.Controllers;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public class StoreControllerTests
{
    [Fact]
    public async Task PedidoResumo_ComDadosValidos_DeveConcluirPedidoEBaixarEstoque()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new ApplicationDbContext(options);

        var produto = new Produto
        {
            Nome = "Produto de teste",
            PrecoVenda = 15m,
            Quantidade = 10
        };
        var pedido = new Pedido();
        pedido.Itens.Add(new ItemPedido(pedido, produto, 2, produto.PrecoVenda));

        var cadastro = new Cadastro
        {
            Nome = "Cliente Teste",
            Email = string.Empty,
            Telefone = "55 11 99999-9999"
        };

        var pedidoService = new Mock<IPedidoService>();
        pedidoService.Setup(service => service.GetPedido()).Returns(pedido);
        pedidoService.Setup(service => service.UpdateCadastro(cadastro)).Returns(pedido);

        var produtoService = new Mock<IProdutoService>();
        produtoService.Setup(service => service.UpdateQuantidade(pedido.Itens)).Returns(true);

        var clienteService = new Mock<IClienteService>();
        clienteService
            .Setup(service => service.ProcurarClienteByTelefoneAsync(cadastro.Telefone))
            .ReturnsAsync(new ClienteViewModel { Id = 10, Nome = cadastro.Nome });

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(service => service.Map<Cliente>(It.IsAny<ClienteViewModel>()))
            .Returns(new Cliente { Nome = cadastro.Nome });

        var controller = new StoreController(
            context,
            mapper.Object,
            pedidoService.Object,
            produtoService.Object,
            Mock.Of<IEmailEnviarService>(),
            clienteService.Object,
            Mock.Of<IItemPedidoService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ICategoriaService>(),
            NullLogger<StoreController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CriarHttpContext("operador@teste.com")
            }
        };

        var result = await controller.PedidoResumo(cadastro);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PedidoDetalhe", redirect.ActionName);
        Assert.Equal(StatusPedido.Confirmado, pedido.Status);
        Assert.Equal(StatusPagamento.AguardandoPagamento, pedido.StatusPagamento);
        Assert.Equal("operador@teste.com", pedido.EmailResponsavel);
        Assert.NotNull(pedido.DataPedido);
        Assert.Equal(30m, pedido.ValorTotalPedido);
        Assert.Equal(10, cadastro.ClienteId);

        produtoService.Verify(service => service.UpdateQuantidade(pedido.Itens), Times.Once);
        pedidoService.Verify(service => service.UpdateCadastro(cadastro), Times.Once);
        pedidoService.Verify(service => service.ClearPedido(), Times.Once);
    }

    [Fact]
    public async Task PedidoResumo_SemEstoqueSuficiente_NaoDeveConcluirPedido()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new ApplicationDbContext(options);

        var produto = new Produto { Nome = "Produto", PrecoVenda = 10m };
        var pedido = new Pedido();
        pedido.Itens.Add(new ItemPedido(pedido, produto, 2, produto.PrecoVenda));
        var cadastro = new Cadastro
        {
            Nome = "Cliente Teste",
            Telefone = "55 11 99999-9999"
        };

        var pedidoService = new Mock<IPedidoService>();
        pedidoService.Setup(service => service.GetPedido()).Returns(pedido);
        var produtoService = new Mock<IProdutoService>();
        produtoService.Setup(service => service.UpdateQuantidade(pedido.Itens)).Returns(false);

        var controller = new StoreController(
            context,
            Mock.Of<IMapper>(),
            pedidoService.Object,
            produtoService.Object,
            Mock.Of<IEmailEnviarService>(),
            Mock.Of<IClienteService>(),
            Mock.Of<IItemPedidoService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ICategoriaService>(),
            NullLogger<StoreController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CriarHttpContext("operador@teste.com")
            },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var result = await controller.PedidoResumo(cadastro);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PedidoCadastro", redirect.ActionName);
        Assert.Equal(StatusPedido.Carrinho, pedido.Status);
        pedidoService.Verify(service => service.UpdateCadastro(It.IsAny<Cadastro>()), Times.Never);
        pedidoService.Verify(service => service.ClearPedido(), Times.Never);
    }

    [Fact]
    public async Task PedidoResumo_VendaAvulsa_DeveUsarDadosDaLojaFisicaSemCadastrarCliente()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new ApplicationDbContext(options);

        var produto = new Produto { Nome = "Produto", PrecoVenda = 12m, Quantidade = 5 };
        var pedido = new Pedido();
        pedido.Itens.Add(new ItemPedido(pedido, produto, 1, produto.PrecoVenda));
        var cadastro = new Cadastro { VendaAvulsa = true };

        var pedidoService = new Mock<IPedidoService>();
        pedidoService.Setup(service => service.GetPedido()).Returns(pedido);
        pedidoService.Setup(service => service.UpdateCadastro(cadastro)).Returns(pedido);
        var produtoService = new Mock<IProdutoService>();
        produtoService.Setup(service => service.UpdateQuantidade(pedido.Itens)).Returns(true);
        var clienteService = new Mock<IClienteService>();

        var controller = new StoreController(
            context,
            Mock.Of<IMapper>(),
            pedidoService.Object,
            produtoService.Object,
            Mock.Of<IEmailEnviarService>(),
            clienteService.Object,
            Mock.Of<IItemPedidoService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ICategoriaService>(),
            NullLogger<StoreController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CriarHttpContext("balcao@teste.com")
            }
        };

        var result = await controller.PedidoResumo(cadastro);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Consumidor avulso - loja física", cadastro.Nome);
        Assert.Equal("Não informado", cadastro.Telefone);
        Assert.Null(cadastro.ClienteId);
        Assert.NotNull(pedido.CodigoPublico);
        clienteService.Verify(
            service => service.ProcurarClienteByTelefoneAsync(It.IsAny<string>()), Times.Never);
        clienteService.Verify(
            service => service.RegistrarClienteAsync(It.IsAny<Cadastro>()), Times.Never);
        pedidoService.Verify(service => service.UpdateCadastro(cadastro), Times.Once);
    }

    private static DefaultHttpContext CriarHttpContext(string usuario)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, usuario) },
            "TestAuthentication");
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }
}
