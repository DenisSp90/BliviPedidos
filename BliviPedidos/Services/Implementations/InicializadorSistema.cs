using BliviPedidos.Data;
using BliviPedidos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public static class InicializadorSistema
{
    public const string PerfilAdministrador = "Administrador";
    public const string PerfilVendedor = "Vendedor";
    public const string PerfilEstoquista = "Estoquista";

    public static readonly IReadOnlyCollection<string> Perfis =
    [
        PerfilAdministrador,
        PerfilVendedor,
        PerfilEstoquista
    ];

    public static async Task InicializarAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("BootstrapAdmin");

        await CriarPerfisAsync(roleManager);

        var settings = configuration
            .GetSection("BootstrapAdmin")
            .Get<BootstrapAdminSettings>() ?? new BootstrapAdminSettings();

        if (string.IsNullOrWhiteSpace(settings.Email))
        {
            logger.LogCritical(
                "BootstrapAdmin:Email não está configurado. O console global permanecerá bloqueado, mas a aplicação continuará disponível.");
            return;
        }

        var email = settings.Email.Trim().ToLowerInvariant();
        var usuario = await userManager.FindByEmailAsync(email);
        var usuarioCriado = usuario is null;
        if (usuario is null)
        {
            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                logger.LogCritical(
                    "O administrador global {Email} ainda não existe e BootstrapAdmin:Password não está configurado. " +
                    "A conta não será criada e o console global permanecerá bloqueado.",
                    email);
                return;
            }

            usuario = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            ValidarResultado(
                await userManager.CreateAsync(usuario, settings.Password),
                "criar o administrador global");
        }

        if (!await userManager.IsInRoleAsync(usuario, PerfilAdministrador))
            ValidarResultado(
                await userManager.AddToRoleAsync(usuario, PerfilAdministrador),
                "atribuir o papel Administrador ao administrador global");

        var possuiVinculo = await context.UsuarioLoja
            .IgnoreQueryFilters()
            .AnyAsync(item => item.UsuarioId == usuario.Id, cancellationToken);
        if (!possuiVinculo)
        {
            context.DefinirLojaAtual(Loja.PadraoId);
            context.UsuarioLoja.Add(new UsuarioLoja
            {
                UsuarioId = usuario.Id,
                LojaId = Loja.PadraoId
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            usuarioCriado
                ? "Administrador global criado. Email: {Email}"
                : "Administrador global validado. Email: {Email}",
            email);
    }

    private static async Task CriarPerfisAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var perfil in Perfis)
        {
            if (await roleManager.RoleExistsAsync(perfil))
            {
                continue;
            }

            ValidarResultado(
                await roleManager.CreateAsync(new IdentityRole(perfil)),
                $"criar o papel {perfil}");
        }
    }

    private static void ValidarResultado(IdentityResult resultado, string operacao)
    {
        if (resultado.Succeeded)
        {
            return;
        }

        var erros = string.Join("; ", resultado.Errors.Select(erro => erro.Description));
        throw new InvalidOperationException($"Não foi possível {operacao}: {erros}");
    }
}
