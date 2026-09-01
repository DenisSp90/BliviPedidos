using BliviPedidos.Seguranca;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public sealed class BackupController : Controller
{
    private readonly IBackupLojaService _backupLojaService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupLojaService backupLojaService, ILogger<BackupController> logger)
    {
        _backupLojaService = backupLojaService;
        _logger = logger;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RestauracaoBackupLojaService.TamanhoMaximoArquivo + 1024 * 1024)]
    public async Task<IActionResult> Restaurar(
        IFormFile? arquivo,
        bool confirmarSubstituicao,
        [FromServices] IRestauracaoBackupLojaService restauracaoService,
        CancellationToken cancellationToken)
    {
        if (!confirmarSubstituicao)
        {
            TempData["BackupErro"] = "Confirme que deseja substituir os dados atuais da loja.";
            return RedirectToAction(nameof(Index));
        }

        if (arquivo is null || arquivo.Length == 0
            || !string.Equals(Path.GetExtension(arquivo.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            TempData["BackupErro"] = "Selecione um arquivo de backup ZIP válido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = arquivo.OpenReadStream();
            var resultado = await restauracaoService.RestaurarAsync(
                stream,
                arquivo.Length,
                cancellationToken);
            TempData["BackupSucesso"] = $"Restauração concluída: {resultado.Produtos} produtos, "
                + $"{resultado.Clientes} clientes e {resultado.Pedidos} pedidos. "
                + $"Backup de segurança criado: {resultado.BackupSeguranca}.";
        }
        catch (InvalidDataException ex)
        {
            TempData["BackupErro"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao restaurar o backup da loja atual.");
            TempData["BackupErro"] = "Não foi possível concluir a restauração. Os dados atuais foram preservados.";
        }

        return RedirectToAction(nameof(Index));
    }
}
