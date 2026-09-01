using BliviPedidos.Seguranca;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BliviPedidos.Tests;

public class PoliticasAutorizacaoTests
{
    [Fact]
    public void BackupController_DeveUsarPoliticaDeVendas()
    {
        var authorize = Assert.Single(typeof(BackupController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(PoliticasAutorizacao.Vendas, authorize.Policy);
    }

    public static TheoryData<string, string[]> PoliticasEsperadas => new()
    {
        {
            PoliticasAutorizacao.Administracao,
            [InicializadorSistema.PerfilAdministrador]
        },
        {
            PoliticasAutorizacao.Vendas,
            [InicializadorSistema.PerfilAdministrador, InicializadorSistema.PerfilVendedor]
        },
        {
            PoliticasAutorizacao.Estoque,
            [
                InicializadorSistema.PerfilAdministrador,
                InicializadorSistema.PerfilVendedor,
                InicializadorSistema.PerfilEstoquista
            ]
        },
        {
            PoliticasAutorizacao.Relatorios,
            InicializadorSistema.Perfis.ToArray()
        },
        {
            PoliticasAutorizacao.AcessoInterno,
            InicializadorSistema.Perfis.ToArray()
        }
    };

    [Theory]
    [MemberData(nameof(PoliticasEsperadas))]
    public void Politica_DevePermitirSomentePerfisConfigurados(
        string nomePolitica,
        string[] perfisEsperados)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarPoliticasAutorizacao();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var politica = options.GetPolicy(nomePolitica);
        var requisito = Assert.Single(politica!.Requirements.OfType<RolesAuthorizationRequirement>());

        Assert.Equal(
            perfisEsperados.OrderBy(perfil => perfil),
            requisito.AllowedRoles.OrderBy(perfil => perfil));
    }
}
