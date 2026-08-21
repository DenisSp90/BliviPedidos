using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821210000_CriarLojaPadrao")]
public class CriarLojaPadrao : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Loja",
            columns: new[] { "Id", "Ativa", "CorPrimaria", "Dominio", "LogoUrl", "Nome", "Slug" },
            values: new object[] { 1, true, "#0d6efd", null, null, "Blivi Pedidos", "blivi-pedidos" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Loja",
            keyColumn: "Id",
            keyValue: 1);
    }
}
