using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class DeleteUnusedCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId_IsAvailable",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId_SortOrder",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemImages_MenuItemId_SortOrder",
                table: "MenuItemImages");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemImages_MenuItemId",
                table: "MenuItemImages",
                column: "MenuItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemImages_MenuItemId",
                table: "MenuItemImages");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId_IsAvailable",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId_SortOrder",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemImages_MenuItemId_SortOrder",
                table: "MenuItemImages",
                columns: new[] { "MenuItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories",
                column: "SortOrder",
                unique: true);
        }
    }
}
