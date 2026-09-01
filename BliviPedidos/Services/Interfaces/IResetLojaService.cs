namespace BliviPedidos.Services.Interfaces;

public interface IResetLojaService
{
    Task<ResetLojaResumo> ObterResumoAsync(int lojaId, CancellationToken cancellationToken = default);
    Task<ResetLojaResultado> ResetarAsync(int lojaId, CancellationToken cancellationToken = default);
}

public sealed record ResetLojaResumo(
    int LojaId,
    string LojaNome,
    int Categorias,
    int Produtos,
    int Clientes,
    int Pedidos,
    int Movimentacoes,
    int Imagens);

public sealed record ResetLojaResultado(ResetLojaResumo Resumo, string BackupSeguranca);
