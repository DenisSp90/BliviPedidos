using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaConfiguracaoEntregaFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssinaturaInicioEm",
                table: "Loja",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssinaturaTerminoEm",
                table: "Loja",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BairroOrigem",
                table: "Loja",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CepOrigem",
                table: "Loja",
                type: "varchar(9)",
                maxLength: 9,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ComplementoOrigem",
                table: "Loja",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EnderecoOrigem",
                table: "Loja",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EntregaAtiva",
                table: "Loja",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LatitudeOrigem",
                table: "Loja",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LongitudeOrigem",
                table: "Loja",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MunicipioOrigem",
                table: "Loja",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroOrigem",
                table: "Loja",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualConsumoEntrega",
                table: "Loja",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<bool>(
                name: "RetiradaAtiva",
                table: "Loja",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "UfOrigem",
                table: "Loja",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FaixaFreteLoja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LojaId = table.Column<int>(type: "int", nullable: false),
                    DistanciaInicialKm = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    DistanciaFinalKm = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    ValorFrete = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlteradaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaixaFreteLoja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaixaFreteLoja_Loja_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Loja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Loja",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AssinaturaInicioEm", "AssinaturaTerminoEm", "BairroOrigem", "CepOrigem", "ComplementoOrigem", "EnderecoOrigem", "EntregaAtiva", "LatitudeOrigem", "LongitudeOrigem", "MunicipioOrigem", "NumeroOrigem", "PercentualConsumoEntrega", "RetiradaAtiva", "UfOrigem" },
                values: new object[] { null, null, null, null, null, null, false, null, null, null, null, 2m, true, null });

            migrationBuilder.CreateIndex(
                name: "IX_FaixaFreteLoja_LojaId_Ordem",
                table: "FaixaFreteLoja",
                columns: new[] { "LojaId", "Ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaixaFreteLoja");

            migrationBuilder.DropColumn(
                name: "AssinaturaInicioEm",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "AssinaturaTerminoEm",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "BairroOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "CepOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "ComplementoOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "EnderecoOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "EntregaAtiva",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "LatitudeOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "LongitudeOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "MunicipioOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "NumeroOrigem",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "PercentualConsumoEntrega",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "RetiradaAtiva",
                table: "Loja");

            migrationBuilder.DropColumn(
                name: "UfOrigem",
                table: "Loja");
        }
    }
}
