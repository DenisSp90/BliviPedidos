using BliviPedidos.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace BliviPedidos.Tests;

public class AssociarUsuariosALojaTests
{
    [Fact]
    public void Up_DeveCriarVinculoEMigrarUsuariosExistentes()
    {
        var migration = new MigracaoTestavel();

        var operations = migration.ObterOperacoesUp();

        var createTable = Assert.Single(operations.OfType<CreateTableOperation>());
        Assert.Equal("UsuarioLoja", createTable.Name);
        Assert.Contains(createTable.Columns, column => column.Name == "UsuarioId" && !column.IsNullable);
        Assert.Contains(createTable.Columns, column => column.Name == "LojaId" && !column.IsNullable);

        var sql = Assert.Single(operations.OfType<SqlOperation>()).Sql;
        Assert.Contains("SELECT `Id`, 1 FROM `AspNetUsers`", sql);
    }

    private sealed class MigracaoTestavel : AssociarUsuariosALoja
    {
        public IReadOnlyList<MigrationOperation> ObterOperacoesUp()
        {
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            base.Up(builder);
            return builder.Operations;
        }
    }
}
