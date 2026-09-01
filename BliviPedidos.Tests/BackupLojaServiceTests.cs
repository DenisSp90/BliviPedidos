using System.IO.Compression;
using System.Text.Json;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.Backup;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class BackupLojaServiceTests
{
    [Fact]
    public async Task GerarAsync_DeveExportarSomenteDadosEImagensDaLojaAtual()
    {
        var pastaTemporaria = Path.Combine(Path.GetTempPath(), $"blivi-backup-{Guid.NewGuid():N}");
        var webRoot = Path.Combine(pastaTemporaria, "wwwroot");
        Directory.CreateDirectory(webRoot);

        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var outraLoja = new Loja
            {
                Nome = "Loja do Backup",
                Slug = "loja-backup",
                PixChave = "chave-pix-nao-exportar",
                Ativa = true
            };
            context.Loja.Add(outraLoja);
            await context.SaveChangesAsync();

            context.Produto.Add(new Produto { Nome = "Produto de outra loja", PrecoVenda = 10m });
            await context.SaveChangesAsync();

            context.DefinirLojaAtual(outraLoja.Id);
            var categoria = new Categoria { Nome = "Categoria exportada", IsAtivo = true };
            context.Categoria.Add(categoria);
            await context.SaveChangesAsync();
            context.Produto.Add(new Produto
            {
                Nome = "Produto exportado",
                PrecoPago = 5m,
                PrecoVenda = 12m,
                Quantidade = 3,
                CategoriaId = categoria.Id,
                Foto = $"/uploads/produtos/{outraLoja.Id}/produto.png",
                IsAtivo = true
            });
            await context.SaveChangesAsync();

            var pastaImagemLoja = Path.Combine(webRoot, "uploads", "produtos", outraLoja.Id.ToString());
            Directory.CreateDirectory(pastaImagemLoja);
            await File.WriteAllBytesAsync(Path.Combine(pastaImagemLoja, "produto.png"), [1, 2, 3]);

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.WebRootPath).Returns(webRoot);
            environment.SetupGet(item => item.ContentRootPath).Returns(pastaTemporaria);
            var service = new BackupLojaService(context, environment.Object);

            var resultado = await service.GerarAsync();

            Assert.StartsWith("backup-loja-backup-", resultado.NomeArquivo);
            using var memoria = new MemoryStream(resultado.Conteudo);
            using var zip = new ZipArchive(memoria, ZipArchiveMode.Read);
            Assert.NotNull(zip.GetEntry("manifesto.json"));
            Assert.NotNull(zip.GetEntry("imagens/produtos/produto.png"));

            await using var dadosStream = zip.GetEntry("dados.json")!.Open();
            using var leitor = new StreamReader(dadosStream);
            var dadosJson = await leitor.ReadToEndAsync();
            var documento = JsonSerializer.Deserialize<BackupLojaDocumento>(
                dadosJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(documento);
            Assert.Equal(outraLoja.Id, documento.Loja.Id);
            Assert.Single(documento.Categorias);
            Assert.Single(documento.Produtos);
            Assert.Equal("Produto exportado", documento.Produtos[0].Nome);
            Assert.DoesNotContain("chave-pix-nao-exportar", dadosJson);
        }
        finally
        {
            if (Directory.Exists(pastaTemporaria))
                Directory.Delete(pastaTemporaria, recursive: true);
        }
    }

    [Fact]
    public async Task RestaurarAsync_DeveSubstituirDadosEImagensEPreservarBackupSeguranca()
    {
        var pastaTemporaria = Path.Combine(Path.GetTempPath(), $"blivi-restauracao-{Guid.NewGuid():N}");
        var webRoot = Path.Combine(pastaTemporaria, "wwwroot");
        Directory.CreateDirectory(webRoot);

        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var categoria = new Categoria { Nome = "Categoria original", IsAtivo = true };
            var produto = new Produto
            {
                Nome = "Produto original",
                PrecoPago = 4m,
                PrecoVenda = 10m,
                Quantidade = 8,
                Foto = "/uploads/produtos/1/original.png",
                Categoria = categoria,
                IsAtivo = true
            };
            var cliente = new Cliente { Nome = "Cliente original", Telefone = "55 11 99999-9999" };
            context.AddRange(categoria, produto, cliente);
            await context.SaveChangesAsync();

            var pedido = new Pedido(new Cadastro
            {
                Nome = "Cliente do pedido",
                Telefone = "55 11 99999-9999",
                Cliente = cliente
            })
            {
                CodigoPublico = "BACKUP-001",
                Status = StatusPedido.Concluido,
                StatusPagamento = StatusPagamento.Pago,
                DataPedido = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc),
                ValorTotalPedido = 20m
            };
            context.Pedido.Add(pedido);
            await context.SaveChangesAsync();
            context.Set<ItemPedido>().Add(new ItemPedido(pedido, produto, 2, 10m));
            context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
            {
                Produto = produto,
                Pedido = pedido,
                Quantidade = 2,
                Tipo = "Saida",
                Ator = "TESTE",
                Origem = OrigemMovimentacaoEstoque.PedidoInterno,
                Data = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();

            var pastaImagem = Path.Combine(webRoot, "uploads", "produtos", "1");
            Directory.CreateDirectory(pastaImagem);
            await File.WriteAllBytesAsync(Path.Combine(pastaImagem, "original.png"), [1, 2, 3, 4]);

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.WebRootPath).Returns(webRoot);
            environment.SetupGet(item => item.ContentRootPath).Returns(pastaTemporaria);
            var backupService = new BackupLojaService(context, environment.Object);
            var backupOriginal = await backupService.GerarAsync();

            produto.Nome = "Produto alterado após backup";
            cliente.Nome = "Cliente alterado após backup";
            await context.SaveChangesAsync();
            await File.WriteAllBytesAsync(Path.Combine(pastaImagem, "original.png"), [9, 9]);
            await File.WriteAllBytesAsync(Path.Combine(pastaImagem, "arquivo-novo.png"), [8]);

            var restauracao = new RestauracaoBackupLojaService(context, environment.Object, backupService);
            await using var stream = new MemoryStream(backupOriginal.Conteudo);
            var resultado = await restauracao.RestaurarAsync(stream, stream.Length);

            context.ChangeTracker.Clear();
            var produtoRestaurado = await context.Produto.Include(item => item.Categoria).SingleAsync();
            var clienteRestaurado = await context.Cliente.SingleAsync();
            var pedidoRestaurado = await context.Pedido.Include(item => item.Cadastro).SingleAsync();
            var itemRestaurado = await context.Set<ItemPedido>().SingleAsync();
            var movimentacaoRestaurada = await context.ProdutoMovimentacao.SingleAsync();

            Assert.Equal("Produto original", produtoRestaurado.Nome);
            Assert.Equal("Categoria original", produtoRestaurado.Categoria!.Nome);
            Assert.Equal("Cliente original", clienteRestaurado.Nome);
            Assert.Equal("BACKUP-001", pedidoRestaurado.CodigoPublico);
            Assert.Equal(clienteRestaurado.Id, pedidoRestaurado.Cadastro.ClienteId);
            Assert.Equal(produtoRestaurado.Id, itemRestaurado.ProdutoId);
            Assert.Equal(pedidoRestaurado.Id, itemRestaurado.PedidoId);
            Assert.Equal(produtoRestaurado.Id, movimentacaoRestaurada.ProdutoId);
            Assert.Equal([1, 2, 3, 4], await File.ReadAllBytesAsync(Path.Combine(pastaImagem, "original.png")));
            Assert.False(File.Exists(Path.Combine(pastaImagem, "arquivo-novo.png")));
            Assert.True(File.Exists(Path.Combine(
                pastaTemporaria, "App_Data", "backups", "1", resultado.BackupSeguranca)));
        }
        finally
        {
            if (Directory.Exists(pastaTemporaria))
                Directory.Delete(pastaTemporaria, recursive: true);
        }
    }

    [Fact]
    public async Task RestaurarAsync_DeveRecusarBackupDeOutraLojaSemAlterarDados()
    {
        var pastaTemporaria = Path.Combine(Path.GetTempPath(), $"blivi-restauracao-loja-{Guid.NewGuid():N}");
        var webRoot = Path.Combine(pastaTemporaria, "wwwroot");
        Directory.CreateDirectory(webRoot);

        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Produto.Add(new Produto { Nome = "Produto loja padrão", PrecoVenda = 10m });
            await context.SaveChangesAsync();

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.WebRootPath).Returns(webRoot);
            environment.SetupGet(item => item.ContentRootPath).Returns(pastaTemporaria);
            var backupService = new BackupLojaService(context, environment.Object);
            var backupOutraLoja = await backupService.GerarAsync();

            var lojaAtual = new Loja { Nome = "Loja atual", Slug = "loja-atual", Ativa = true };
            context.Loja.Add(lojaAtual);
            await context.SaveChangesAsync();
            context.DefinirLojaAtual(lojaAtual.Id);
            context.Produto.Add(new Produto { Nome = "Produto que deve permanecer", PrecoVenda = 20m });
            await context.SaveChangesAsync();

            var restauracao = new RestauracaoBackupLojaService(context, environment.Object, backupService);
            await using var stream = new MemoryStream(backupOutraLoja.Conteudo);

            var erro = await Assert.ThrowsAsync<InvalidDataException>(
                () => restauracao.RestaurarAsync(stream, stream.Length));

            Assert.Equal("Este backup pertence a outra loja.", erro.Message);
            Assert.Equal("Produto que deve permanecer", (await context.Produto.SingleAsync()).Nome);
            Assert.False(Directory.Exists(Path.Combine(
                pastaTemporaria, "App_Data", "backups", lojaAtual.Id.ToString())));
        }
        finally
        {
            if (Directory.Exists(pastaTemporaria))
                Directory.Delete(pastaTemporaria, recursive: true);
        }
    }
}
