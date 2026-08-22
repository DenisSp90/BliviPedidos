using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BliviPedidos.Tests;

public class ConfirmacaoCheckoutServiceTests
{
    [Fact]
    public async Task Confirmar_DeveCriarPedidoBaixarEstoqueESomenteEntaoLimparSessao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var produto = new Produto
        {
            Nome = "Produto checkout",
            PrecoVenda = 25m,
            PrecoPago = 10m,
            Quantidade = 5,
            IsAtivo = true
        };
        context.Produto.Add(produto);
        await context.SaveChangesAsync();

        var carrinho = new CarrinhoFake(produto.Id, quantidade: 2);
        var dados = new DadosFake();
        var service = new ConfirmacaoCheckoutService(
            context,
            carrinho,
            dados,
            NullLogger<ConfirmacaoCheckoutService>.Instance);

        Assert.Empty(context.Pedido);
        var resultado = await service.ConfirmarAsync(Loja.PadraoId);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.CodigoPublico);
        Assert.Equal(CodigoPublicoPedido.Tamanho, resultado.CodigoPublico.Length);
        Assert.DoesNotContain(resultado.PedidoId!.Value.ToString(), resultado.CodigoPublico);
        var pedido = await context.Pedido.Include(item => item.Itens).Include(item => item.Cadastro).SingleAsync();
        Assert.Equal(resultado.CodigoPublico, pedido.CodigoPublico);
        Assert.Equal(50m, pedido.ValorTotalPedido);
        Assert.Equal(2, Assert.Single(pedido.Itens).Quantidade);
        Assert.Equal("Maria", pedido.Cadastro.Nome);
        Assert.Equal(3, (await context.Produto.SingleAsync()).Quantidade);
        Assert.Single(context.ProdutoMovimentacao);
        Assert.True(carrinho.Limpo);
        Assert.True(dados.Limpos);
    }

    [Fact]
    public void CodigoPublico_DeveSerOpacoNaoSequencialEUnicoNaAmostra()
    {
        var codigos = Enumerable.Range(0, 1000)
            .Select(_ => CodigoPublicoPedido.Gerar())
            .ToArray();

        Assert.All(codigos, codigo =>
        {
            Assert.Equal(CodigoPublicoPedido.Tamanho, codigo.Length);
            Assert.DoesNotContain("+", codigo);
            Assert.DoesNotContain("/", codigo);
            Assert.DoesNotContain("=", codigo);
        });
        Assert.Equal(codigos.Length, codigos.Distinct().Count());
    }

    private sealed class CarrinhoFake : ICarrinhoPublicoService
    {
        private readonly CarrinhoPublico _carrinho;
        public bool Limpo { get; private set; }

        public CarrinhoFake(int produtoId, int quantidade)
        {
            _carrinho = new CarrinhoPublico
            {
                Identificador = "confirmacao",
                LojaId = Loja.PadraoId,
                Itens = [new ItemCarrinhoPublico { ProdutoId = produtoId, Quantidade = quantidade }]
            };
        }

        public CarrinhoPublico Obter(int lojaId) => _carrinho;
        public CarrinhoPublico Adicionar(int lojaId, int produtoId, int quantidade = 1) => _carrinho;
        public CarrinhoPublico Remover(int lojaId, int produtoId) => _carrinho;
        public void Limpar(int lojaId) => Limpo = true;
    }

    private sealed class DadosFake : IDadosConsumidorCheckoutService
    {
        public bool Limpos { get; private set; }
        public DadosConsumidorCheckout? Obter(int lojaId) => new()
        {
            Nome = "Maria",
            Telefone = "11 99999-9999",
            Email = "maria@example.com"
        };
        public void Salvar(int lojaId, DadosConsumidorCheckout dados) { }
        public void Limpar(int lojaId) => Limpos = true;
    }
}
