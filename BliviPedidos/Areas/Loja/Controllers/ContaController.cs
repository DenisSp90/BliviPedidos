using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Dtos.Publico;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;
using BliviPedidos.Models;
using Microsoft.AspNetCore.RateLimiting;
using BliviPedidos.Seguranca;

namespace BliviPedidos.Areas.Loja.Controllers;

[Area("Loja")]
public sealed class ContaController : Controller
{
    public const string ClaimNomeConsumidor = "nome_consumidor";
    public const string ClaimCelularConsumidor = "celular_consumidor";
    private readonly ILojaAtualService _lojaAtualService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender? _emailSender;
    private readonly IEnvioSmsService? _smsSender;
    private readonly ConfirmacaoConsumidorOptions _confirmacaoOptions;
    private readonly IPagamentoService? _pagamentoService;

    public ContaController(
        ILojaAtualService lojaAtualService,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext context,
        IEmailSender? emailSender = null,
        IEnvioSmsService? smsSender = null,
        IOptions<ConfirmacaoConsumidorOptions>? confirmacaoOptions = null,
        IPagamentoService? pagamentoService = null)
    {
        _lojaAtualService = lojaAtualService;
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _confirmacaoOptions = confirmacaoOptions?.Value ?? new ConfirmacaoConsumidorOptions();
        _pagamentoService = pagamentoService;
    }

    [AllowAnonymous]
    [HttpGet("/loja/{lojaSlug}/conta/criar", Name = "CriarContaConsumidorLoja")]
    public async Task<IActionResult> Criar(string lojaSlug, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarLocal(returnUrl, lojaSlug);

        var loja = await _lojaAtualService.ObterLojaAsync();
        return View(new CriarContaConsumidorViewModel
        {
            Loja = ProjetarLoja(loja),
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.ContaConsumidor)]
    [HttpPost("/loja/{lojaSlug}/conta/criar")]
    public async Task<IActionResult> Criar(string lojaSlug, CriarContaConsumidorViewModel model)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        model.Loja = ProjetarLoja(loja);
        if (!ModelState.IsValid)
            return View(model);

        var usuario = new IdentityUser
        {
            UserName = model.Email.Trim().ToLowerInvariant(),
            Email = model.Email.Trim().ToLowerInvariant(),
            EmailConfirmed = !_confirmacaoOptions.ExigirEmail,
            PhoneNumber = model.Celular.Trim(),
            PhoneNumberConfirmed = !_confirmacaoOptions.ExigirTelefone
        };
        var resultado = await _userManager.CreateAsync(usuario, model.Senha);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(string.Empty, erro.Description);
            return View(model);
        }

        var claimResultado = await _userManager.AddClaimsAsync(
            usuario,
            [
                new Claim(ClaimNomeConsumidor, model.Nome.Trim()),
                new Claim(ClaimCelularConsumidor, model.Celular.Trim())
            ]);
        if (!claimResultado.Succeeded)
        {
            await _userManager.DeleteAsync(usuario);
            foreach (var erro in claimResultado.Errors)
                ModelState.AddModelError(string.Empty, erro.Description);
            return View(model);
        }

        try
        {
            if (_confirmacaoOptions.ExigirTelefone)
            {
                if (_smsSender == null)
                    throw new InvalidOperationException("O serviço de confirmação por telefone não está disponível.");
                var codigoTelefone = await _userManager.GenerateChangePhoneNumberTokenAsync(
                    usuario, usuario.PhoneNumber!);
                await _smsSender.EnviarCodigoAsync(
                    usuario.PhoneNumber!, codigoTelefone, HttpContext.RequestAborted);
            }

            if (_confirmacaoOptions.ExigirEmail)
            {
                if (_emailSender == null)
                    throw new InvalidOperationException("O serviço de confirmação por e-mail não está disponível.");
                var codigoEmail = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
                codigoEmail = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codigoEmail));
                var retorno = Url.IsLocalUrl(model.ReturnUrl)
                    ? model.ReturnUrl
                    : Url.RouteUrl("CheckoutLoja", new { lojaSlug });
                var link = Url.Page(
                    "/Account/ConfirmEmail", null,
                    new { area = "Identity", userId = usuario.Id, code = codigoEmail, returnUrl = retorno },
                    Request.Scheme)!;
                await _emailSender.SendEmailAsync(
                    usuario.Email!, "Confirme seu e-mail",
                    $"Confirme sua conta acessando <a href='{HtmlEncoder.Default.Encode(link)}'>este link</a>.");
            }
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(usuario);
            ModelState.AddModelError(string.Empty, $"Não foi possível enviar a confirmação: {ex.Message}");
            return View(model);
        }

        if (_confirmacaoOptions.ExigirTelefone)
            return RedirectToAction(nameof(ConfirmarTelefone), new
            {
                lojaSlug,
                usuarioId = usuario.Id,
                returnUrl = model.ReturnUrl
            });

        if (_confirmacaoOptions.ExigirEmail)
            return RedirectToAction(nameof(ConfirmacaoEnviada), new { lojaSlug });

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        return RedirecionarLocal(model.ReturnUrl, lojaSlug);
    }

    [AllowAnonymous]
    [HttpGet("/loja/{lojaSlug}/conta/confirmacao-enviada", Name = "ConfirmacaoConsumidorEnviadaLoja")]
    public async Task<IActionResult> ConfirmacaoEnviada(string lojaSlug)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        return View(ProjetarLoja(loja));
    }

    [AllowAnonymous]
    [HttpGet("/loja/{lojaSlug}/conta/confirmar-telefone", Name = "ConfirmarTelefoneConsumidorLoja")]
    public async Task<IActionResult> ConfirmarTelefone(
        string lojaSlug, string usuarioId, string? returnUrl = null)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        return View(new ConfirmarTelefoneConsumidorViewModel
        {
            Loja = ProjetarLoja(loja),
            UsuarioId = usuarioId,
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.ContaConsumidor)]
    [HttpPost("/loja/{lojaSlug}/conta/confirmar-telefone")]
    public async Task<IActionResult> ConfirmarTelefone(
        string lojaSlug, ConfirmarTelefoneConsumidorViewModel model)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        model.Loja = ProjetarLoja(loja);
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _userManager.FindByIdAsync(model.UsuarioId);
        if (usuario?.PhoneNumber == null)
        {
            ModelState.AddModelError(string.Empty, "Solicitação de confirmação inválida.");
            return View(model);
        }

        var resultado = await _userManager.ChangePhoneNumberAsync(
            usuario, usuario.PhoneNumber, model.Codigo.Trim());
        if (!resultado.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Codigo), "Código inválido ou expirado.");
            return View(model);
        }

        if (_confirmacaoOptions.ExigirEmail && !usuario.EmailConfirmed)
            return RedirectToAction(nameof(ConfirmacaoEnviada), new { lojaSlug });

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        return RedirecionarLocal(model.ReturnUrl, lojaSlug);
    }

    [AllowAnonymous]
    [HttpGet("/loja/{lojaSlug}/conta/entrar", Name = "EntrarConsumidorLoja")]
    public IActionResult Entrar(string lojaSlug, string? returnUrl = null)
    {
        returnUrl ??= Url.RouteUrl("CheckoutLoja", new { lojaSlug });
        return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/loja/{lojaSlug}/conta/sair", Name = "SairConsumidorLoja")]
    public async Task<IActionResult> Sair(string lojaSlug)
    {
        await _signInManager.SignOutAsync();
        return RedirectToRoute("CatalogoLoja", new { lojaSlug });
    }

    [Authorize]
    [HttpGet("/loja/{lojaSlug}/conta/minha-conta", Name = "MinhaContaConsumidorLoja")]
    public async Task<IActionResult> MinhaConta(string lojaSlug)
    {
        var usuario = await ObterConsumidorAtualAsync();
        if (usuario == null)
            return Forbid();

        var loja = await _lojaAtualService.ObterLojaAsync();
        return View(new MinhaContaConsumidorViewModel
        {
            Loja = ProjetarLoja(loja),
            Dados = new EditarDadosConsumidorViewModel
            {
                Nome = User.FindFirst(ClaimNomeConsumidor)?.Value ?? string.Empty,
                Celular = usuario.PhoneNumber ?? User.FindFirst(ClaimCelularConsumidor)?.Value ?? string.Empty,
                Email = usuario.Email ?? string.Empty
            }
        });
    }

    [Authorize]
    [HttpPost("/loja/{lojaSlug}/conta/minha-conta/dados", Name = "EditarDadosConsumidorLoja")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarDados(string lojaSlug, [Bind(Prefix = "Dados")] EditarDadosConsumidorViewModel model)
    {
        var usuario = await ObterConsumidorAtualAsync();
        if (usuario == null)
            return Forbid();

        model.Nome = (model.Nome ?? string.Empty).Trim();
        model.Celular = (model.Celular ?? string.Empty).Trim();
        model.Email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var emailAlterado = !string.Equals(usuario.Email, model.Email, StringComparison.OrdinalIgnoreCase);
        var celularAlterado = !string.Equals(usuario.PhoneNumber, model.Celular, StringComparison.Ordinal);
        var emailNormalizado = _userManager.NormalizeEmail(model.Email);
        if (await _context.Users.AnyAsync(item => item.Id != usuario.Id && item.NormalizedEmail == emailNormalizado))
            ModelState.AddModelError("Dados.Email", "Este e-mail já está sendo utilizado.");
        if (emailAlterado && _confirmacaoOptions.ExigirEmail && _emailSender == null)
            ModelState.AddModelError("Dados.Email", "A confirmação de e-mail não está disponível no momento.");
        if (celularAlterado && _confirmacaoOptions.ExigirTelefone && _smsSender == null)
            ModelState.AddModelError("Dados.Celular", "A confirmação por celular não está disponível no momento.");

        if (!ModelState.IsValid)
            return await ExibirMinhaContaComErrosAsync(model);

        var claims = await _userManager.GetClaimsAsync(usuario);
        var claimNome = claims.FirstOrDefault(item => item.Type == ClaimNomeConsumidor);
        var claimCelular = claims.FirstOrDefault(item => item.Type == ClaimCelularConsumidor);

        usuario.Email = model.Email;
        usuario.UserName = model.Email;
        usuario.PhoneNumber = model.Celular;
        if (emailAlterado)
            usuario.EmailConfirmed = !_confirmacaoOptions.ExigirEmail;
        if (celularAlterado)
            usuario.PhoneNumberConfirmed = !_confirmacaoOptions.ExigirTelefone;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(string.Empty, erro.Description);
            return await ExibirMinhaContaComErrosAsync(model);
        }

        if (claimNome == null)
            resultado = await _userManager.AddClaimAsync(usuario, new Claim(ClaimNomeConsumidor, model.Nome));
        else if (claimNome.Value != model.Nome)
            resultado = await _userManager.ReplaceClaimAsync(usuario, claimNome, new Claim(ClaimNomeConsumidor, model.Nome));
        if (!resultado.Succeeded)
            return await ExibirMinhaContaComErrosAsync(model, resultado);

        if (claimCelular == null)
            resultado = await _userManager.AddClaimAsync(usuario, new Claim(ClaimCelularConsumidor, model.Celular));
        else if (claimCelular.Value != model.Celular)
            resultado = await _userManager.ReplaceClaimAsync(usuario, claimCelular, new Claim(ClaimCelularConsumidor, model.Celular));
        if (!resultado.Succeeded)
            return await ExibirMinhaContaComErrosAsync(model, resultado);

        try
        {
            var retorno = Url.RouteUrl("MinhaContaConsumidorLoja", new { lojaSlug });
            if (emailAlterado && _confirmacaoOptions.ExigirEmail)
            {
                var codigoEmail = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
                codigoEmail = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codigoEmail));
                var link = Url.Page(
                    "/Account/ConfirmEmail", null,
                    new { area = "Identity", userId = usuario.Id, code = codigoEmail, returnUrl = retorno },
                    Request.Scheme)!;
                await _emailSender!.SendEmailAsync(
                    usuario.Email!, "Confirme seu novo e-mail",
                    $"Confirme o novo e-mail acessando <a href='{HtmlEncoder.Default.Encode(link)}'>este link</a>.");
            }
            if (celularAlterado && _confirmacaoOptions.ExigirTelefone)
            {
                var codigoTelefone = await _userManager.GenerateChangePhoneNumberTokenAsync(usuario, model.Celular);
                await _smsSender!.EnviarCodigoAsync(model.Celular, codigoTelefone, HttpContext.RequestAborted);
                return RedirectToAction(nameof(ConfirmarTelefone), new
                {
                    lojaSlug,
                    usuarioId = usuario.Id,
                    returnUrl = retorno
                });
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Os dados foram salvos, mas não foi possível enviar a confirmação: {ex.Message}");
            return await ExibirMinhaContaComErrosAsync(model);
        }

        await _signInManager.RefreshSignInAsync(usuario);
        TempData["ContaSucesso"] = "Seus dados foram atualizados.";
        return RedirectToRoute("MinhaContaConsumidorLoja", new { lojaSlug });
    }

    [Authorize]
    [HttpPost("/loja/{lojaSlug}/conta/minha-conta/senha", Name = "AlterarSenhaConsumidorLoja")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(string lojaSlug, [Bind(Prefix = "Senha")] AlterarSenhaConsumidorViewModel model)
    {
        var usuario = await ObterConsumidorAtualAsync();
        if (usuario == null)
            return Forbid();
        if (!ModelState.IsValid)
            return await ExibirMinhaContaComErrosAsync(senha: model);

        var resultado = await _userManager.ChangePasswordAsync(usuario, model.SenhaAtual, model.NovaSenha);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(string.Empty,
                    erro.Code == "PasswordMismatch" ? "A senha atual está incorreta." : erro.Description);
            return await ExibirMinhaContaComErrosAsync(senha: model);
        }

        await _signInManager.RefreshSignInAsync(usuario);
        TempData["ContaSucesso"] = "Sua senha foi alterada com sucesso.";
        return RedirectToRoute("MinhaContaConsumidorLoja", new { lojaSlug });
    }

    [Authorize]
    [HttpGet("/loja/{lojaSlug}/meus-pedidos", Name = "MeusPedidosConsumidorLoja")]
    public async Task<IActionResult> MeusPedidos(string lojaSlug)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var pedidos = await _context.Pedido
            .AsNoTracking()
            .Where(pedido => pedido.ConsumidorUsuarioId == usuarioId)
            .OrderByDescending(pedido => pedido.DataPedido)
            .Select(pedido => new ResumoPedidoConsumidorViewModel
            {
                PedidoId = pedido.Id,
                CodigoPublico = pedido.CodigoPublico!,
                DataPedido = pedido.DataPedido,
                Status = pedido.Status,
                StatusPagamento = pedido.StatusPagamento,
                Total = pedido.ValorTotalPedido
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new MeusPedidosConsumidorViewModel
        {
            Loja = ProjetarLoja(loja),
            Pedidos = pedidos
        });
    }

    [Authorize]
    [HttpGet("/loja/{lojaSlug}/meus-pedidos/{codigoPublico}", Name = "PedidoConsumidorDetalheLoja")]
    public async Task<IActionResult> Pedido(string lojaSlug, string codigoPublico)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var pedido = await _context.Pedido
            .AsNoTracking()
            .Where(item => item.ConsumidorUsuarioId == usuarioId && item.CodigoPublico == codigoPublico)
            .Select(item => new PedidoConsumidorDetalheViewModel
            {
                PedidoId = item.Id,
                CodigoPublico = item.CodigoPublico!,
                DataPedido = item.DataPedido,
                Status = item.Status,
                StatusPagamento = item.StatusPagamento,
                Total = item.ValorTotalPedido,
                Itens = item.Itens.Select(produto => new ItemPedidoConsumidorViewModel
                {
                    Produto = produto.Produto.Nome,
                    Foto = produto.Produto.Foto,
                    Quantidade = produto.Quantidade,
                    PrecoUnitario = produto.PrecoUnitario
                }).ToList()
            })
            .SingleOrDefaultAsync(HttpContext.RequestAborted);

        if (pedido == null)
            return NotFound();

        if (pedido.StatusPagamento == StatusPagamento.AguardandoPagamento)
        {
            try
            {
                var pagamento = _pagamentoService?.GerarPix(loja, pedido.Total, pedido.CodigoPublico);
                pedido.PixCopiaECola = pagamento?.CopiaECola;
                pedido.PixQrCodeBase64 = pagamento?.QrCodeBase64;
                if (pagamento == null)
                    pedido.PagamentoErro = "O serviço de pagamento não está disponível.";
            }
            catch (Exception ex)
            {
                pedido.PagamentoErro = ex.Message;
            }
        }

        pedido.Loja = ProjetarLoja(loja);
        return View(pedido);
    }

    private IActionResult RedirecionarLocal(string? returnUrl, string lojaSlug) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToRoute("CheckoutLoja", new { lojaSlug });

    private async Task<IdentityUser?> ObterConsumidorAtualAsync()
    {
        if (User.FindFirst(ClaimNomeConsumidor) == null)
            return null;
        return await _userManager.GetUserAsync(User);
    }

    private async Task<IActionResult> ExibirMinhaContaComErrosAsync(
        EditarDadosConsumidorViewModel? dados = null,
        IdentityResult? resultado = null,
        AlterarSenhaConsumidorViewModel? senha = null)
    {
        if (resultado != null)
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(string.Empty, erro.Description);
        var usuario = await _userManager.GetUserAsync(User);
        var loja = await _lojaAtualService.ObterLojaAsync();
        return View("MinhaConta", new MinhaContaConsumidorViewModel
        {
            Loja = ProjetarLoja(loja),
            Dados = dados ?? new EditarDadosConsumidorViewModel
            {
                Nome = User.FindFirst(ClaimNomeConsumidor)?.Value ?? string.Empty,
                Celular = usuario?.PhoneNumber ?? string.Empty,
                Email = usuario?.Email ?? string.Empty
            },
            Senha = senha ?? new AlterarSenhaConsumidorViewModel()
        });
    }

    private static LojaPublicaDto ProjetarLoja(Models.Loja loja) => new()
    {
        Nome = loja.Nome,
        Slug = loja.Slug,
        LogoUrl = loja.LogoUrl,
        CorPrimaria = loja.CorPrimaria,
        CorSecundaria = loja.CorSecundaria,
        Whatsapp = loja.Whatsapp,
        Descricao = loja.Descricao,
        EmailContato = loja.EmailContato,
        InstagramUrl = loja.InstagramUrl
    };
}
