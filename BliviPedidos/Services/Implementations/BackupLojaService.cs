using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.Backup;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class BackupLojaService : IBackupLojaService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public BackupLojaService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<BackupLojaArquivo> GerarAsync(CancellationToken cancellationToken = default)
    {
        var geradoEmUtc = DateTime.UtcNow;
        var lojaId = _context.LojaIdAtual;
        var documento = await CriarDocumentoAsync(lojaId, geradoEmUtc, cancellationToken);
        var imagens = ObterImagens(lojaId);

        await using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifesto = new BackupLojaManifesto
            {
                GeradoEmUtc = geradoEmUtc,
                LojaId = documento.Loja.Id,
                LojaNome = documento.Loja.Nome,
                QuantidadeImagens = imagens.Count,
                Quantidades = new Dictionary<string, int>
                {
                    ["categorias"] = documento.Categorias.Count,
                    ["produtos"] = documento.Produtos.Count,
                    ["clientes"] = documento.Clientes.Count,
                    ["pedidos"] = documento.Pedidos.Count,
                    ["cadastros"] = documento.Cadastros.Count,
                    ["itensPedido"] = documento.ItensPedido.Count,
                    ["movimentacoesEstoque"] = documento.MovimentacoesEstoque.Count
                }
            };

            await GravarJsonAsync(zip, "manifesto.json", manifesto, cancellationToken);
            await GravarJsonAsync(zip, "dados.json", documento, cancellationToken);

            foreach (var imagem in imagens)
            {
                var entrada = zip.CreateEntry($"imagens/produtos/{Path.GetFileName(imagem)}", CompressionLevel.Optimal);
                await using var destino = entrada.Open();
                await using var origem = new FileStream(imagem, FileMode.Open, FileAccess.Read, FileShare.Read);
                await origem.CopyToAsync(destino, cancellationToken);
            }
        }

        var slugSeguro = string.Concat(documento.Loja.Slug.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-'));
        var nome = $"backup-{slugSeguro}-{geradoEmUtc:yyyyMMdd-HHmmss}.zip";
        return new BackupLojaArquivo(nome, memoria.ToArray());
    }

    private async Task<BackupLojaDocumento> CriarDocumentoAsync(
        int lojaId,
        DateTime geradoEmUtc,
        CancellationToken cancellationToken)
    {
        var loja = await _context.Loja
            .AsNoTracking()
            .Where(item => item.Id == lojaId)
            .Select(item => new BackupLojaDto
            {
                Id = item.Id,
                Nome = item.Nome,
                Slug = item.Slug,
                Dominio = item.Dominio,
                LogoUrl = item.LogoUrl,
                CorPrimaria = item.CorPrimaria,
                CorSecundaria = item.CorSecundaria,
                Descricao = item.Descricao,
                Whatsapp = item.Whatsapp,
                EmailContato = item.EmailContato,
                InstagramUrl = item.InstagramUrl,
                Ativa = item.Ativa
            })
            .SingleAsync(cancellationToken);

        return new BackupLojaDocumento
        {
            GeradoEmUtc = geradoEmUtc,
            Loja = loja,
            Categorias = await _context.Categoria.AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupCategoriaDto(item.Id, item.Nome, item.IsAtivo))
                .ToListAsync(cancellationToken),
            Produtos = await _context.Produto.AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupProdutoDto(
                    item.Id, item.Codigo, item.Nome, item.PrecoVenda, item.PrecoPago,
                    item.Quantidade, item.Tamanho, item.CodeBar, item.Foto,
                    item.IsAtivo, item.CategoriaId))
                .ToListAsync(cancellationToken),
            Clientes = await _context.Cliente.AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupClienteDto(
                    item.Id, item.Nome, item.Email, item.Telefone, item.ResponsavelCerimar,
                    item.Turma, item.Endereco, item.Complemento, item.Bairro,
                    item.Municipio, item.UF, item.CEP))
                .ToListAsync(cancellationToken),
            Pedidos = await _context.Pedido.AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupPedidoDto(
                    item.Id, item.CodigoPublico, item.Status, item.StatusPagamento,
                    item.DataPedido, item.ReservaExpiraEm, item.DataPagamento,
                    item.ValorTotalPedido, item.EmailResponsavel, item.ConsumidorUsuarioId))
                .ToListAsync(cancellationToken),
            Cadastros = await _context.Set<Cadastro>().AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupCadastroDto(
                    item.Id, item.PedidoId, item.ClienteId, item.Nome, item.Email,
                    item.Telefone, item.ResponsavelCerimar, item.Turma, item.Endereco,
                    item.Complemento, item.Bairro, item.Municipio, item.UF, item.CEP))
                .ToListAsync(cancellationToken),
            ItensPedido = await _context.Set<ItemPedido>().AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupItemPedidoDto(
                    item.Id, item.PedidoId, item.ProdutoId, item.Quantidade, item.PrecoUnitario))
                .ToListAsync(cancellationToken),
            MovimentacoesEstoque = await _context.ProdutoMovimentacao.AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => new BackupMovimentacaoDto(
                    item.Id, item.ProdutoId, item.PedidoId, item.Data, item.Quantidade,
                    item.Tipo, item.Ator, item.Origem, item.Observacao))
                .ToListAsync(cancellationToken)
        };
    }

    private List<string> ObterImagens(int lojaId)
    {
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var pasta = Path.Combine(webRoot, "uploads", "produtos", lojaId.ToString());

        return Directory.Exists(pasta)
            ? Directory.EnumerateFiles(pasta, "*", SearchOption.TopDirectoryOnly).ToList()
            : [];
    }

    private static async Task GravarJsonAsync<T>(
        ZipArchive zip,
        string nome,
        T valor,
        CancellationToken cancellationToken)
    {
        var entrada = zip.CreateEntry(nome, CompressionLevel.Optimal);
        await using var stream = entrada.Open();
        await JsonSerializer.SerializeAsync(stream, valor, JsonOptions, cancellationToken);
    }
}
