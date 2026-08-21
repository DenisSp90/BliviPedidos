using BliviPedidos.Data;
using BliviPedidos.Services.Interfaces;

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
        ApplicationDbContext context)
    {
        var lojaId = await lojaAtualService.ObterLojaIdAsync();
        context.DefinirLojaAtual(lojaId);
        httpContext.Items["LojaId"] = lojaId;

        await _next(httpContext);
    }
}
