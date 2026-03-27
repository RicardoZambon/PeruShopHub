using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantScopedUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ExternalOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceConnections_MarketplaceId",
                table: "MarketplaceConnections");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_ExternalOrderId",
                table: "Orders",
                columns: new[] { "TenantId", "ExternalOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceConnections_TenantId_MarketplaceId",
                table: "MarketplaceConnections",
                columns: new[] { "TenantId", "MarketplaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId_Slug",
                table: "Categories",
                columns: new[] { "TenantId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_ExternalOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceConnections_TenantId_MarketplaceId",
                table: "MarketplaceConnections");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId_Slug",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExternalOrderId",
                table: "Orders",
                column: "ExternalOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceConnections_MarketplaceId",
                table: "MarketplaceConnections",
                column: "MarketplaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }
    }
}
