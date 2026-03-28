using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMinMaxStockToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxStock",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinStock",
                table: "Products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinStock",
                table: "Products");
        }
    }
}
