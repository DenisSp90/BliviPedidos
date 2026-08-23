using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
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
        context.Users.Add(CriarUsuario());
        await context.SaveChangesAsync();

        var carrinho = new CarrinhoFake(produto.Id, quantidade: 2);
        var dados = new DadosFake();
        var service = new ConfirmacaoCheckoutService(
            context,
            carrinho,
            dados,
            NullLogger<ConfirmacaoCheckoutService>.Instance,
            Options.Create(new ReservaEstoqueOptions { ExpiracaoMinutos = 30 }),
            CriarAcessor());

        Assert.Empty(context.Pedido);
        var resultado = await service.ConfirmarAsync(Loja.PadraoId);
        context.ChangeTracker.Clear();

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.CodigoPublico);
        Assert.Equal(CodigoPublicoPedido.Tamanho, resultado.CodigoPublico.Length);
        Assert.NotEqual(resultado.PedidoId!.Value.ToString(), resultado.CodigoPublico);
        var pedido = await context.Pedido.Include(item => item.Itens).Include(item => item.Cadastro).SingleAsync();
        Assert.Equal(resultado.CodigoPublico, pedido.CodigoPublico);
        Assert.Equal(StatusPedido.Confirmado, pedido.Status);
        Assert.Equal(StatusPagamento.AguardandoPagamento, pedido.StatusPagamento);
        Assert.Equal("consumidor-1", pedido.ConsumidorUsuarioId);
        Assert.InRange(pedido.ReservaExpiraEm!.Value, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        Assert.Equal(50m, pedido.ValorTotalPedido);
        Assert.Equal(2, Assert.Single(pedido.Itens).Quantidade);
        Assert.Equal("Maria", pedido.Cadastro.Nome);
        Assert.Equal(3, (await context.Produto.SingleAsync()).Quantidade);
        var movimentacao = await context.ProdutoMovimentacao.SingleAsync();
        Assert.Equal(Loja.PadraoId, movimentacao.LojaId);
        Assert.Equal(pedido.Id, movimentacao.PedidoId);
        Assert.Equal("maria@example.com", movimentacao.Ator);
        Assert.Equal(OrigemMovimentacaoEstoque.CheckoutPublico, movimentacao.Origem);
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
            Assert.All(codigo, caractere => Assert.True(char.IsLetterOrDigit(caractere)));
        });
        Assert.Equal(codigos.Length, codigos.Distinct().Count());
    }

    [Fact]
    public async Task Confirmar_SemEstoqueNaoDeveCriarPedidoMovimentarOuLimparSessao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var produto = new Produto
        {
            Nome = "Última unidade",
            PrecoVenda = 40m,
            PrecoPago = 20m,
            Quantidade = 1,
            IsAtivo = true
        };
        context.Produto.Add(produto);
        context.Users.Add(CriarUsuario());
        await context.SaveChangesAsync();
        var carrinho = new CarrinhoFake(produto.Id, quantidade: 2);
        var dados = new DadosFake();
        var service = new ConfirmacaoCheckoutService(
            context,
            carrinho,
            dados,
            NullLogger<ConfirmacaoCheckoutService>.Instance,
            Options.Create(new ReservaEstoqueOptions()),
            CriarAcessor());

        var resultado = await service.ConfirmarAsync(Loja.PadraoId);

        context.ChangeTracker.Clear();
        Assert.False(resultado.Sucesso);
        Assert.Contains("estoque suficiente", resultado.Erro);
        Assert.Empty(await context.Pedido.ToListAsync());
        Assert.Empty(await context.ProdutoMovimentacao.ToListAsync());
        Assert.Equal(1, (await context.Produto.SingleAsync()).Quantidade);
        Assert.False(carrinho.Limpo);
        Assert.False(dados.Limpos);
    }

    [Fact]
    public async Task Confirmar_DoisCheckoutsParaUltimaUnidadeDeveAceitarSomenteUm()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

        int produtoId;
        await using (var preparacao = new ApplicationDbContext(options))
        {
            await preparacao.Database.EnsureCreatedAsync();
            var produto = new Produto
            {
                Nome = "Última unidade concorrida",
                PrecoVenda = 100m,
                PrecoPago = 50m,
                Quantidade = 1,
                IsAtivo = true
            };
            preparacao.Produto.Add(produto);
            preparacao.Users.Add(CriarUsuario());
            await preparacao.SaveChangesAsync();
            produtoId = produto.Id;
        }

        var primeiroCarrinho = new CarrinhoFake(produtoId, 1);
        await using (var primeiroContexto = new ApplicationDbContext(options))
        {
            var primeiroServico = new ConfirmacaoCheckoutService(
                primeiroContexto,
                primeiroCarrinho,
                new DadosFake(),
                NullLogger<ConfirmacaoCheckoutService>.Instance,
                Options.Create(new ReservaEstoqueOptions()),
                CriarAcessor());
            var primeiroResultado = await primeiroServico.ConfirmarAsync(Loja.PadraoId);
            Assert.True(primeiroResultado.Sucesso);
        }

        var segundoCarrinho = new CarrinhoFake(produtoId, 1);
        var segundoDados = new DadosFake();
        await using (var segundoContexto = new ApplicationDbContext(options))
        {
            var segundoServico = new ConfirmacaoCheckoutService(
                segundoContexto,
                segundoCarrinho,
                segundoDados,
                NullLogger<ConfirmacaoCheckoutService>.Instance,
                Options.Create(new ReservaEstoqueOptions()),
                CriarAcessor());
            var segundoResultado = await segundoServico.ConfirmarAsync(Loja.PadraoId);

            Assert.False(segundoResultado.Sucesso);
            Assert.Contains("estoque suficiente", segundoResultado.Erro);
        }

        await using var verificacao = new ApplicationDbContext(options);
        Assert.Equal(0, (await verificacao.Produto.SingleAsync()).Quantidade);
        Assert.Single(await verificacao.Pedido.ToListAsync());
        Assert.Single(await verificacao.ProdutoMovimentacao.ToListAsync());
        Assert.True(primeiroCarrinho.Limpo);
        Assert.False(segundoCarrinho.Limpo);
        Assert.False(segundoDados.Limpos);
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

    private static IHttpContextAccessor CriarAcessor()
    {
        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "consumidor-1")],
            "TestAuthentication");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identidade)
            }
        };
    }

    private static IdentityUser CriarUsuario() => new()
    {
        Id = "consumidor-1",
        UserName = "consumidor@example.com",
        NormalizedUserName = "CONSUMIDOR@EXAMPLE.COM",
        Email = "consumidor@example.com",
        NormalizedEmail = "CONSUMIDOR@EXAMPLE.COM"
    };

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
