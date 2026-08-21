using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821213000_AssociarUsuariosALoja")]
public class AssociarUsuariosALoja : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsuarioLoja",
            columns: table => new
            {
                UsuarioId = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LojaId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioLoja", x => x.UsuarioId);
                table.ForeignKey(
                    name: "FK_UsuarioLoja_AspNetUsers_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UsuarioLoja_Loja_LojaId",
                    column: x => x.LojaId,
                    principalTable: "Loja",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioLoja_LojaId",
            table: "UsuarioLoja",
            column: "LojaId");

        migrationBuilder.Sql(
            "INSERT INTO `UsuarioLoja` (`UsuarioId`, `LojaId`) " +
            "SELECT `Id`, 1 FROM `AspNetUsers`;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UsuarioLoja");
    }
}
