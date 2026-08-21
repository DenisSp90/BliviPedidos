using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821150000_AdicionarLoja")]
public class AdicionarLoja : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Loja",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Slug = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Dominio = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LogoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CorPrimaria = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Loja", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Loja_Dominio",
            table: "Loja",
            column: "Dominio",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Loja_Slug",
            table: "Loja",
            column: "Slug",
            unique: true);

        AddLojaColumn(migrationBuilder, "Categoria");
        AddLojaColumn(migrationBuilder, "Cliente");
        AddLojaColumn(migrationBuilder, "Pedido");
        AddLojaColumn(migrationBuilder, "Produto");

        // A tabela acabou de ser criada, portanto o identificador 1 e seguro.
        // O backfill preserva todos os registros anteriores em uma loja inicial.
        migrationBuilder.InsertData(
            table: "Loja",
            columns: new[] { "Id", "Nome", "Slug", "Ativa" },
            values: new object[] { 1, "Loja Principal", "principal", true });

        migrationBuilder.Sql("UPDATE `Categoria` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Cliente` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Pedido` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");
        migrationBuilder.Sql("UPDATE `Produto` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");

        AddLojaForeignKey(migrationBuilder, "Categoria");
        AddLojaForeignKey(migrationBuilder, "Cliente");
        AddLojaForeignKey(migrationBuilder, "Pedido");
        AddLojaForeignKey(migrationBuilder, "Produto");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropLojaReference(migrationBuilder, "Categoria");
        DropLojaReference(migrationBuilder, "Cliente");
        DropLojaReference(migrationBuilder, "Pedido");
        DropLojaReference(migrationBuilder, "Produto");

        migrationBuilder.DropTable(name: "Loja");
    }

    private static void AddLojaColumn(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<int>(
            name: "LojaId",
            table: table,
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: $"IX_{table}_LojaId",
            table: table,
            column: "LojaId");
    }

    private static void AddLojaForeignKey(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddForeignKey(
            name: $"FK_{table}_Loja_LojaId",
            table: table,
            column: "LojaId",
            principalTable: "Loja",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    private static void DropLojaReference(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.DropForeignKey(
            name: $"FK_{table}_Loja_LojaId",
            table: table);

        migrationBuilder.DropIndex(
            name: $"IX_{table}_LojaId",
            table: table);

        migrationBuilder.DropColumn(
            name: "LojaId",
            table: table);
    }
}
