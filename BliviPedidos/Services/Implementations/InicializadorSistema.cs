using BliviPedidos.Data;
using BliviPedidos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public static class InicializadorSistema
{
    public const string PerfilAdministrador = "Administrador";

    public static async Task InicializarAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("BootstrapAdmin");

        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var settings = configuration
            .GetSection("BootstrapAdmin")
            .Get<BootstrapAdminSettings>() ?? new BootstrapAdminSettings();

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "O banco não possui usuários. Configure BootstrapAdmin:Email e BootstrapAdmin:Password para criar o primeiro administrador.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        if (!await roleManager.RoleExistsAsync(PerfilAdministrador))
        {
            ValidarResultado(
                await roleManager.CreateAsync(new IdentityRole(PerfilAdministrador)),
                "criar o papel Administrador");
        }

        var email = settings.Email.Trim().ToLowerInvariant();
        var usuario = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        ValidarResultado(await userManager.CreateAsync(usuario, settings.Password), "criar o administrador inicial");
        ValidarResultado(await userManager.AddToRoleAsync(usuario, PerfilAdministrador), "atribuir o papel Administrador");

        context.DefinirLojaAtual(Loja.PadraoId);
        context.UsuarioLoja.Add(new UsuarioLoja
        {
            UsuarioId = usuario.Id,
            LojaId = Loja.PadraoId
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Administrador inicial criado e associado à loja padrão. Email: {Email}", email);
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
