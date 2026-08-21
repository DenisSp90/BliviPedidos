using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public class PedidoServiceTests
{
    [Fact]
    public async Task RegistrarCancelamentoPedido_DeveInativarPedidoERestaurarEstoque()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produto = new Produto
        {
            Nome = "Produto de teste",
            PrecoPago = 5m,
            PrecoVenda = 10m,
            Quantidade = 7,
            IsAtivo = true
        };
        var pedido = new Pedido
        {
            Ativo = true,
            Pago = true,
            ValorTotalPedido = 20m,
            Cadastro = new Cadastro
            {
                Nome = "Cliente Teste",
                Telefone = "55 11 99999-9999"
            }
        };
        var item = new ItemPedido(pedido, produto, 2, produto.PrecoVenda);
        pedido.Itens.Add(item);

        context.Add(pedido);
        await context.SaveChangesAsync();

        ProdutoMovimentacao? movimentacaoRegistrada = null;
        var produtoService = new Mock<IProdutoService>();
        produtoService
            .Setup(service => service.RegistrarMovimentacaoAsync(It.IsAny<ProdutoMovimentacao>()))
            .Callback<ProdutoMovimentacao>(movimentacao => movimentacaoRegistrada = movimentacao)
            .ReturnsAsync(1);

        var httpContextAccessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            httpContextAccessor,
            context,
            Mock.Of<IItemPedidoService>(),
            Mock.Of<ICadastroService>(),
            produtoService.Object,
            httpContextAccessor,
            NullLogger<PedidoService>.Instance);

        await service.RegistrarCancelamentoPedido(pedido.Id);

        context.ChangeTracker.Clear();
        var pedidoAtualizado = await context.Pedido
            .Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .SingleAsync(p => p.Id == pedido.Id);

        Assert.False(pedidoAtualizado.Ativo);
        Assert.False(pedidoAtualizado.Pago);
        Assert.Equal(0m, pedidoAtualizado.ValorTotalPedido);
        Assert.Equal(9, pedidoAtualizado.Itens.Single().Produto.Quantidade);

        Assert.NotNull(movimentacaoRegistrada);
        Assert.Equal(produto.Id, movimentacaoRegistrada.ProdutoId);
        Assert.Equal(2, movimentacaoRegistrada.Quantidade);
        Assert.Equal("Entrada", movimentacaoRegistrada.Tipo);
        Assert.Contains("PEDIDO-CANCELAMENTO", movimentacaoRegistrada.Observacao);
        produtoService.Verify(
            service => service.RegistrarMovimentacaoAsync(It.IsAny<ProdutoMovimentacao>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarCancelamentoPedido_ComPedidoInexistente_DeveFalharSemMovimentarEstoque()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produtoService = new Mock<IProdutoService>();
        var httpContextAccessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            httpContextAccessor,
            context,
            Mock.Of<IItemPedidoService>(),
            Mock.Of<ICadastroService>(),
            produtoService.Object,
            httpContextAccessor,
            NullLogger<PedidoService>.Instance);

        var exception = await Assert.ThrowsAsync<Exception>(
            () => service.RegistrarCancelamentoPedido(999));

        Assert.Equal("Pedido não encontrado", exception.Message);
        produtoService.Verify(
            service => service.RegistrarMovimentacaoAsync(It.IsAny<ProdutoMovimentacao>()),
            Times.Never);
    }

    private static IHttpContextAccessor CriarHttpContextAccessor(string usuario)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, usuario) },
            "TestAuthentication");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
