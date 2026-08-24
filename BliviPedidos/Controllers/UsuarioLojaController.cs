using BliviPedidos.Areas.Admin.Controllers;
using BliviPedidos.Areas.Loja.Controllers;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Seguranca;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Vendas)]
public sealed class UsuarioLojaController : Controller
{
    private static readonly string[] PerfisPermitidos =
        [InicializadorSistema.PerfilVendedor, InicializadorSistema.PerfilEstoquista];

    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILojaAtualService _lojaAtualService;
    private readonly ILogger<UsuarioLojaController> _logger;

    public UsuarioLojaController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILojaAtualService lojaAtualService,
        ILogger<UsuarioLojaController> logger)
    {
        _context = context;
        _userManager = userManager;
        _lojaAtualService = lojaAtualService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var lojaId = await _lojaAtualService.ObterLojaIdAsync();
        var atualId = _userManager.GetUserId(User);
        var vinculos = await _context.UsuarioLoja.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.LojaId == lojaId)
            .ToDictionaryAsync(item => item.UsuarioId);
        var consumidoresIds = await _context.Pedido.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.LojaId == lojaId && item.ConsumidorUsuarioId != null)
            .Select(item => item.ConsumidorUsuarioId!).Distinct().ToListAsync();
        var ids = vinculos.Keys.Concat(consumidoresIds).Distinct().ToList();
        var contas = await _context.Users.AsNoTracking()
            .Where(item => ids.Contains(item.Id)).OrderBy(item => item.Email).ToListAsync();

        var model = new List<UsuarioLojaListaViewModel>(contas.Count);
        foreach (var conta in contas)
        {
            var interno = vinculos.ContainsKey(conta.Id);
            var perfis = await _userManager.GetRolesAsync(conta);
            var perfil = interno ? perfis.FirstOrDefault() ?? "Sem perfil" : "Consumidor";
            model.Add(new UsuarioLojaListaViewModel
            {
                Id = conta.Id,
                Email = conta.Email ?? conta.UserName ?? "—",
                Telefone = conta.PhoneNumber ?? string.Empty,
                Tipo = interno ? "Interno" : "Consumidor",
                Perfil = perfil,
                Ativo = Ativo(conta),
                PodeEditar = perfil != InicializadorSistema.PerfilAdministrador,
                UsuarioAtual = conta.Id == atualId
            });
        }
        return View(model);
    }

    public IActionResult Criar()
    {
        CarregarPerfis();
        return View(new CriarUsuarioLojaViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(CriarUsuarioLojaViewModel model)
    {
        Normalizar(model);
        if (!PerfisPermitidos.Contains(model.Perfil))
            ModelState.AddModelError(nameof(model.Perfil), "Selecione Vendedor ou Estoquista.");
        if (!ModelState.IsValid)
        {
            CarregarPerfis(model.Perfil);
            return View(model);
        }

        var usuario = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            PhoneNumber = model.Telefone,
            PhoneNumberConfirmed = true
        };
        var resultado = await _userManager.CreateAsync(usuario, model.Senha);
        if (!resultado.Succeeded)
        {
            AdicionarErros(resultado);
            CarregarPerfis(model.Perfil);
            return View(model);
        }

        try
        {
            Validar(await _userManager.AddToRoleAsync(usuario, model.Perfil));
            _context.UsuarioLoja.Add(new UsuarioLoja
            {
                UsuarioId = usuario.Id,
                LojaId = await _lojaAtualService.ObterLojaIdAsync()
            });
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = $"Usuário {model.Email} criado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(usuario);
            _logger.LogError(ex, "Falha ao criar usuário pelo vendedor. Email: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Não foi possível criar o usuário.");
            CarregarPerfis(model.Perfil);
            return View(model);
        }
    }

    public async Task<IActionResult> Editar(string id)
    {
        var acesso = await ObterAcessoAsync(id);
        if (!acesso.Permitido) return NotFound();
        if (acesso.Administrador) return Forbid();
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null) return NotFound();
        var perfis = await _userManager.GetRolesAsync(usuario);
        var model = new EditarUsuarioLojaViewModel
        {
            Id = id, Email = usuario.Email ?? string.Empty, Telefone = usuario.PhoneNumber ?? string.Empty,
            Perfil = acesso.Interno ? perfis.FirstOrDefault() : null, Ativo = Ativo(usuario),
            Interno = acesso.Interno, UsuarioAtual = id == _userManager.GetUserId(User)
        };
        CarregarPerfis(model.Perfil);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(string id, EditarUsuarioLojaViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var acesso = await ObterAcessoAsync(id);
        if (!acesso.Permitido) return NotFound();
        if (acesso.Administrador) return Forbid();
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null) return NotFound();
        var perfisAtuais = await _userManager.GetRolesAsync(usuario);
        var perfilAtual = perfisAtuais.FirstOrDefault();
        model.Interno = acesso.Interno;
        model.UsuarioAtual = id == _userManager.GetUserId(User);
        model.Email = model.Email.Trim().ToLowerInvariant();
        model.Telefone = model.Telefone.Trim();

        if (model.Interno && !PerfisPermitidos.Contains(model.Perfil ?? string.Empty))
            ModelState.AddModelError(nameof(model.Perfil), "Selecione Vendedor ou Estoquista.");
        if (model.UsuarioAtual && (!model.Ativo || model.Perfil != perfilAtual))
            ModelState.AddModelError(string.Empty, "Você não pode desativar ou alterar o próprio perfil nesta sessão.");
        var normalizado = _userManager.NormalizeEmail(model.Email);
        if (await _context.Users.AnyAsync(item => item.Id != id && item.NormalizedEmail == normalizado))
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está em uso.");
        if (!ModelState.IsValid)
        {
            CarregarPerfis(model.Perfil);
            return View(model);
        }

        usuario.Email = model.Email;
        usuario.UserName = model.Email;
        usuario.PhoneNumber = model.Telefone;
        usuario.LockoutEnabled = true;
        usuario.LockoutEnd = model.Ativo ? null : DateTimeOffset.MaxValue;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            AdicionarErros(resultado);
            CarregarPerfis(model.Perfil);
            return View(model);
        }
        if (model.Interno && perfilAtual != model.Perfil)
        {
            if (perfisAtuais.Count > 0) Validar(await _userManager.RemoveFromRolesAsync(usuario, perfisAtuais));
            Validar(await _userManager.AddToRoleAsync(usuario, model.Perfil!));
        }
        if (!model.Interno)
        {
            var claimCelular = (await _userManager.GetClaimsAsync(usuario))
                .FirstOrDefault(item => item.Type == ContaController.ClaimCelularConsumidor);
            if (claimCelular != null && claimCelular.Value != model.Telefone)
                Validar(await _userManager.ReplaceClaimAsync(
                    usuario, claimCelular,
                    new Claim(ContaController.ClaimCelularConsumidor, model.Telefone)));
        }
        TempData["Sucesso"] = $"Usuário {model.Email} atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RedefinirSenhaPadrao(string id)
    {
        var acesso = await ObterAcessoAsync(id);
        if (!acesso.Permitido) return NotFound();
        if (acesso.Administrador) return Forbid();
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null) return NotFound();
        var senha = UsuarioController.CriarSenhaPadrao(usuario.PhoneNumber);
        if (senha == null)
        {
            TempData["Erro"] = "O usuário não possui celular válido cadastrado.";
            return RedirectToAction(nameof(Index));
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await _userManager.ResetPasswordAsync(usuario, token, senha);
        if (!resultado.Succeeded)
        {
            TempData["Erro"] = string.Join(" ", resultado.Errors.Select(item => item.Description));
            return RedirectToAction(nameof(Index));
        }
        await _userManager.UpdateSecurityStampAsync(usuario);
        _logger.LogWarning("Senha redefinida pelo vendedor. UsuarioId: {UsuarioId}, Operador: {Operador}", id, User.Identity?.Name);
        TempData["Sucesso"] = $"Senha de {usuario.Email} redefinida.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Permitido, bool Interno, bool Administrador)> ObterAcessoAsync(string usuarioId)
    {
        var lojaId = await _lojaAtualService.ObterLojaIdAsync();
        var interno = await _context.UsuarioLoja.IgnoreQueryFilters().AnyAsync(item => item.UsuarioId == usuarioId && item.LojaId == lojaId);
        var consumidor = !interno && await _context.Pedido.IgnoreQueryFilters()
            .AnyAsync(item => item.LojaId == lojaId && item.ConsumidorUsuarioId == usuarioId);
        if (!interno && !consumidor) return (false, false, false);
        var usuario = await _userManager.FindByIdAsync(usuarioId);
        return (usuario != null, interno, usuario != null && await _userManager.IsInRoleAsync(usuario, InicializadorSistema.PerfilAdministrador));
    }

    private void CarregarPerfis(string? selecionado = null) =>
        ViewBag.Perfis = new SelectList(PerfisPermitidos, selecionado);
    private void AdicionarErros(IdentityResult resultado)
    { foreach (var erro in resultado.Errors) ModelState.AddModelError(string.Empty, erro.Description); }
    private static bool Ativo(IdentityUser usuario) => usuario.LockoutEnd == null || usuario.LockoutEnd <= DateTimeOffset.UtcNow;
    private static void Normalizar(CriarUsuarioLojaViewModel model)
    { model.Email = model.Email.Trim().ToLowerInvariant(); model.Telefone = model.Telefone.Trim(); }
    private static void Validar(IdentityResult resultado)
    { if (!resultado.Succeeded) throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(item => item.Description))); }
}
