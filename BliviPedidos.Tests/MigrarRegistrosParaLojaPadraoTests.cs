using BliviPedidos.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace BliviPedidos.Tests;

public class MigrarRegistrosParaLojaPadraoTests
{
    [Fact]
    public void Up_DeveAssociarSomenteRegistrosSemLojaNasQuatroTabelas()
    {
        var migration = new MigracaoTestavel();

        var operations = migration.ObterOperacoesUp()
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToList();

        Assert.Collection(
            operations,
            sql => Assert.Equal("UPDATE `Categoria` SET `LojaId` = 1 WHERE `LojaId` IS NULL;", sql),
            sql => Assert.Equal("UPDATE `Cliente` SET `LojaId` = 1 WHERE `LojaId` IS NULL;", sql),
            sql => Assert.Equal("UPDATE `Produto` SET `LojaId` = 1 WHERE `LojaId` IS NULL;", sql),
            sql => Assert.Equal("UPDATE `Pedido` SET `LojaId` = 1 WHERE `LojaId` IS NULL;", sql));
    }

    private sealed class MigracaoTestavel : MigrarRegistrosParaLojaPadrao
    {
        public IReadOnlyList<MigrationOperation> ObterOperacoesUp()
        {
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            base.Up(builder);
            return builder.Operations;
        }
    }
}
