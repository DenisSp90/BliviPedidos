using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.Backup;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class RestauracaoBackupLojaService : IRestauracaoBackupLojaService
{
    public const long TamanhoMaximoArquivo = 100 * 1024 * 1024;
    private const long TamanhoMaximoJson = 50 * 1024 * 1024;
    private const long TamanhoMaximoImagem = 5 * 1024 * 1024;
    private const long TamanhoMaximoDescompactado = 500 * 1024 * 1024;
    private const int QuantidadeMaximaEntradas = 10_000;
    private const string Formato = "BliviPedidos.BackupLoja";
    private const int Versao = 1;
    private const string PrefixoImagens = "imagens/produtos/";
    private static readonly SemaphoreSlim TravaRestauracao = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IBackupLojaService _backupLojaService;

    public RestauracaoBackupLojaService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IBackupLojaService backupLojaService)
    {
        _context = context;
        _environment = environment;
        _backupLojaService = backupLojaService;
    }

    public async Task<RestauracaoBackupResultado> RestaurarAsync(
        Stream arquivo,
        long tamanhoArquivo,
        CancellationToken cancellationToken = default)
    {
        if (tamanhoArquivo <= 0 || tamanhoArquivo > TamanhoMaximoArquivo)
            throw new InvalidDataException("O backup deve possuir no máximo 100 MB.");
        if (!arquivo.CanRead || !arquivo.CanSeek)
            throw new InvalidDataException("Não foi possível ler o arquivo de backup.");

        await TravaRestauracao.WaitAsync(cancellationToken);
        try
        {
            arquivo.Position = 0;
            using var zip = new ZipArchive(arquivo, ZipArchiveMode.Read, leaveOpen: true);
            var pacote = await ValidarELerAsync(zip, cancellationToken);
            var lojaId = _context.LojaIdAtual;
            if (pacote.Documento.Loja.Id != lojaId || pacote.Manifesto.LojaId != lojaId)
                throw new InvalidDataException("Este backup pertence a outra loja.");

            var backupSeguranca = await SalvarBackupSegurancaAsync(lojaId, cancellationToken);
            var pastas = await PrepararImagensAsync(zip, lojaId, pacote.Imagens, cancellationToken);

            try
            {
                await RestaurarBancoETrocarImagensAsync(
                    pacote.Documento,
                    pacote.Imagens,
                    pastas,
                    cancellationToken);
            }
            finally
            {
                if (Directory.Exists(pastas.Novas))
                    Directory.Delete(pastas.Novas, recursive: true);
            }

            return new RestauracaoBackupResultado(
                backupSeguranca,
                pacote.Documento.Categorias.Count,
                pacote.Documento.Produtos.Count,
                pacote.Documento.Clientes.Count,
                pacote.Documento.Pedidos.Count,
                pacote.Imagens.Count);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidDataException("O conteúdo do backup é inválido.", ex);
        }
        finally
        {
            TravaRestauracao.Release();
        }
    }

    private async Task<PacoteValidado> ValidarELerAsync(
        ZipArchive zip,
        CancellationToken cancellationToken)
    {
        if (zip.Entries.Count == 0 || zip.Entries.Count > QuantidadeMaximaEntradas)
            throw new InvalidDataException("O arquivo ZIP está vazio ou possui entradas demais.");
        if (zip.Entries.Sum(item => item.Length) > TamanhoMaximoDescompactado)
            throw new InvalidDataException("O conteúdo descompactado excede 500 MB.");

        var manifestoEntry = ObterEntradaUnica(zip, "manifesto.json");
        var dadosEntry = ObterEntradaUnica(zip, "dados.json");
        if (manifestoEntry.Length > TamanhoMaximoJson || dadosEntry.Length > TamanhoMaximoJson)
            throw new InvalidDataException("Os arquivos de dados do backup são grandes demais.");

        var manifesto = await LerJsonAsync<BackupLojaManifesto>(manifestoEntry, cancellationToken);
        var documento = await LerJsonAsync<BackupLojaDocumento>(dadosEntry, cancellationToken);
        if (manifesto.Quantidades is null || documento.Loja is null
            || documento.Categorias is null || documento.Produtos is null
            || documento.Clientes is null || documento.Pedidos is null
            || documento.Cadastros is null || documento.ItensPedido is null
            || documento.MovimentacoesEstoque is null)
            throw new InvalidDataException("O backup possui dados obrigatórios ausentes.");
        if (manifesto.Formato != Formato || documento.Formato != Formato
            || manifesto.Versao != Versao || documento.Versao != Versao)
            throw new InvalidDataException("O formato ou a versão do backup não é compatível.");
        if (manifesto.LojaId != documento.Loja.Id)
            throw new InvalidDataException("O manifesto não corresponde aos dados da loja.");

        ValidarQuantidadesManifesto(manifesto, documento);

        var imagens = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries.Where(item => item.FullName.StartsWith(PrefixoImagens, StringComparison.Ordinal)))
        {
            var nome = Path.GetFileName(entry.FullName);
            if (string.IsNullOrWhiteSpace(nome)
                || entry.FullName != PrefixoImagens + nome
                || entry.Length > TamanhoMaximoImagem
                || !ExtensaoImagemPermitida(nome)
                || !imagens.TryAdd(nome, entry))
                throw new InvalidDataException("O backup contém uma imagem inválida ou duplicada.");
        }

        ValidarRelacionamentos(documento);
        if (manifesto.QuantidadeImagens != imagens.Count)
            throw new InvalidDataException("A quantidade de imagens não corresponde ao manifesto.");

        return new PacoteValidado(manifesto, documento, imagens);
    }

    private static void ValidarRelacionamentos(BackupLojaDocumento documento)
    {
        ValidarIdsUnicos(documento.Categorias.Select(item => item.Id), "categorias");
        ValidarIdsUnicos(documento.Produtos.Select(item => item.Id), "produtos");
        ValidarIdsUnicos(documento.Clientes.Select(item => item.Id), "clientes");
        ValidarIdsUnicos(documento.Pedidos.Select(item => item.Id), "pedidos");
        ValidarIdsUnicos(documento.Cadastros.Select(item => item.Id), "cadastros");
        ValidarIdsUnicos(documento.ItensPedido.Select(item => item.Id), "itens de pedido");
        ValidarIdsUnicos(documento.MovimentacoesEstoque.Select(item => item.Id), "movimentações");

        var categorias = documento.Categorias.Select(item => item.Id).ToHashSet();
        var produtos = documento.Produtos.Select(item => item.Id).ToHashSet();
        var clientes = documento.Clientes.Select(item => item.Id).ToHashSet();
        var pedidos = documento.Pedidos.Select(item => item.Id).ToHashSet();

        if (documento.Produtos.Any(item => item.CategoriaId.HasValue && !categorias.Contains(item.CategoriaId.Value)))
            throw new InvalidDataException("Existe produto associado a uma categoria ausente.");
        if (documento.Cadastros.Count != documento.Pedidos.Count
            || documento.Cadastros.GroupBy(item => item.PedidoId).Any(grupo => grupo.Count() != 1)
            || documento.Cadastros.Any(item => !pedidos.Contains(item.PedidoId)
                || item.ClienteId.HasValue && !clientes.Contains(item.ClienteId.Value)))
            throw new InvalidDataException("Os cadastros dos pedidos estão incompletos ou inválidos.");
        if (documento.ItensPedido.Any(item => !pedidos.Contains(item.PedidoId) || !produtos.Contains(item.ProdutoId)))
            throw new InvalidDataException("Existe item associado a pedido ou produto ausente.");
        if (documento.MovimentacoesEstoque.Any(item =>
                item.ProdutoId.HasValue && !produtos.Contains(item.ProdutoId.Value)
                || item.PedidoId.HasValue && !pedidos.Contains(item.PedidoId.Value)))
            throw new InvalidDataException("Existe movimentação associada a pedido ou produto ausente.");
    }

    private async Task<string> SalvarBackupSegurancaAsync(int lojaId, CancellationToken cancellationToken)
    {
        var backup = await _backupLojaService.GerarAsync(cancellationToken);
        var pasta = Path.Combine(_environment.ContentRootPath, "App_Data", "backups", lojaId.ToString());
        Directory.CreateDirectory(pasta);
        var nomeArquivo = backup.NomeArquivo;
        var caminho = Path.Combine(pasta, nomeArquivo);
        if (File.Exists(caminho))
        {
            nomeArquivo = $"{Path.GetFileNameWithoutExtension(nomeArquivo)}-{Guid.NewGuid():N}.zip";
            caminho = Path.Combine(pasta, nomeArquivo);
        }
        await File.WriteAllBytesAsync(caminho, backup.Conteudo, cancellationToken);
        return nomeArquivo;
    }

    private async Task<PastasImagens> PrepararImagensAsync(
        ZipArchive zip,
        int lojaId,
        IReadOnlyDictionary<string, ZipArchiveEntry> imagens,
        CancellationToken cancellationToken)
    {
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var raiz = Path.Combine(webRoot, "uploads", "produtos");
        Directory.CreateDirectory(raiz);
        var identificador = Guid.NewGuid().ToString("N");
        var novas = Path.Combine(raiz, $".restauracao-{lojaId}-{identificador}");
        var anteriores = Path.Combine(raiz, $".anterior-{lojaId}-{identificador}");
        var destino = Path.Combine(raiz, lojaId.ToString());
        Directory.CreateDirectory(novas);

        try
        {
            foreach (var (nome, entry) in imagens)
            {
                await using var origem = entry.Open();
                await using var arquivo = new FileStream(
                    Path.Combine(novas, nome), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await origem.CopyToAsync(arquivo, cancellationToken);
            }
        }
        catch
        {
            if (Directory.Exists(novas))
                Directory.Delete(novas, recursive: true);
            throw;
        }

        return new PastasImagens(destino, novas, anteriores);
    }

    private async Task RestaurarBancoETrocarImagensAsync(
        BackupLojaDocumento documento,
        IReadOnlyDictionary<string, ZipArchiveEntry> imagens,
        PastasImagens pastas,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var imagensTrocadas = false;
        try
        {
            await ExcluirDadosAtuaisAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            await ImportarDadosAsync(documento, imagens.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), cancellationToken);

            if (Directory.Exists(pastas.Destino))
                Directory.Move(pastas.Destino, pastas.Anteriores);
            Directory.Move(pastas.Novas, pastas.Destino);
            imagensTrocadas = true;

            await transaction.CommitAsync(cancellationToken);
            if (Directory.Exists(pastas.Anteriores))
            {
                try
                {
                    Directory.Delete(pastas.Anteriores, recursive: true);
                }
                catch (IOException)
                {
                    // A restauração já foi confirmada. A pasta oculta pode ser limpa posteriormente.
                }
                catch (UnauthorizedAccessException)
                {
                    // A restauração já foi confirmada. A pasta oculta pode ser limpa posteriormente.
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (imagensTrocadas && Directory.Exists(pastas.Destino))
                Directory.Delete(pastas.Destino, recursive: true);
            if (Directory.Exists(pastas.Anteriores))
                Directory.Move(pastas.Anteriores, pastas.Destino);
            throw;
        }
    }

    private async Task ExcluirDadosAtuaisAsync(CancellationToken cancellationToken)
    {
        await _context.ProdutoMovimentacao.ExecuteDeleteAsync(cancellationToken);
        await _context.Set<ItemPedido>().ExecuteDeleteAsync(cancellationToken);
        await _context.Set<Cadastro>().ExecuteDeleteAsync(cancellationToken);
        await _context.Pedido.ExecuteDeleteAsync(cancellationToken);
        await _context.Produto.ExecuteDeleteAsync(cancellationToken);
        await _context.Categoria.ExecuteDeleteAsync(cancellationToken);
        await _context.Cliente.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ImportarDadosAsync(
        BackupLojaDocumento documento,
        HashSet<string> imagens,
        CancellationToken cancellationToken)
    {
        var loja = await _context.Loja.SingleAsync(item => item.Id == _context.LojaIdAtual, cancellationToken);
        loja.Nome = documento.Loja.Nome;
        loja.Slug = documento.Loja.Slug;
        loja.Dominio = documento.Loja.Dominio;
        loja.LogoUrl = documento.Loja.LogoUrl;
        loja.CorPrimaria = documento.Loja.CorPrimaria;
        loja.CorSecundaria = documento.Loja.CorSecundaria;
        loja.Descricao = documento.Loja.Descricao;
        loja.Whatsapp = documento.Loja.Whatsapp;
        loja.EmailContato = documento.Loja.EmailContato;
        loja.InstagramUrl = documento.Loja.InstagramUrl;
        loja.Ativa = documento.Loja.Ativa;

        var categorias = documento.Categorias.Select(item => new Categoria
        {
            Nome = item.Nome,
            IsAtivo = item.IsAtivo
        }).ToList();
        _context.Categoria.AddRange(categorias);
        await _context.SaveChangesAsync(cancellationToken);
        var mapaCategorias = documento.Categorias.Zip(categorias).ToDictionary(item => item.First.Id, item => item.Second);

        var produtos = documento.Produtos.Select(item => new Produto
        {
            Codigo = item.Codigo,
            Nome = item.Nome,
            PrecoVenda = item.PrecoVenda,
            PrecoPago = item.PrecoPago,
            Quantidade = item.Quantidade,
            Tamanho = item.Tamanho,
            CodeBar = item.CodeBar,
            Foto = NormalizarFotoRestaurada(item.Foto, documento.Loja.Id, imagens),
            IsAtivo = item.IsAtivo,
            Categoria = item.CategoriaId.HasValue ? mapaCategorias[item.CategoriaId.Value] : null
        }).ToList();
        _context.Produto.AddRange(produtos);
        await _context.SaveChangesAsync(cancellationToken);
        var mapaProdutos = documento.Produtos.Zip(produtos).ToDictionary(item => item.First.Id, item => item.Second);

        var clientes = documento.Clientes.Select(item => new Cliente
        {
            Nome = item.Nome,
            Email = item.Email,
            Telefone = item.Telefone,
            ResponsavelCerimar = item.ResponsavelCerimar,
            Turma = item.Turma,
            Endereco = item.Endereco,
            Complemento = item.Complemento,
            Bairro = item.Bairro,
            Municipio = item.Municipio,
            UF = item.UF,
            CEP = item.CEP
        }).ToList();
        _context.Cliente.AddRange(clientes);
        await _context.SaveChangesAsync(cancellationToken);
        var mapaClientes = documento.Clientes.Zip(clientes).ToDictionary(item => item.First.Id, item => item.Second);

        var idsUsuarios = documento.Pedidos
            .Select(item => item.ConsumidorUsuarioId)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .Distinct()
            .ToList();
        var usuariosExistentes = (await _context.Users
                .Where(item => idsUsuarios.Contains(item.Id))
                .Select(item => item.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var cadastros = documento.Cadastros.ToDictionary(item => item.PedidoId);
        var pedidos = documento.Pedidos.Select(item =>
        {
            var cadastro = cadastros[item.Id];
            return new Pedido
            {
                CodigoPublico = item.CodigoPublico,
                ConsumidorUsuarioId = item.ConsumidorUsuarioId is not null
                    && usuariosExistentes.Contains(item.ConsumidorUsuarioId)
                        ? item.ConsumidorUsuarioId
                        : null,
                Status = item.Status,
                StatusPagamento = item.StatusPagamento,
                DataPedido = item.DataPedido,
                ReservaExpiraEm = item.ReservaExpiraEm,
                DataPagamento = item.DataPagamento,
                ValorTotalPedido = item.ValorTotalPedido,
                EmailResponsavel = item.EmailResponsavel,
                Cadastro = new Cadastro
                {
                    Cliente = cadastro.ClienteId.HasValue ? mapaClientes[cadastro.ClienteId.Value] : null,
                    Nome = cadastro.Nome,
                    Email = cadastro.Email,
                    Telefone = cadastro.Telefone,
                    ResponsavelCerimar = cadastro.ResponsavelCerimar,
                    Turma = cadastro.Turma,
                    Endereco = cadastro.Endereco,
                    Complemento = cadastro.Complemento,
                    Bairro = cadastro.Bairro,
                    Municipio = cadastro.Municipio,
                    UF = cadastro.UF,
                    CEP = cadastro.CEP
                }
            };
        }).ToList();
        _context.Pedido.AddRange(pedidos);
        await _context.SaveChangesAsync(cancellationToken);
        var mapaPedidos = documento.Pedidos.Zip(pedidos).ToDictionary(item => item.First.Id, item => item.Second);

        var itens = documento.ItensPedido.Select(item => new ItemPedido(
            mapaPedidos[item.PedidoId],
            mapaProdutos[item.ProdutoId],
            item.Quantidade,
            item.PrecoUnitario)).ToList();
        _context.Set<ItemPedido>().AddRange(itens);

        var movimentacoes = documento.MovimentacoesEstoque.Select(item => new ProdutoMovimentacao
        {
            Produto = item.ProdutoId.HasValue ? mapaProdutos[item.ProdutoId.Value] : null,
            Pedido = item.PedidoId.HasValue ? mapaPedidos[item.PedidoId.Value] : null,
            Data = item.Data,
            Quantidade = item.Quantidade,
            Tipo = item.Tipo,
            Ator = item.Ator,
            Origem = item.Origem,
            Observacao = item.Observacao
        }).ToList();
        _context.ProdutoMovimentacao.AddRange(movimentacoes);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizarFotoRestaurada(string? foto, int lojaId, HashSet<string> imagens)
    {
        var prefixo = $"/uploads/produtos/{lojaId}/";
        if (string.IsNullOrWhiteSpace(foto) || !foto.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            return foto;

        var nome = Path.GetFileName(foto);
        return imagens.Contains(nome) ? $"{prefixo}{nome}" : null;
    }

    private static ZipArchiveEntry ObterEntradaUnica(ZipArchive zip, string nome)
    {
        var entradas = zip.Entries.Where(item => item.FullName == nome).ToList();
        return entradas.Count == 1
            ? entradas[0]
            : throw new InvalidDataException($"O backup deve conter exatamente um arquivo {nome}.");
    }

    private static async Task<T> LerJsonAsync<T>(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("O backup possui um arquivo JSON vazio.");
    }

    private static void ValidarIdsUnicos(IEnumerable<int> ids, string nome)
    {
        var lista = ids.ToList();
        if (lista.Any(id => id <= 0) || lista.Distinct().Count() != lista.Count)
            throw new InvalidDataException($"O backup contém IDs inválidos ou duplicados em {nome}.");
    }

    private static void ValidarQuantidadesManifesto(
        BackupLojaManifesto manifesto,
        BackupLojaDocumento documento)
    {
        var esperadas = new Dictionary<string, int>
        {
            ["categorias"] = documento.Categorias.Count,
            ["produtos"] = documento.Produtos.Count,
            ["clientes"] = documento.Clientes.Count,
            ["pedidos"] = documento.Pedidos.Count,
            ["cadastros"] = documento.Cadastros.Count,
            ["itensPedido"] = documento.ItensPedido.Count,
            ["movimentacoesEstoque"] = documento.MovimentacoesEstoque.Count
        };

        if (esperadas.Any(item => !manifesto.Quantidades.TryGetValue(item.Key, out var quantidade)
                || quantidade != item.Value))
            throw new InvalidDataException("As quantidades do manifesto não correspondem aos dados.");
    }

    private static bool ExtensaoImagemPermitida(string nome)
    {
        return Path.GetExtension(nome).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp";
    }

    private sealed record PacoteValidado(
        BackupLojaManifesto Manifesto,
        BackupLojaDocumento Documento,
        IReadOnlyDictionary<string, ZipArchiveEntry> Imagens);

    private sealed record PastasImagens(string Destino, string Novas, string Anteriores);
}
