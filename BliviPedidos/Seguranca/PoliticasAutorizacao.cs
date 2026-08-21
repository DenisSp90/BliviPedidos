using BliviPedidos.Services.Implementations;

namespace BliviPedidos.Seguranca;

public static class PoliticasAutorizacao
{
    public const string Administracao = "Administracao";
    public const string Vendas = "Vendas";
    public const string Estoque = "Estoque";
    public const string Relatorios = "Relatorios";
    public const string AcessoInterno = "AcessoInterno";

    public static IServiceCollection AdicionarPoliticasAutorizacao(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Administracao, policy =>
                policy.RequireRole(InicializadorSistema.PerfilAdministrador));

            options.AddPolicy(Vendas, policy =>
                policy.RequireRole(
                    InicializadorSistema.PerfilAdministrador,
                    InicializadorSistema.PerfilVendedor));

            options.AddPolicy(Estoque, policy =>
                policy.RequireRole(
                    InicializadorSistema.PerfilAdministrador,
                    InicializadorSistema.PerfilEstoquista));

            options.AddPolicy(Relatorios, policy =>
                policy.RequireRole(InicializadorSistema.Perfis));

            options.AddPolicy(AcessoInterno, policy =>
                policy.RequireRole(InicializadorSistema.Perfis));
        });

        return services;
    }
}
