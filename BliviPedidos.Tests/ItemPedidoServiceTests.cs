using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class ItemPedidoServiceTests
{
    [Fact]
    public async Task UpdateItemPedidoAsync_DeveTrocarProdutoEstoqueTotalEMovimentacoes()
    {
        await using var cenario = await CriarCenarioAsync();

        await cenario.Service.UpdateItemPedidoAsync(
            cenario.Item.Id, cenario.NovoProduto.Id, 3, 0.01m);

        cenario.Context.ChangeTracker.Clear();
        var item = await cenario.Context.Set<ItemPedido>().SingleAsync(i => i.Id == cenario.Item.Id);
        var pedido = await cenario.Context.Pedido.SingleAsync(p => p.Id == cenario.Pedido.Id);
        var produtoAnterior = await cenario.Context.Produto.SingleAsync(p => p.Id == cenario.ProdutoAnterior.Id);
        var produtoNovo = await cenario.Context.Produto.SingleAsync(p => p.Id == cenario.NovoProduto.Id);
        var movimentacoes = await cenario.Context.ProdutoMovimentacao
            .Where(m => m.PedidoId == cenario.Pedido.Id)
            .ToListAsync();

        Assert.Equal(cenario.NovoProduto.Id, item.ProdutoId);
        Assert.Equal(3, item.Quantidade);
        Assert.Equal(20m, item.PrecoUnitario); // Nunca confia no preço recebido da tela.
        Assert.Equal(60m, pedido.ValorTotalPedido);
        Assert.Equal(7, produtoAnterior.Quantidade);
        Assert.Equal(7, produtoNovo.Quantidade);
        Assert.Equal(2, movimentacoes.Count);
        Assert.All(movimentacoes, m => Assert.Equal(OrigemMovimentacaoEstoque.AlteracaoPedido, m.Origem));
    }

    [Fact]
    public async Task UpdateItemPedidoAsync_MesmoProduto_DeveConsiderarQuantidadeJaReservada()
    {
        await using var cenario = await CriarCenarioAsync(estoqueProdutoAnterior: 1);

        await cenario.Service.UpdateItemPedidoAsync(
            cenario.Item.Id, cenario.ProdutoAnterior.Id, 3, cenario.ProdutoAnterior.PrecoVenda);

        cenario.Context.ChangeTracker.Clear();
        var produto = await cenario.Context.Produto.SingleAsync(p => p.Id == cenario.ProdutoAnterior.Id);
        var item = await cenario.Context.Set<ItemPedido>().SingleAsync(i => i.Id == cenario.Item.Id);

        Assert.Equal(0, produto.Quantidade);
        Assert.Equal(3, item.Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateItemPedidoAsync_QuantidadeInvalida_NaoDeveAlterarDados(int quantidade)
    {
        await using var cenario = await CriarCenarioAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            cenario.Service.UpdateItemPedidoAsync(cenario.Item.Id, cenario.NovoProduto.Id, quantidade, 20m));

        Assert.Empty(await cenario.Context.ProdutoMovimentacao.ToListAsync());
    }

    [Theory]
    [InlineData(StatusPedido.Cancelado)]
    [InlineData(StatusPedido.Concluido)]
    [InlineData(StatusPedido.Carrinho)]
    public async Task UpdateItemPedidoAsync_StatusBloqueado_NaoDeveAlterarEstoque(StatusPedido status)
    {
        await using var cenario = await CriarCenarioAsync(status: status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cenario.Service.UpdateItemPedidoAsync(cenario.Item.Id, cenario.NovoProduto.Id, 1, 20m));

        cenario.Context.ChangeTracker.Clear();
        Assert.Equal(10, (await cenario.Context.Produto.SingleAsync(p => p.Id == cenario.NovoProduto.Id)).Quantidade);
        Assert.Empty(await cenario.Context.ProdutoMovimentacao.ToListAsync());
    }

    private static async Task<Cenario> CriarCenarioAsync(
        int estoqueProdutoAnterior = 5,
        StatusPedido status = StatusPedido.Confirmado)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var anterior = new Produto { Nome = "Anterior", PrecoVenda = 10m, Quantidade = estoqueProdutoAnterior };
        var novo = new Produto { Nome = "Novo", PrecoVenda = 20m, Quantidade = 10 };
        var pedido = new Pedido { Status = status, ValorTotalPedido = 20m };
        var item = new ItemPedido(pedido, anterior, 2, anterior.PrecoVenda);
        pedido.Itens.Add(item);
        context.Pedido.Add(pedido);
        context.Produto.Add(novo);
        await context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        var service = new ItemPedidoService(context, Mock.Of<IProdutoService>(), accessor.Object);

        return new Cenario(connection, context, service, pedido, item, anterior, novo);
    }

    private sealed class Cenario(
        SqliteConnection connection,
        ApplicationDbContext context,
        ItemPedidoService service,
        Pedido pedido,
        ItemPedido item,
        Produto produtoAnterior,
        Produto novoProduto) : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; } = context;
        public ItemPedidoService Service { get; } = service;
        public Pedido Pedido { get; } = pedido;
        public ItemPedido Item { get; } = item;
        public Produto ProdutoAnterior { get; } = produtoAnterior;
        public Produto NovoProduto { get; } = novoProduto;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
