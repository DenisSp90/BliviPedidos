using BliviPedidos.Data;
using BliviPedidos.Services.Exceptions;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace BliviPedidos.Middleware;

public class LojaAtualMiddleware
{
    private readonly RequestDelegate _next;

    public LojaAtualMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ILojaAtualService lojaAtualService,
        ApplicationDbContext context,
        IAuthenticationService authenticationService)
    {
        try
        {
            var lojaId = await lojaAtualService.ObterLojaIdAsync();
            context.DefinirLojaAtual(lojaId);
            httpContext.Items["LojaId"] = lojaId;
        }
        catch (KeyNotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync("Loja não encontrada.");
            return;
        }
        catch (UsuarioAutenticadoInexistenteException)
        {
            await authenticationService.SignOutAsync(
                httpContext,
                IdentityConstants.ApplicationScheme,
                properties: null);

            var retorno = $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}";
            var login = QueryString.Create("returnUrl", retorno);
            httpContext.Response.Redirect($"/Identity/Account/Login{login}");
            return;
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // O cliente encerrou a requisição (navegação, recarga ou aba fechada).
            // Não transforma um cancelamento esperado em erro HTTP 500.
        }
    }
}
