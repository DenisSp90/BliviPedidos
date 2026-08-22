using BliviPedidos.Areas.Loja.Controllers;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BliviPedidos.Tests;

public class LojaPublicaAreaTests
{
    [Fact]
    public void NovoProduto_DeveIniciarAtivoParaOCatalogo()
    {
        Assert.True(new ProdutoViewModel().IsAtivo);
    }

    [Fact]
    public void HomeController_DevePertencerAAreaLojaEPermitirAcessoAnonimo()
    {
        var controllerType = typeof(HomeController);
        var area = Assert.Single(controllerType
            .GetCustomAttributes(typeof(AreaAttribute), true)
            .Cast<AreaAttribute>());

        Assert.Equal("Loja", area.RouteValue);
        Assert.Single(controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        Assert.Empty(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Fact]
    public async Task Index_DeveExibirCatalogoDaLojaResolvida()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CriarContextoAsync(connection);
        var loja = new BliviPedidos.Models.Loja
        {
            Id = 9,
            Nome = "MK Store",
            Slug = "mkstore",
            CorPrimaria = "#112233",
            CorSecundaria = "#f5f5f5",
            Descricao = "Tecnologia para todos",
            Whatsapp = "5511999999999",
            EmailContato = "contato@mkstore.com",
            InstagramUrl = "https://instagram.com/mkstore",
            Ativa = true
        };
        var controller = CriarController(loja, context);

        var result = Assert.IsType<ViewResult>(await controller.Index("mkstore"));
        var model = Assert.IsType<CatalogoLojaViewModel>(result.Model);

        Assert.Equal("MK Store", model.Loja.Nome);
        Assert.Equal("mkstore", model.Loja.Slug);
        Assert.Equal("#112233", model.Loja.CorPrimaria);
        Assert.Equal("#f5f5f5", model.Loja.CorSecundaria);
        Assert.Equal("Tecnologia para todos", model.Loja.Descricao);
        Assert.Equal("5511999999999", model.Loja.Whatsapp);
        Assert.Equal("contato@mkstore.com", model.Loja.EmailContato);
        Assert.Equal("https://instagram.com/mkstore", model.Loja.InstagramUrl);
    }

    [Fact]
    public async Task Index_DeveExibirSomenteProdutosAtivosDisponiveisDaLojaAtual()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CriarContextoAsync(connection);
        context.Produto.AddRange(
            CriarProduto("Disponível", ativo: true, quantidade: 3),
            CriarProduto("Sem estoque", ativo: true, quantidade: 0),
            CriarProduto("Inativo", ativo: false, quantidade: 5));
        await context.SaveChangesAsync();

        var outraLoja = new BliviPedidos.Models.Loja
        {
            Nome = "Outra Loja",
            Slug = "outra-loja",
            Ativa = true
        };
        context.Loja.Add(outraLoja);
        await context.SaveChangesAsync();
        context.DefinirLojaAtual(outraLoja.Id);
        context.Produto.Add(CriarProduto("Produto de outra loja", ativo: true, quantidade: 10));
        await context.SaveChangesAsync();
        context.DefinirLojaAtual(BliviPedidos.Models.Loja.PadraoId);

        var lojaAtual = await context.Loja.SingleAsync(loja => loja.Id == BliviPedidos.Models.Loja.PadraoId);
        var controller = CriarController(lojaAtual, context);

        var result = Assert.IsType<ViewResult>(await controller.Index(lojaAtual.Slug));
        var model = Assert.IsType<CatalogoLojaViewModel>(result.Model);
        var produto = Assert.Single(model.Produtos);

        Assert.Equal("Disponível", produto.Nome);
        Assert.DoesNotContain(model.Produtos, item => item.Nome == "Produto de outra loja");
    }

    [Fact]
    public async Task BuscaCategoriaEDetalhe_DevemRespeitarProdutoPublicavel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CriarContextoAsync(connection);
        var celulares = new Categoria { Nome = "Celulares", IsAtivo = true };
        var acessorios = new Categoria { Nome = "Acessórios", IsAtivo = true };
        context.Categoria.AddRange(celulares, acessorios);
        await context.SaveChangesAsync();

        var alpha = CriarProduto("Telefone Alpha", ativo: true, quantidade: 2);
        alpha.Codigo = "ALPHA-01";
        alpha.CategoriaId = celulares.Id;
        var capa = CriarProduto("Capa Premium", ativo: true, quantidade: 4);
        capa.CategoriaId = acessorios.Id;
        context.Produto.AddRange(alpha, capa);
        await context.SaveChangesAsync();

        var lojaAtual = await context.Loja.SingleAsync(loja => loja.Id == BliviPedidos.Models.Loja.PadraoId);
        var controller = CriarController(lojaAtual, context);

        var resultadoBusca = Assert.IsType<ViewResult>(
            await controller.Index(lojaAtual.Slug, "Alpha", celulares.Id));
        var catalogo = Assert.IsType<CatalogoLojaViewModel>(resultadoBusca.Model);
        Assert.Equal("Telefone Alpha", Assert.Single(catalogo.Produtos).Nome);
        Assert.Equal(2, catalogo.Categorias.Count);

        var resultadoDetalhe = Assert.IsType<ViewResult>(
            await controller.Produto(lojaAtual.Slug, alpha.Id));
        var detalhe = Assert.IsType<ProdutoDetalheLojaViewModel>(resultadoDetalhe.Model);
        Assert.Equal("Telefone Alpha", detalhe.Produto.Nome);
        Assert.Equal("Celulares", detalhe.Produto.CategoriaNome);

        alpha.IsAtivo = false;
        await context.SaveChangesAsync();

        Assert.IsType<NotFoundResult>(await controller.Produto(lojaAtual.Slug, alpha.Id));
    }

    [Fact]
    public void Index_DeveExporRotaPublicaComSlug()
    {
        var action = typeof(HomeController).GetMethod(nameof(HomeController.Index));
        var route = Assert.Single(action!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>());

        Assert.Equal("/loja/{lojaSlug}", route.Template);
        Assert.Equal("CatalogoLoja", route.Name);
    }

    [Fact]
    public void Produto_DeveExporRotaPublicaComSlugEId()
    {
        var action = typeof(HomeController).GetMethod(nameof(HomeController.Produto));
        var route = Assert.Single(action!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>());

        Assert.Equal("/loja/{lojaSlug}/produto/{id:int}", route.Template);
        Assert.Equal("DetalheProdutoLoja", route.Name);
    }

    private sealed class LojaAtualServiceFake : ILojaAtualService
    {
        private readonly BliviPedidos.Models.Loja _loja;

        public LojaAtualServiceFake(BliviPedidos.Models.Loja loja)
        {
            _loja = loja;
        }

        public Task<BliviPedidos.Models.Loja> ObterLojaAsync() => Task.FromResult(_loja);
        public Task<int> ObterLojaIdAsync() => Task.FromResult(_loja.Id);
    }

    private static HomeController CriarController(
        BliviPedidos.Models.Loja loja,
        ApplicationDbContext context)
    {
        return new HomeController(new LojaAtualServiceFake(loja), context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static async Task<ApplicationDbContext> CriarContextoAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Produto CriarProduto(string nome, bool ativo, int quantidade)
    {
        return new Produto
        {
            Nome = nome,
            PrecoVenda = 100,
            PrecoPago = 50,
            IsAtivo = ativo,
            Quantidade = quantidade
        };
    }
}
