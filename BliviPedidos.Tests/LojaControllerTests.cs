using BliviPedidos.Controllers;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BliviPedidos.Tests;

public class LojaControllerTests
{
    [Fact]
    public async Task Criar_DeveNormalizarESalvarLoja()
    {
        await using var cenario = await Cenario.CriarAsync();
        var model = new LojaViewModel
        {
            Nome = " Loja Nova ",
            Slug = "LOJA-NOVA",
            Dominio = "https://LOJA.EXEMPLO.COM/catalogo",
            LogoUrl = "https://cdn.exemplo.com/logo.png",
            CorPrimaria = "#112233",
            Ativa = true
        };

        var result = await cenario.Controller.Criar(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LojaController.Index), redirect.ActionName);
        var loja = await cenario.Context.Loja.SingleAsync(item => item.Slug == "loja-nova");
        Assert.Equal("Loja Nova", loja.Nome);
        Assert.Equal("loja.exemplo.com", loja.Dominio);
        Assert.Equal("#112233", loja.CorPrimaria);
    }

    [Fact]
    public async Task Criar_ComSlugDuplicado_DeveRetornarFormularioComErro()
    {
        await using var cenario = await Cenario.CriarAsync();
        var model = new LojaViewModel
        {
            Nome = "Outra loja",
            Slug = "BLIVI-PEDIDOS",
            CorPrimaria = "#112233"
        };

        var result = await cenario.Controller.Criar(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Formulario", view.ViewName);
        Assert.True(cenario.Controller.ModelState.ContainsKey(nameof(model.Slug)));
        Assert.Single(await cenario.Context.Loja.ToListAsync());
    }

    [Fact]
    public async Task Editar_NaoDevePermitirDesativarLojaPadrao()
    {
        await using var cenario = await Cenario.CriarAsync();
        var model = new LojaViewModel
        {
            Id = Loja.PadraoId,
            Nome = "Blivi Pedidos",
            Slug = "blivi-pedidos",
            CorPrimaria = "#0d6efd",
            Ativa = false
        };

        var result = await cenario.Controller.Editar(Loja.PadraoId, model);

        Assert.IsType<ViewResult>(result);
        Assert.True(cenario.Controller.ModelState.ContainsKey(nameof(model.Ativa)));
        Assert.True((await cenario.Context.Loja.SingleAsync(item => item.Id == Loja.PadraoId)).Ativa);
    }

    private sealed class Cenario : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Cenario(SqliteConnection connection, ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;
            Controller = new LojaController(context, NullLogger<LojaController>.Instance);
        }

        public ApplicationDbContext Context { get; }
        public LojaController Controller { get; }

        public static async Task<Cenario> CriarAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new Cenario(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
