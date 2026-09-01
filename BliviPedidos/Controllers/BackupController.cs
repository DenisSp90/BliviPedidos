using BliviPedidos.Seguranca;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public sealed class BackupController : Controller
{
    private readonly IBackupLojaService _backupLojaService;

    public BackupController(IBackupLojaService backupLojaService)
    {
        _backupLojaService = backupLojaService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gerar(CancellationToken cancellationToken)
    {
        var backup = await _backupLojaService.GerarAsync(cancellationToken);
        return File(backup.Conteudo, "application/zip", backup.NomeArquivo);
    }
}
