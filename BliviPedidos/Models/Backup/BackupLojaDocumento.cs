namespace BliviPedidos.Models.Backup;

public sealed class BackupLojaDocumento
{
    public string Formato { get; init; } = "BliviPedidos.BackupLoja";
    public int Versao { get; init; } = 1;
    public DateTime GeradoEmUtc { get; init; }
    public BackupLojaDto Loja { get; init; } = new();
    public List<BackupCategoriaDto> Categorias { get; init; } = [];
    public List<BackupProdutoDto> Produtos { get; init; } = [];
    public List<BackupClienteDto> Clientes { get; init; } = [];
    public List<BackupPedidoDto> Pedidos { get; init; } = [];
    public List<BackupCadastroDto> Cadastros { get; init; } = [];
    public List<BackupItemPedidoDto> ItensPedido { get; init; } = [];
    public List<BackupMovimentacaoDto> MovimentacoesEstoque { get; init; } = [];
}

public sealed class BackupLojaDto
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Dominio { get; init; }
    public string? LogoUrl { get; init; }
    public string? CorPrimaria { get; init; }
    public string? CorSecundaria { get; init; }
    public string? Descricao { get; init; }
    public string? Whatsapp { get; init; }
    public string? EmailContato { get; init; }
    public string? InstagramUrl { get; init; }
    public bool Ativa { get; init; }
}

public sealed record BackupCategoriaDto(int Id, string Nome, bool IsAtivo);

public sealed record BackupProdutoDto(
    int Id,
    string? Codigo,
    string Nome,
    decimal PrecoVenda,
    decimal PrecoPago,
    int Quantidade,
    string? Tamanho,
    string? CodeBar,
    string? Foto,
    bool IsAtivo,
    int? CategoriaId);

public sealed record BackupClienteDto(
    int Id,
    string Nome,
    string? Email,
    string Telefone,
    string? ResponsavelCerimar,
    string? Turma,
    string? Endereco,
    string? Complemento,
    string? Bairro,
    string? Municipio,
    string? UF,
    string? CEP);

public sealed record BackupPedidoDto(
    int Id,
    string? CodigoPublico,
    StatusPedido Status,
    StatusPagamento StatusPagamento,
    DateTime? DataPedido,
    DateTime? ReservaExpiraEm,
    DateTime? DataPagamento,
    decimal ValorTotalPedido,
    string? EmailResponsavel);

public sealed record BackupCadastroDto(
    int Id,
    int PedidoId,
    int? ClienteId,
    string Nome,
    string? Email,
    string Telefone,
    string? ResponsavelCerimar,
    string? Turma,
    string? Endereco,
    string? Complemento,
    string? Bairro,
    string? Municipio,
    string? UF,
    string? CEP);

public sealed record BackupItemPedidoDto(
    int Id,
    int PedidoId,
    int ProdutoId,
    int Quantidade,
    decimal PrecoUnitario);

public sealed record BackupMovimentacaoDto(
    int Id,
    int? ProdutoId,
    int? PedidoId,
    DateTime Data,
    int Quantidade,
    string Tipo,
    string Ator,
    string Origem,
    string? Observacao);

public sealed class BackupLojaManifesto
{
    public string Formato { get; init; } = "BliviPedidos.BackupLoja";
    public int Versao { get; init; } = 1;
    public DateTime GeradoEmUtc { get; init; }
    public int LojaId { get; init; }
    public string LojaNome { get; init; } = string.Empty;
    public Dictionary<string, int> Quantidades { get; init; } = [];
    public int QuantidadeImagens { get; init; }
    public string[] DadosExcluidos { get; init; } = ["senhas", "contas de acesso", "chave PIX"];
}
