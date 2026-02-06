using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class AlterCategoryIdLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the foreign key first
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems");

            // 2. Alter the column in MenuCategories
            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "MenuCategories",
                type: "nvarchar(20)",  // new size
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)");

            // 3. Alter the column in MenuItems (FK side too!)
            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "MenuItems",
                type: "nvarchar(20)",   // must match parent
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)");

            // 4. Recreate the foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems",
                column: "CategoryId",
                principalTable: "MenuCategories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "MenuItems",
                type: "nvarchar(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "MenuCategories",
                type: "nvarchar(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)");

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