using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "SystemUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "SystemUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiresAt",
                table: "SystemUsers",
                type: "timestamp with time zone",
                nullable: true);

            // Seed admin user password (admin123)
            migrationBuilder.Sql(@"
                UPDATE ""SystemUsers"" SET
                    ""PasswordHash"" = '$2a$11$aDfIVIhAnHO.HXRjlU6HTOfzlOpVYR0SHpyEpiKN3r4pvydOs7Oi2'
                WHERE ""Email"" = 'admin@perushophub.com';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "SystemUsers");
        }
    }
}
