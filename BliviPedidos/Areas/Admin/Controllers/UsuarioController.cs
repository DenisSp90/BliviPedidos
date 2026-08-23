using BliviPedidos.Data;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public sealed class UsuarioController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UsuarioController> _logger;

    public UsuarioController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<UsuarioController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var usuarioAtualId = _userManager.GetUserId(User);
        var vinculos = await _context.UsuarioLoja
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(item => item.Usuario)
            .Include(item => item.Loja)
            .OrderBy(item => item.Loja.Nome)
            .ThenBy(item => item.Usuario.Email)
            .ToListAsync();

        var usuarios = new List<UsuarioAdminListaViewModel>(vinculos.Count);
        foreach (var vinculo in vinculos)
        {
            var perfis = await _userManager.GetRolesAsync(vinculo.Usuario);
            usuarios.Add(new UsuarioAdminListaViewModel
            {
                Id = vinculo.UsuarioId,
                Email = vinculo.Usuario.Email ?? vinculo.Usuario.UserName ?? "—",
                LojaNome = vinculo.Loja.Nome,
                Perfil = perfis.FirstOrDefault() ?? "Sem perfil",
                Ativo = UsuarioAtivo(vinculo.Usuario),
                UsuarioAtual = vinculo.UsuarioId == usuarioAtualId
            });
        }

        return View(usuarios);
    }

    public async Task<IActionResult> Editar(string id)
    {
        var vinculo = await ObterVinculoAsync(id);
        if (vinculo == null)
            return NotFound();

        var perfis = await _userManager.GetRolesAsync(vinculo.Usuario);
        var model = new UsuarioAdminEdicaoViewModel
        {
            Id = vinculo.UsuarioId,
            Email = vinculo.Usuario.Email ?? string.Empty,
            LojaId = vinculo.LojaId,
            Perfil = perfis.FirstOrDefault() ?? string.Empty,
            Ativo = UsuarioAtivo(vinculo.Usuario),
            UsuarioAtual = vinculo.UsuarioId == _userManager.GetUserId(User)
        };
        await CarregarOpcoesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(string id, UsuarioAdminEdicaoViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var vinculo = await ObterVinculoAsync(id);
        if (vinculo == null)
            return NotFound();

        var usuario = vinculo.Usuario;
        var perfisAtuais = await _userManager.GetRolesAsync(usuario);
        var perfilAtual = perfisAtuais.FirstOrDefault() ?? string.Empty;
        model.Email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        model.UsuarioAtual = id == _userManager.GetUserId(User);

        if (!InicializadorSistema.Perfis.Contains(model.Perfil, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Perfil), "Selecione um perfil válido.");
        if (!await _context.Loja.AnyAsync(loja => loja.Id == model.LojaId && loja.Ativa))
            ModelState.AddModelError(nameof(model.LojaId), "Selecione uma loja ativa.");

        var emailNormalizado = _userManager.NormalizeEmail(model.Email);
        if (await _context.Users.AnyAsync(item => item.Id != id && item.NormalizedEmail == emailNormalizado))
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está sendo utilizado.");

        if (model.UsuarioAtual &&
            (model.LojaId != vinculo.LojaId || model.Perfil != perfilAtual || !model.Ativo))
        {
            ModelState.AddModelError(string.Empty,
                "Você não pode alterar a própria loja, perfil ou situação durante esta sessão.");
        }

        if (perfilAtual == InicializadorSistema.PerfilAdministrador &&
            (model.Perfil != InicializadorSistema.PerfilAdministrador || !model.Ativo) &&
            !await ExisteOutroAdministradorAtivoAsync(id))
        {
            ModelState.AddModelError(string.Empty, "O último administrador ativo não pode ser desativado ou ter seu perfil alterado.");
        }

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesAsync(model);
            return View(model);
        }

        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            usuario.Email = model.Email;
            usuario.UserName = model.Email;
            usuario.LockoutEnd = model.Ativo ? null : DateTimeOffset.MaxValue;
            usuario.LockoutEnabled = true;
            Validar(await _userManager.UpdateAsync(usuario));

            if (!string.Equals(perfilAtual, model.Perfil, StringComparison.Ordinal))
            {
                if (perfisAtuais.Count > 0)
                    Validar(await _userManager.RemoveFromRolesAsync(usuario, perfisAtuais));
                Validar(await _userManager.AddToRoleAsync(usuario, model.Perfil));
            }

            vinculo.LojaId = model.LojaId;
            _context.DefinirLojaAtual(model.LojaId);
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            TempData["Sucesso"] = $"Usuário {model.Email} atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            _logger.LogError(ex, "Falha ao editar usuário interno. UsuarioId: {UsuarioId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            await CarregarOpcoesAsync(model);
            return View(model);
        }
    }

    private Task<Models.UsuarioLoja?> ObterVinculoAsync(string id) =>
        _context.UsuarioLoja.IgnoreQueryFilters()
            .Include(item => item.Usuario)
            .SingleOrDefaultAsync(item => item.UsuarioId == id);

    private async Task CarregarOpcoesAsync(UsuarioAdminEdicaoViewModel model)
    {
        ViewBag.Lojas = new SelectList(
            await _context.Loja.AsNoTracking().Where(loja => loja.Ativa).OrderBy(loja => loja.Nome).ToListAsync(),
            "Id", "Nome", model.LojaId);
        ViewBag.Perfis = new SelectList(InicializadorSistema.Perfis, model.Perfil);
    }

    private async Task<bool> ExisteOutroAdministradorAtivoAsync(string usuarioId)
    {
        var administradores = await _userManager.GetUsersInRoleAsync(InicializadorSistema.PerfilAdministrador);
        return administradores.Any(usuario => usuario.Id != usuarioId && UsuarioAtivo(usuario));
    }

    private static bool UsuarioAtivo(IdentityUser usuario) =>
        usuario.LockoutEnd == null || usuario.LockoutEnd <= DateTimeOffset.UtcNow;

    private static void Validar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
    }
}
