using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class Add_Unique_SortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable_SortOrder",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable",
                table: "MenuItems",
                columns: new[] { "CategoryId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_SortOrder",
                table: "MenuItems",
                columns: new[] { "CategoryId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories",
                column: "SortOrder",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_SortOrder",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_IsAvailable_SortOrder",
                table: "MenuItems",
                columns: new[] { "CategoryId", "IsAvailable", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_SortOrder",
                table: "MenuCategories",
                column: "SortOrder");
        }
    }
}
