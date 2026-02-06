using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrderLines");

            migrationBuilder.RenameColumn(
                name: "Qty",
                table: "OrderLines",
                newName: "Quantity");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableNo",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "OrderLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableNo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "OrderLines");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "OrderLines",
                newName: "Qty");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrderLines",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
