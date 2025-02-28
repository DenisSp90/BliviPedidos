using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations
{
    /// <inheritdoc />
    public partial class AjusteCamposCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cadastro_Cliente_ClienteId",
                table: "Cadastro");

            migrationBuilder.AlterColumn<int>(
                name: "ClienteId",
                table: "Cadastro",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Cadastro_Cliente_ClienteId",
                table: "Cadastro",
                column: "ClienteId",
                principalTable: "Cliente",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cadastro_Cliente_ClienteId",
                table: "Cadastro");

            migrationBuilder.AlterColumn<int>(
                name: "ClienteId",
                table: "Cadastro",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cadastro_Cliente_ClienteId",
                table: "Cadastro",
                column: "ClienteId",
                principalTable: "Cliente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
