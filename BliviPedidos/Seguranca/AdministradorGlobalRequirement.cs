using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BliviPedidos.Seguranca;

public sealed class AdministradorGlobalRequirement : IAuthorizationRequirement;

public sealed class AdministradorGlobalHandler : AuthorizationHandler<AdministradorGlobalRequirement>
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<IdentityUser> _userManager;

    public AdministradorGlobalHandler(IConfiguration configuration, UserManager<IdentityUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministradorGlobalRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailConfigurado = _configuration["BootstrapAdmin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(emailConfigurado))
            return;

        var usuario = await _userManager.FindByIdAsync(usuarioId);
        if (usuario is not null && AdministradorGlobal.EmailConfere(usuario.Email, emailConfigurado))
            context.Succeed(requirement);
    }
}

public static class AdministradorGlobal
{
    public static bool EmailConfere(string? emailUsuario, string? emailConfigurado) =>
        !string.IsNullOrWhiteSpace(emailUsuario)
        && !string.IsNullOrWhiteSpace(emailConfigurado)
        && string.Equals(emailUsuario.Trim(), emailConfigurado.Trim(), StringComparison.OrdinalIgnoreCase);
}
