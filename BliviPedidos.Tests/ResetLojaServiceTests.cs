using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class ResetLojaServiceTests
{
    [Fact]
    public async Task ResetarAsync_DeveApagarSomenteDadosComerciaisDaLojaSelecionada()
    {
        var pastaTemporaria = Path.Combine(Path.GetTempPath(), $"blivi-reset-{Guid.NewGuid():N}");
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

            context.Produto.Add(new Produto { Nome = "Produto loja padrão", PrecoVenda = 5m });
            await context.SaveChangesAsync();

            var lojaAlvo = new Loja
            {
                Nome = "Loja para reset",
                Slug = "loja-reset",
                PixAtivo = true,
                PixChave = "pix-preservado",
                Ativa = true
            };
            context.Loja.Add(lojaAlvo);
            await context.SaveChangesAsync();
            context.DefinirLojaAtual(lojaAlvo.Id);

            var usuario = new IdentityUser
            {
                UserName = "vendedor@loja.test",
                NormalizedUserName = "VENDEDOR@LOJA.TEST",
                Email = "vendedor@loja.test",
                NormalizedEmail = "VENDEDOR@LOJA.TEST"
            };
            context.Users.Add(usuario);
            await context.SaveChangesAsync();
            context.UsuarioLoja.Add(new UsuarioLoja { UsuarioId = usuario.Id, LojaId = lojaAlvo.Id });

            var categoria = new Categoria { Nome = "Categoria alvo", IsAtivo = true };
            var produto = new Produto
            {
                Nome = "Produto alvo",
                PrecoVenda = 15m,
                Quantidade = 4,
                Categoria = categoria,
                Foto = $"/uploads/produtos/{lojaAlvo.Id}/produto.png"
            };
            var cliente = new Cliente { Nome = "Cliente alvo", Telefone = "55 11 99999-9999" };
            context.AddRange(categoria, produto, cliente);
            await context.SaveChangesAsync();

            var pedido = new Pedido(new Cadastro
            {
                Nome = "Cliente alvo",
                Telefone = "55 11 99999-9999",
                Cliente = cliente
            })
            {
                CodigoPublico = "RESET-001",
                ValorTotalPedido = 15m
            };
            context.Pedido.Add(pedido);
            await context.SaveChangesAsync();
            context.Set<ItemPedido>().Add(new ItemPedido(pedido, produto, 1, 15m));
            context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
            {
                Produto = produto,
                Pedido = pedido,
                Quantidade = 1,
                Tipo = "Saida",
                Ator = "TESTE",
                Origem = OrigemMovimentacaoEstoque.PedidoInterno
            });
            await context.SaveChangesAsync();

            var pastaImagem = Path.Combine(webRoot, "uploads", "produtos", lojaAlvo.Id.ToString());
            Directory.CreateDirectory(pastaImagem);
            await File.WriteAllBytesAsync(Path.Combine(pastaImagem, "produto.png"), [1, 2, 3]);

            context.DefinirLojaAtual(Loja.PadraoId);
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.WebRootPath).Returns(webRoot);
            environment.SetupGet(item => item.ContentRootPath).Returns(pastaTemporaria);
            var backupService = new BackupLojaService(context, environment.Object);
            var resetService = new ResetLojaService(context, environment.Object, backupService);

            var resultado = await resetService.ResetarAsync(lojaAlvo.Id);

            Assert.Equal(Loja.PadraoId, context.LojaIdAtual);
            Assert.Equal("Produto loja padrão", (await context.Produto.SingleAsync()).Nome);

            context.DefinirLojaAtual(lojaAlvo.Id);
            Assert.Empty(await context.Categoria.ToListAsync());
            Assert.Empty(await context.Produto.ToListAsync());
            Assert.Empty(await context.Cliente.ToListAsync());
            Assert.Empty(await context.Pedido.ToListAsync());
            Assert.Empty(await context.Set<ItemPedido>().ToListAsync());
            Assert.Empty(await context.Set<Cadastro>().ToListAsync());
            Assert.Empty(await context.ProdutoMovimentacao.ToListAsync());
            Assert.False(Directory.Exists(pastaImagem));

            var lojaPreservada = await context.Loja.SingleAsync(item => item.Id == lojaAlvo.Id);
            Assert.True(lojaPreservada.PixAtivo);
            Assert.Equal("pix-preservado", lojaPreservada.PixChave);
            Assert.True(await context.Users.AnyAsync(item => item.Id == usuario.Id));
            Assert.True(await context.UsuarioLoja.AnyAsync(item => item.UsuarioId == usuario.Id));
            Assert.True(File.Exists(Path.Combine(
                pastaTemporaria, "App_Data", "backups", lojaAlvo.Id.ToString(), resultado.BackupSeguranca)));
        }
        finally
        {
            if (Directory.Exists(pastaTemporaria))
                Directory.Delete(pastaTemporaria, recursive: true);
        }
    }
}
