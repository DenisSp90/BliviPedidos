using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BliviPedidos.Tests;

public class LojaAtualServiceTests
{
    [Fact]
    public async Task UsuarioAutenticado_DeveTerPrioridadeSobreRota()
    {
        await using var cenario = await Cenario.CriarAsync();
        cenario.HttpContext.User = CriarUsuarioAutenticado(cenario.UsuarioId);
        cenario.HttpContext.Request.RouteValues["lojaSlug"] = "blivi-pedidos";

        var loja = await cenario.Service.ObterLojaAsync();

        Assert.Equal(cenario.LojaAlternativaId, loja.Id);
    }

    [Fact]
    public async Task Rota_DeveResolverLojaPeloSlug()
    {
        await using var cenario = await Cenario.CriarAsync();
        cenario.HttpContext.Request.RouteValues["lojaSlug"] = "loja-alternativa";

        var loja = await cenario.Service.ObterLojaAsync();

        Assert.Equal(cenario.LojaAlternativaId, loja.Id);
    }

    [Fact]
    public async Task Dominio_DeveResolverLojaPeloHostSemPorta()
    {
        await using var cenario = await Cenario.CriarAsync();
        cenario.HttpContext.Request.Host = new HostString("alternativa.exemplo.com", 443);

        var loja = await cenario.Service.ObterLojaAsync();

        Assert.Equal(cenario.LojaAlternativaId, loja.Id);
    }

    [Fact]
    public async Task RequisicaoAnonimaSemIdentificacao_DeveUsarLojaPadrao()
    {
        await using var cenario = await Cenario.CriarAsync();

        var lojaId = await cenario.Service.ObterLojaIdAsync();

        Assert.Equal(Loja.PadraoId, lojaId);
    }

    [Fact]
    public async Task UsuarioSemLoja_DeveFalharEmVezDeUsarLojaPadrao()
    {
        await using var cenario = await Cenario.CriarAsync();
        cenario.HttpContext.User = CriarUsuarioAutenticado("usuario-sem-loja");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cenario.Service.ObterLojaAsync());

        Assert.Contains("não está associado", exception.Message);
    }

    private static ClaimsPrincipal CriarUsuarioAutenticado(string usuarioId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId) },
            "TestAuthentication");
        return new ClaimsPrincipal(identity);
    }

    private sealed class Cenario : IAsyncDisposable
    {
        private Cenario(
            SqliteConnection connection,
            ApplicationDbContext context,
            DefaultHttpContext httpContext,
            LojaAtualService service,
            string usuarioId,
            int lojaAlternativaId)
        {
            Connection = connection;
            Context = context;
            HttpContext = httpContext;
            Service = service;
            UsuarioId = usuarioId;
            LojaAlternativaId = lojaAlternativaId;
        }

        private SqliteConnection Connection { get; }
        private ApplicationDbContext Context { get; }
        public DefaultHttpContext HttpContext { get; }
        public LojaAtualService Service { get; }
        public string UsuarioId { get; }
        public int LojaAlternativaId { get; }

        public static async Task<Cenario> CriarAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var loja = new Loja
            {
                Nome = "Loja Alternativa",
                Slug = "loja-alternativa",
                Dominio = "alternativa.exemplo.com",
                Ativa = true
            };
            var usuario = new IdentityUser
            {
                Id = "usuario-loja-alternativa",
                UserName = "operador@alternativa.com"
            };
            context.AddRange(loja, usuario);
            await context.SaveChangesAsync();

            context.DefinirLojaAtual(loja.Id);
            context.UsuarioLoja.Add(new UsuarioLoja
            {
                UsuarioId = usuario.Id,
                LojaId = loja.Id
            });
            await context.SaveChangesAsync();
            context.DefinirLojaAtual(Loja.PadraoId);

            var httpContext = new DefaultHttpContext();
            var accessor = new TestHttpContextAccessor { HttpContext = httpContext };
            var service = new LojaAtualService(context, accessor);

            return new Cenario(connection, context, httpContext, service, usuario.Id, loja.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
