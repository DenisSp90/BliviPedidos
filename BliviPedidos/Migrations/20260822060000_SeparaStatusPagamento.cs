using BliviPedidos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BliviPedidos.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260822060000_SeparaStatusPagamento")]
public sealed class SeparaStatusPagamento : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("StatusPagamento", "Pedido", "int", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE `Pedido` SET `StatusPagamento` = CASE WHEN `Pago` = 1 THEN 1 ELSE 0 END;");
        migrationBuilder.DropColumn("Pago", "Pedido");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("Pago", "Pedido", "tinyint(1)", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE `Pedido` SET `Pago` = CASE WHEN `StatusPagamento` = 1 THEN 1 ELSE 0 END;");
        migrationBuilder.DropColumn("StatusPagamento", "Pedido");
    }
}
