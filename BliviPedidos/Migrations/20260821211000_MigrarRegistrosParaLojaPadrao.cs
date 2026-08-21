using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821211000_MigrarRegistrosParaLojaPadrao")]
public class MigrarRegistrosParaLojaPadrao : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE `Categoria` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Cliente` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Produto` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Pedido` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A reversao nao remove associacoes para evitar perda da informacao de loja.
    }
}
