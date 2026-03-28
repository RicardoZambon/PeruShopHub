using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFeeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentFeeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentMin = table.Column<int>(type: "integer", nullable: false),
                    InstallmentMax = table.Column<int>(type: "integer", nullable: false),
                    FeePercentage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentFeeRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFeeRules_InstallmentMin_InstallmentMax",
                table: "PaymentFeeRules",
                columns: new[] { "InstallmentMin", "InstallmentMax" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFeeRules_TenantId",
                table: "PaymentFeeRules",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentFeeRules");
        }
    }
}
