namespace BliviPedidos.Services.Interfaces;

public interface IRestauracaoBackupLojaService
{
    Task<RestauracaoBackupResultado> RestaurarAsync(
        Stream arquivo,
        long tamanhoArquivo,
        CancellationToken cancellationToken = default);
}

public sealed record RestauracaoBackupResultado(
    string BackupSeguranca,
    int Categorias,
    int Produtos,
    int Clientes,
    int Pedidos,
    int Imagens);
