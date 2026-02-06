using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class Alter_Inventory_IntQty_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "StockItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "ReorderLevel",
                table: "StockItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "StockItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "QtyChange",
                table: "InventoryTxns",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_Name",
                table: "StockItems",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItems_Name",
                table: "StockItems");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "StockItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReorderLevel",
                table: "StockItems",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "StockItems",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "QtyChange",
                table: "InventoryTxns",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
