using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821212000_TornarLojaIdObrigatorio")]
public class TornarLojaIdObrigatorio : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        TornarObrigatorio(migrationBuilder, "Categoria");
        TornarObrigatorio(migrationBuilder, "Cliente");
        TornarObrigatorio(migrationBuilder, "Produto");
        TornarObrigatorio(migrationBuilder, "Pedido");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        TornarOpcional(migrationBuilder, "Categoria");
        TornarOpcional(migrationBuilder, "Cliente");
        TornarOpcional(migrationBuilder, "Produto");
        TornarOpcional(migrationBuilder, "Pedido");
    }

    private static void TornarObrigatorio(MigrationBuilder migrationBuilder, string tabela)
    {
        migrationBuilder.AlterColumn<int>(
            name: "LojaId",
            table: tabela,
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }

    private static void TornarOpcional(MigrationBuilder migrationBuilder, string tabela)
    {
        migrationBuilder.AlterColumn<int>(
            name: "LojaId",
            table: tabela,
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");
    }
}
