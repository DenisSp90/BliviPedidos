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
}
