using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class ResetLojaService : IResetLojaService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IBackupLojaService _backupLojaService;

    public ResetLojaService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IBackupLojaService backupLojaService)
    {
        _context = context;
        _environment = environment;
        _backupLojaService = backupLojaService;
    }

    public async Task<ResetLojaResumo> ObterResumoAsync(
        int lojaId,
        CancellationToken cancellationToken = default)
    {
        if (lojaId <= 0)
            throw new KeyNotFoundException("Loja não encontrada.");

        var lojaNome = await _context.Loja
            .AsNoTracking()
            .Where(item => item.Id == lojaId)
            .Select(item => item.Nome)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Loja não encontrada.");

        var lojaAnterior = _context.LojaIdAtual;
        try
        {
            _context.DefinirLojaAtual(lojaId);
            return new ResetLojaResumo(
                lojaId,
                lojaNome,
                await _context.Categoria.CountAsync(cancellationToken),
                await _context.Produto.CountAsync(cancellationToken),
                await _context.Cliente.CountAsync(cancellationToken),
                await _context.Pedido.CountAsync(cancellationToken),
                await _context.ProdutoMovimentacao.CountAsync(cancellationToken),
                ContarImagens(lojaId));
        }
        finally
        {
            _context.DefinirLojaAtual(lojaAnterior);
        }
    }

    public async Task<ResetLojaResultado> ResetarAsync(
        int lojaId,
        CancellationToken cancellationToken = default)
    {
        await OperacaoDadosLojaLock.Global.WaitAsync(cancellationToken);
        var lojaAnterior = _context.LojaIdAtual;
        try
        {
            var resumo = await ObterResumoAsync(lojaId, cancellationToken);
            _context.DefinirLojaAtual(lojaId);
            _context.ChangeTracker.Clear();

            var backup = await _backupLojaService.GerarAsync(cancellationToken);
            var nomeBackup = await SalvarBackupSegurancaAsync(lojaId, backup, cancellationToken);
            await ResetarDadosEImagensAsync(lojaId, cancellationToken);

            return new ResetLojaResultado(resumo, nomeBackup);
        }
        finally
        {
            _context.ChangeTracker.Clear();
            _context.DefinirLojaAtual(lojaAnterior);
            OperacaoDadosLojaLock.Global.Release();
        }
    }

    private async Task ResetarDadosEImagensAsync(int lojaId, CancellationToken cancellationToken)
    {
        var pastaImagens = ObterPastaImagens(lojaId);
        var pastaAnterior = pastaImagens + $".reset-{Guid.NewGuid():N}";
        var imagensMovidas = false;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (Directory.Exists(pastaImagens))
            {
                Directory.Move(pastaImagens, pastaAnterior);
                imagensMovidas = true;
            }

            await _context.ProdutoMovimentacao.ExecuteDeleteAsync(cancellationToken);
            await _context.Set<ItemPedido>().ExecuteDeleteAsync(cancellationToken);
            await _context.Set<Cadastro>().ExecuteDeleteAsync(cancellationToken);
            await _context.Pedido.ExecuteDeleteAsync(cancellationToken);
            await _context.Produto.ExecuteDeleteAsync(cancellationToken);
            await _context.Categoria.ExecuteDeleteAsync(cancellationToken);
            await _context.Cliente.ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (Directory.Exists(pastaAnterior))
            {
                try
                {
                    Directory.Delete(pastaAnterior, recursive: true);
                }
                catch (IOException)
                {
                    // Os dados já foram removidos; a pasta temporária pode ser limpa posteriormente.
                }
                catch (UnauthorizedAccessException)
                {
                    // Os dados já foram removidos; a pasta temporária pode ser limpa posteriormente.
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (imagensMovidas && Directory.Exists(pastaAnterior) && !Directory.Exists(pastaImagens))
                Directory.Move(pastaAnterior, pastaImagens);
            throw;
        }
    }

    private async Task<string> SalvarBackupSegurancaAsync(
        int lojaId,
        BackupLojaArquivo backup,
        CancellationToken cancellationToken)
    {
        var pasta = Path.Combine(_environment.ContentRootPath, "App_Data", "backups", lojaId.ToString());
        Directory.CreateDirectory(pasta);
        var nome = backup.NomeArquivo;
        var caminho = Path.Combine(pasta, nome);
        if (File.Exists(caminho))
        {
            nome = $"{Path.GetFileNameWithoutExtension(nome)}-{Guid.NewGuid():N}.zip";
            caminho = Path.Combine(pasta, nome);
        }

        await File.WriteAllBytesAsync(caminho, backup.Conteudo, cancellationToken);
        return nome;
    }

    private int ContarImagens(int lojaId)
    {
        var pasta = ObterPastaImagens(lojaId);
        return Directory.Exists(pasta)
            ? Directory.EnumerateFiles(pasta, "*", SearchOption.TopDirectoryOnly).Count()
            : 0;
    }

    private string ObterPastaImagens(int lojaId)
    {
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "produtos", lojaId.ToString());
    }
}
