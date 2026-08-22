using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BliviPedidos.Tests;

public class CarrinhoPublicoServiceTests
{
    [Fact]
    public void Carrinho_DeveSerSeparadoPorLojaESomarQuantidade()
    {
        var session = new SessaoTeste();
        var httpContext = new DefaultHttpContext { Session = session };
        var service = new CarrinhoPublicoService(new HttpContextAccessor { HttpContext = httpContext });

        service.Adicionar(lojaId: 1, produtoId: 10);
        service.Adicionar(lojaId: 1, produtoId: 10, quantidade: 2);
        service.Adicionar(lojaId: 2, produtoId: 20);

        var itemLojaUm = Assert.Single(service.Obter(1).Itens);
        var itemLojaDois = Assert.Single(service.Obter(2).Itens);
        Assert.Equal(service.Obter(1).Identificador, service.Obter(2).Identificador);
        Assert.True(service.Obter(1).Identificador.Length >= 43);
        Assert.Equal(10, itemLojaUm.ProdutoId);
        Assert.Equal(3, itemLojaUm.Quantidade);
        Assert.Equal(20, itemLojaDois.ProdutoId);
        Assert.Equal(1, itemLojaDois.Quantidade);
    }

    [Fact]
    public void Carrinho_NaoDeveConterPedidoPrecoOuEntidadeProduto()
    {
        var propriedadesCarrinho = typeof(CarrinhoPublico).GetProperties();
        var propriedadesItem = typeof(ItemCarrinhoPublico).GetProperties();

        Assert.Equal(["Identificador", "LojaId", "Itens"], propriedadesCarrinho.Select(item => item.Name));
        Assert.Equal(["ProdutoId", "Quantidade"], propriedadesItem.Select(item => item.Name));
        Assert.DoesNotContain(propriedadesCarrinho, item => item.PropertyType == typeof(Pedido));
        Assert.DoesNotContain(propriedadesItem, item => item.PropertyType == typeof(Produto));
        Assert.DoesNotContain(propriedadesItem, item => item.Name.Contains("Preco", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Carrinho_DeveTerIdentificadorAnonimoDiferentePorSessao()
    {
        var primeiro = CriarService(new SessaoTeste()).Obter(lojaId: 1);
        var segundo = CriarService(new SessaoTeste()).Obter(lojaId: 1);

        Assert.NotEqual(primeiro.Identificador, segundo.Identificador);
        Assert.DoesNotContain("+", primeiro.Identificador);
        Assert.DoesNotContain("/", primeiro.Identificador);
        Assert.DoesNotContain("=", primeiro.Identificador);
    }

    [Fact]
    public async Task Total_DeveUsarPrecoAtualDoBancoEIgnorarValorDoNavegador()
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
            Nome = "Produto com preço atualizado",
            PrecoVenda = 10m,
            PrecoPago = 1m,
            Quantidade = 5,
            IsAtivo = true
        };
        context.Produto.Add(produto);
        await context.SaveChangesAsync();

        var carrinho = new CarrinhoPublico
        {
            Identificador = "carrinho-teste",
            LojaId = Loja.PadraoId,
            Itens = [new ItemCarrinhoPublico { ProdutoId = produto.Id, Quantidade = 2 }]
        };

        // Simula uma alteração posterior ao valor que o navegador poderia ter exibido.
        produto.PrecoVenda = 37.50m;
        await context.SaveChangesAsync();

        var calculador = new CalculadorCarrinhoPublicoService(context);
        var resultado = await calculador.ValidarERecalcularAsync(carrinho);
        var item = Assert.Single(resultado.Itens);

        Assert.True(resultado.Valido);
        Assert.Equal(37.50m, item.Produto.PrecoVenda);
        Assert.Equal(75m, item.Subtotal);
    }

    [Fact]
    public async Task Validacao_DeveRejeitarProdutoInativoQuantidadeExcedenteEProdutoInexistente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var produto = new Produto
        {
            Nome = "Produto indisponível",
            PrecoVenda = 20m,
            PrecoPago = 10m,
            Quantidade = 1,
            IsAtivo = false
        };
        var produtoComEstoqueInsuficiente = new Produto
        {
            Nome = "Produto com estoque insuficiente",
            PrecoVenda = 30m,
            PrecoPago = 15m,
            Quantidade = 1,
            IsAtivo = true
        };
        context.Produto.AddRange(produto, produtoComEstoqueInsuficiente);
        await context.SaveChangesAsync();
        var carrinho = new CarrinhoPublico
        {
            Identificador = "validacao",
            LojaId = Loja.PadraoId,
            Itens =
            [
                new ItemCarrinhoPublico { ProdutoId = produto.Id, Quantidade = 2 },
                new ItemCarrinhoPublico { ProdutoId = 999999, Quantidade = 1 },
                new ItemCarrinhoPublico { ProdutoId = produto.Id, Quantidade = 1 },
                new ItemCarrinhoPublico { ProdutoId = produtoComEstoqueInsuficiente.Id, Quantidade = 2 },
                new ItemCarrinhoPublico { ProdutoId = 0, Quantidade = 0 }
            ]
        };

        var resultado = await new CalculadorCarrinhoPublicoService(context)
            .ValidarERecalcularAsync(carrinho);

        Assert.False(resultado.Valido);
        Assert.Contains(resultado.Erros, erro => erro.Contains("inválida"));
        Assert.Contains(resultado.Erros, erro => erro.Contains("duplicados"));
        Assert.Contains(resultado.Erros, erro => erro.Contains("não existem"));
        Assert.Contains(resultado.Erros, erro => erro.Contains("não está mais disponível"));
        Assert.Contains(resultado.Erros, erro => erro.Contains("quantidade solicitada"));
    }

    private static CarrinhoPublicoService CriarService(ISession session)
    {
        var httpContext = new DefaultHttpContext { Session = session };
        return new CarrinhoPublicoService(new HttpContextAccessor { HttpContext = httpContext });
    }

    private sealed class SessaoTeste : ISession
    {
        private readonly Dictionary<string, byte[]> _dados = [];

        public IEnumerable<string> Keys => _dados.Keys;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public bool IsAvailable => true;

        public void Clear() => _dados.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _dados.Remove(key);
        public void Set(string key, byte[] value) => _dados[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return _dados.TryGetValue(key, out value!);
        }
    }
}
