using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class InitClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_SortOrder",
                table: "MenuItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories");

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "CategoryId",
                keyColumnType: "nvarchar(20)",
                keyValue: "DESSERT");

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "CategoryId",
                keyColumnType: "nvarchar(20)",
                keyValue: "DRINK");

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "CategoryId",
                keyColumnType: "nvarchar(20)",
                keyValue: "MAIN");

            migrationBuilder.DeleteData(
                table: "MenuCategories",
                keyColumn: "CategoryId",
                keyColumnType: "nvarchar(20)",
                keyValue: "SNACK");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "MenuCategories");

            migrationBuilder.AlterColumn<string>(
                name: "VoucherCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,2)",
                oldPrecision: 8,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentChannel",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MenuCategoryId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MenuCategoryId",
                table: "MenuCategories",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId_IsAvailable",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId_SortOrder",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId",
                principalTable: "MenuCategories",
                principalColumn: "MenuCategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId_IsAvailable",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId_SortOrder",
                table: "MenuItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "MenuCategoryId",
                table: "MenuCategories");

            migrationBuilder.AlterColumn<string>(
                name: "VoucherCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentChannel",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "MenuItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "MenuCategories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories",
                column: "CategoryId");

            migrationBuilder.InsertData(
                table: "MenuCategories",
                columns: new[] { "CategoryId", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { "DESSERT", true, "Dessert", 4 },
                    { "DRINK", true, "Drinks", 2 },
                    { "MAIN", true, "Main Dish", 1 },
                    { "SNACK", true, "Snacks", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable",
                table: "MenuItems",
                columns: new[] { "CategoryId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_SortOrder",
                table: "MenuItems",
                columns: new[] { "CategoryId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems",
                column: "CategoryId",
                principalTable: "MenuCategories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
