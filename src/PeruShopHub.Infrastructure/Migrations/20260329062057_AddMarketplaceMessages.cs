using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marketplace_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalPackId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_messages_TenantId_ExternalMessageId",
                table: "marketplace_messages",
                columns: new[] { "TenantId", "ExternalMessageId" },
                unique: true,
                filter: "\"ExternalMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_messages_TenantId_ExternalPackId",
                table: "marketplace_messages",
                columns: new[] { "TenantId", "ExternalPackId" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_messages_TenantId_IsRead",
                table: "marketplace_messages",
                columns: new[] { "TenantId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_messages_TenantId_OrderId",
                table: "marketplace_messages",
                columns: new[] { "TenantId", "OrderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_messages");
        }
    }
}
