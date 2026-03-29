using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageCostAccumulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FulfillmentType",
                table: "MarketplaceListings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockReconciliationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketplaceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemsChecked = table.Column<int>(type: "integer", nullable: false),
                    Matches = table.Column<int>(type: "integer", nullable: false),
                    Discrepancies = table.Column<int>(type: "integer", nullable: false),
                    AutoCorrected = table.Column<int>(type: "integer", nullable: false),
                    ManualReviewRequired = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageCostAccumulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DailyCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CumulativeCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DaysStored = table.Column<int>(type: "integer", nullable: false),
                    SizeCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PenaltyMultiplier = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageCostAccumulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageCostAccumulations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockReconciliationReportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LocalQuantity = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceQuantity = table.Column<int>(type: "integer", nullable: false),
                    Difference = table.Column<int>(type: "integer", nullable: false),
                    Resolution = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliationReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReconciliationReportItems_ProductVariants_ProductVaria~",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockReconciliationReportItems_StockReconciliationReports_R~",
                        column: x => x.ReportId,
                        principalTable: "StockReconciliationReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationReportItems_ProductVariantId",
                table: "StockReconciliationReportItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationReportItems_ReportId",
                table: "StockReconciliationReportItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationReportItems_TenantId",
                table: "StockReconciliationReportItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationReports_TenantId",
                table: "StockReconciliationReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationReports_TenantId_StartedAt",
                table: "StockReconciliationReports",
                columns: new[] { "TenantId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StorageCostAccumulations_ProductId_Date",
                table: "StorageCostAccumulations",
                columns: new[] { "ProductId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageCostAccumulations_TenantId",
                table: "StorageCostAccumulations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockReconciliationReportItems");

            migrationBuilder.DropTable(
                name: "StorageCostAccumulations");

            migrationBuilder.DropTable(
                name: "StockReconciliationReports");

            migrationBuilder.DropColumn(
                name: "FulfillmentType",
                table: "MarketplaceListings");
        }
    }
}
