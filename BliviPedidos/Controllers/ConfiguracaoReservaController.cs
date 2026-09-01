using BliviPedidos.Models.ViewModels;
using BliviPedidos.Seguranca;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Vendas)]
public sealed class ConfiguracaoReservaController : Controller
{
    private readonly ILojaAtualService _lojaAtualService;
    private readonly IConfiguracaoReservaLojaService _configuracaoReservaLojaService;

    public ConfiguracaoReservaController(
        ILojaAtualService lojaAtualService,
        IConfiguracaoReservaLojaService configuracaoReservaLojaService)
    {
        _lojaAtualService = lojaAtualService;
        _configuracaoReservaLojaService = configuracaoReservaLojaService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var lojaId = await _lojaAtualService.ObterLojaIdAsync();
        var expiracaoMinutos = await _configuracaoReservaLojaService
            .ObterExpiracaoMinutosAsync(lojaId, cancellationToken);

        return View(new ConfiguracaoReservaEstoqueViewModel
        {
            ExpiracaoMinutos = expiracaoMinutos
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(
        ConfiguracaoReservaEstoqueViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), model);

        var lojaId = await _lojaAtualService.ObterLojaIdAsync();
        await _configuracaoReservaLojaService.SalvarExpiracaoMinutosAsync(
            lojaId,
            model.ExpiracaoMinutos,
            cancellationToken);
        TempData["MensagemSucesso"] = "Prazo para pagamento atualizado para esta loja.";

        return RedirectToAction(nameof(Index));
    }
}
