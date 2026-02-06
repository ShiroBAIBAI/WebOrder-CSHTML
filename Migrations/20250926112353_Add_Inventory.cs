using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class Add_Inventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockItems",
                columns: table => new
                {
                    StockItemId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItems", x => x.StockItemId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTxns",
                columns: table => new
                {
                    InventoryTxnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockItemId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    QtyChange = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    TxnType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTxns", x => x.InventoryTxnId);
                    table.ForeignKey(
                        name: "FK_InventoryTxns_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "StockItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTxns_StockItemId",
                table: "InventoryTxns",
                column: "StockItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTxns");

            migrationBuilder.DropTable(
                name: "StockItems");
        }
    }
}
