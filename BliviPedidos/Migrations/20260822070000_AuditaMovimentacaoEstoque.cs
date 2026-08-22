using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260822070000_AuditaMovimentacaoEstoque")]
public sealed class AuditaMovimentacaoEstoque : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Ator", "ProdutoMovimentacao", "varchar(256)", maxLength: 256, nullable: false, defaultValue: "SISTEMA");
        migrationBuilder.AddColumn<int>("LojaId", "ProdutoMovimentacao", "int", nullable: true);
        migrationBuilder.AddColumn<string>("Origem", "ProdutoMovimentacao", "varchar(64)", maxLength: 64, nullable: false, defaultValue: "Legado");
        migrationBuilder.AddColumn<int>("PedidoId", "ProdutoMovimentacao", "int", nullable: true);

        migrationBuilder.Sql(
            "UPDATE `ProdutoMovimentacao` AS `m` INNER JOIN `Produto` AS `p` ON `p`.`Id` = `m`.`ProdutoId` SET `m`.`LojaId` = `p`.`LojaId`;");
        migrationBuilder.Sql(
            "UPDATE `ProdutoMovimentacao` SET `LojaId` = 1 WHERE `LojaId` IS NULL;");

        migrationBuilder.AlterColumn<int>(
            "LojaId",
            "ProdutoMovimentacao",
            "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.CreateIndex("IX_ProdutoMovimentacao_LojaId", "ProdutoMovimentacao", "LojaId");
        migrationBuilder.CreateIndex("IX_ProdutoMovimentacao_PedidoId", "ProdutoMovimentacao", "PedidoId");
        migrationBuilder.AddForeignKey(
            "FK_ProdutoMovimentacao_Loja_LojaId",
            "ProdutoMovimentacao",
            "LojaId",
            "Loja",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            "FK_ProdutoMovimentacao_Pedido_PedidoId",
            "ProdutoMovimentacao",
            "PedidoId",
            "Pedido",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_ProdutoMovimentacao_Loja_LojaId", "ProdutoMovimentacao");
        migrationBuilder.DropForeignKey("FK_ProdutoMovimentacao_Pedido_PedidoId", "ProdutoMovimentacao");
        migrationBuilder.DropIndex("IX_ProdutoMovimentacao_LojaId", "ProdutoMovimentacao");
        migrationBuilder.DropIndex("IX_ProdutoMovimentacao_PedidoId", "ProdutoMovimentacao");
        migrationBuilder.DropColumn("Ator", "ProdutoMovimentacao");
        migrationBuilder.DropColumn("LojaId", "ProdutoMovimentacao");
        migrationBuilder.DropColumn("Origem", "ProdutoMovimentacao");
        migrationBuilder.DropColumn("PedidoId", "ProdutoMovimentacao");
    }
}
