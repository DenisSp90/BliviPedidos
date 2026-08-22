using BliviPedidos.Data;
using BliviPedidos.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BliviPedidos.Tests;

public class ApplicationDbContextTests
{
    [Fact]
    public void Pedido_DeveMapearStatusENaoMaisAtivo()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApplicationDbContext(options);
        var pedido = context.Model.FindEntityType(typeof(Pedido));

        Assert.NotNull(pedido?.FindProperty(nameof(Pedido.Status)));
        Assert.Null(pedido?.FindProperty("Ativo"));
        Assert.NotNull(pedido?.FindProperty(nameof(Pedido.StatusPagamento)));
        Assert.Null(pedido?.FindProperty("Pago"));
        Assert.Equal(StatusPedido.Carrinho, new Pedido().Status);
        Assert.Equal(StatusPagamento.AguardandoPagamento, new Pedido().StatusPagamento);
    }

    [Fact]
    public async Task CriacaoDoBanco_DeveIncluirLojaPadrao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var loja = await context.Loja.SingleAsync(l => l.Id == 1);
        Assert.Equal("Blivi Pedidos", loja.Nome);
        Assert.Equal("blivi-pedidos", loja.Slug);
        Assert.Equal("#0d6efd", loja.CorPrimaria);
        Assert.Equal("#ffffff", loja.CorSecundaria);
        Assert.True(loja.Ativa);
    }

    [Theory]
    [InlineData(typeof(Produto))]
    [InlineData(typeof(Categoria))]
    [InlineData(typeof(Cliente))]
    [InlineData(typeof(Pedido))]
    public void Modelo_DeveExigirLojaId(Type entidade)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApplicationDbContext(options);

        var entityType = context.Model.FindEntityType(entidade);
        var lojaId = entityType?.FindProperty("LojaId");
        var relacionamento = entityType?.GetForeignKeys()
            .Single(fk => fk.Properties.Any(property => property.Name == "LojaId"));

        Assert.NotNull(lojaId);
        Assert.False(lojaId.IsNullable);
        Assert.NotNull(relacionamento);
        Assert.True(relacionamento.IsRequired);
    }

    [Fact]
    public void Modelo_DevePermitirUmaUnicaLojaPorUsuario()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApplicationDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(UsuarioLoja));
        var primaryKey = entityType?.FindPrimaryKey();
        var lojaForeignKey = entityType?.GetForeignKeys()
            .Single(fk => fk.Properties.Any(property => property.Name == nameof(UsuarioLoja.LojaId)));

        Assert.Equal(nameof(UsuarioLoja.UsuarioId), primaryKey?.Properties.Single().Name);
        Assert.NotNull(lojaForeignKey);
        Assert.True(lojaForeignKey.IsRequired);
    }

    [Theory]
    [InlineData(typeof(Produto))]
    [InlineData(typeof(Categoria))]
    [InlineData(typeof(Cliente))]
    [InlineData(typeof(Pedido))]
    [InlineData(typeof(UsuarioLoja))]
    [InlineData(typeof(ItemPedido))]
    [InlineData(typeof(Cadastro))]
    [InlineData(typeof(ProdutoMovimentacao))]
    public void Modelo_DevePossuirFiltroGlobalDeLoja(Type entidade)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApplicationDbContext(options);

        var filtro = context.Model.FindEntityType(entidade)?.GetQueryFilter();

        Assert.NotNull(filtro);
    }

    [Fact]
    public async Task ConsultaEGravacao_DevemFicarRestritasALojaAtual()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var lojaAlternativa = new Loja
        {
            Nome = "Loja Alternativa",
            Slug = "loja-filtro",
            Ativa = true
        };
        context.Loja.Add(lojaAlternativa);
        await context.SaveChangesAsync();

        context.Produto.Add(new Produto { Nome = "Produto padrão", PrecoVenda = 10m });
        await context.SaveChangesAsync();

        context.DefinirLojaAtual(lojaAlternativa.Id);
        context.Produto.Add(new Produto { Nome = "Produto alternativo", PrecoVenda = 20m });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var produtosAlternativos = await context.Produto.ToListAsync();
        Assert.Single(produtosAlternativos);
        Assert.Equal("Produto alternativo", produtosAlternativos[0].Nome);
        Assert.Equal(lojaAlternativa.Id, produtosAlternativos[0].LojaId);

        context.DefinirLojaAtual(Loja.PadraoId);
        var produtosPadrao = await context.Produto.ToListAsync();
        Assert.Single(produtosPadrao);
        Assert.Equal("Produto padrão", produtosPadrao[0].Nome);

        var produtoOutraLoja = await context.Produto
            .IgnoreQueryFilters()
            .SingleAsync(produto => produto.LojaId == lojaAlternativa.Id);
        produtoOutraLoja.Nome = "Alteração indevida";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}
