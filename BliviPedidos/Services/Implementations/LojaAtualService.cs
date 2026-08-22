using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Exceptions;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public class LojaAtualService : ILojaAtualService
{
    private static readonly string[] ChavesSlugRota = ["lojaSlug", "slug"];

    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Task<Loja>? _lojaAtualTask;

    public LojaAtualService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<Loja> ObterLojaAsync()
    {
        return _lojaAtualTask ??= ResolverLojaAsync();
    }

    public async Task<int> ObterLojaIdAsync()
    {
        return (await ObterLojaAsync()).Id;
    }

    private async Task<Loja> ResolverLojaAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Não existe uma requisição HTTP ativa para resolver a loja.");
        var cancellationToken = httpContext.RequestAborted;

        if (string.Equals(
                httpContext.Request.RouteValues["area"]?.ToString(),
                "Loja",
                StringComparison.OrdinalIgnoreCase))
        {
            var lojaPublica = await ResolverLojaPelaRotaAsync(httpContext, cancellationToken);
            if (lojaPublica != null)
            {
                return lojaPublica;
            }
        }

        var usuarioId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            var lojaUsuario = await _context.UsuarioLoja
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(ul => ul.UsuarioId == usuarioId && ul.Loja.Ativa)
                .Select(ul => ul.Loja)
                .SingleOrDefaultAsync(cancellationToken);

            if (lojaUsuario != null)
            {
                return lojaUsuario;
            }

            var usuarioExiste = await _context.Users
                .AsNoTracking()
                .AnyAsync(usuario => usuario.Id == usuarioId, cancellationToken);

            if (!usuarioExiste)
            {
                throw new UsuarioAutenticadoInexistenteException();
            }

            throw new InvalidOperationException("O usuário autenticado não está associado a uma loja ativa.");
        }

        var lojaRota = await ResolverLojaPelaRotaAsync(httpContext, cancellationToken);
        if (lojaRota != null)
        {
            return lojaRota;
        }

        var dominio = httpContext.Request.Host.Host.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(dominio))
        {
            var lojaDominio = await _context.Loja
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    loja => loja.Ativa && loja.Dominio != null && loja.Dominio.ToLower() == dominio,
                    cancellationToken);

            if (lojaDominio != null)
            {
                return lojaDominio;
            }
        }

        return await _context.Loja
            .AsNoTracking()
            .SingleOrDefaultAsync(loja => loja.Id == Loja.PadraoId && loja.Ativa, cancellationToken)
            ?? throw new InvalidOperationException("A loja padrão não existe ou está inativa.");
    }

    private async Task<Loja?> ResolverLojaPelaRotaAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        foreach (var chave in ChavesSlugRota)
        {
            var slug = httpContext.Request.RouteValues[chave]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            var slugNormalizado = slug.ToLowerInvariant();
            var loja = await _context.Loja
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Ativa && item.Slug == slugNormalizado,
                    cancellationToken);

            return loja
                ?? throw new KeyNotFoundException($"Nenhuma loja ativa foi encontrada para o slug '{slug}'.");
        }

        return null;
    }
}
