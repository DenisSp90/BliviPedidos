using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BliviPedidos.Tests;

public class InicializadorSistemaTests
{
    [Fact]
    public async Task BancoSemUsuarios_DeveCriarAdministradorNaLojaPadrao()
    {
        await using var cenario = await CriarCenarioAsync(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Email"] = "admin@teste.com",
            ["BootstrapAdmin:Password"] = "SenhaForte!123"
        });

        await InicializadorSistema.InicializarAsync(cenario.Scope.ServiceProvider, cenario.Configuration);

        var userManager = cenario.Scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var context = cenario.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usuario = await userManager.FindByEmailAsync("admin@teste.com");

        Assert.NotNull(usuario);
        Assert.True(usuario.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(usuario, InicializadorSistema.PerfilAdministrador));
        var vinculo = await context.UsuarioLoja.SingleAsync(item => item.UsuarioId == usuario.Id);
        Assert.Equal(Loja.PadraoId, vinculo.LojaId);
    }

    [Fact]
    public async Task BancoSemUsuariosESemCredenciais_DeveInterromperInicializacao()
    {
        await using var cenario = await CriarCenarioAsync(new Dictionary<string, string?>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InicializadorSistema.InicializarAsync(cenario.Scope.ServiceProvider, cenario.Configuration));

        Assert.Contains("BootstrapAdmin:Email", exception.Message);
    }

    private static async Task<Cenario> CriarCenarioAsync(Dictionary<string, string?> configuracoes)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configuracoes)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        return new Cenario(connection, provider, scope, configuration);
    }

    private sealed class Cenario : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        public Cenario(
            SqliteConnection connection,
            ServiceProvider provider,
            IServiceScope scope,
            IConfiguration configuration)
        {
            _connection = connection;
            _provider = provider;
            Scope = scope;
            Configuration = configuration;
        }

        public IServiceScope Scope { get; }
        public IConfiguration Configuration { get; }

        public async ValueTask DisposeAsync()
        {
            Scope.Dispose();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
