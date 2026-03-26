using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySkuPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkuPrefix",
                table: "Categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkuPrefix",
                table: "Categories");
        }
    }
}
