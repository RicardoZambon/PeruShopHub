using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthFieldsToMarketplaceConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessTokenProtected",
                table: "MarketplaceConnections",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUserId",
                table: "MarketplaceConnections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenProtected",
                table: "MarketplaceConnections",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MarketplaceConnections",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Disconnected");

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "MarketplaceConnections",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenProtected",
                table: "MarketplaceConnections");

            migrationBuilder.DropColumn(
                name: "ExternalUserId",
                table: "MarketplaceConnections");

            migrationBuilder.DropColumn(
                name: "RefreshTokenProtected",
                table: "MarketplaceConnections");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MarketplaceConnections");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "MarketplaceConnections");
        }
    }
}
