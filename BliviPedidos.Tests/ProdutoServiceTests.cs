using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class ProdutoServiceTests
{
    [Fact]
    public async Task RegistrarProdutoAsync_AoEditar_DeveAtualizarAmbosOsPrecos()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produto = new Produto
        {
            Nome = "Produto",
            PrecoPago = 10m,
            PrecoVenda = 15m,
            Quantidade = 5,
            IsAtivo = true,
            ProdutoMovimentacao = []
        };
        context.Produto.Add(produto);
        await context.SaveChangesAsync();

        var service = new ProdutoService(
            context,
            Mock.Of<IMapper>(),
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<ProdutoService>.Instance);

        var atualizado = new Produto(
            produto.Id,
            produto.Codigo ?? string.Empty,
            produto.Nome,
            19.90m,
            12.50m,
            produto.Quantidade,
            produto.Tamanho,
            produto.CodeBar,
            produto.CategoriaId)
        {
            IsAtivo = produto.IsAtivo
        };

        var resultado = await service.RegistrarProdutoAsync(atualizado);

        Assert.True(resultado);
        var salvo = await context.Produto.SingleAsync(item => item.Id == produto.Id);
        Assert.Equal(12.50m, salvo.PrecoPago);
        Assert.Equal(19.90m, salvo.PrecoVenda);
    }
}
