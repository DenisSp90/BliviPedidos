namespace BliviPedidos.Services.Interfaces;

public interface IBackupLojaService
{
    Task<BackupLojaArquivo> GerarAsync(CancellationToken cancellationToken = default);
}

public sealed record BackupLojaArquivo(string NomeArquivo, byte[] Conteudo);
