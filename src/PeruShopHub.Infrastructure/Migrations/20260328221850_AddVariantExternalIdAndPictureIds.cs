using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantExternalIdAndPictureIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PictureIds",
                table: "ProductVariants",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_TenantId_ExternalId",
                table: "ProductVariants",
                columns: new[] { "TenantId", "ExternalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_TenantId_ExternalId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PictureIds",
                table: "ProductVariants");
        }
    }
}
