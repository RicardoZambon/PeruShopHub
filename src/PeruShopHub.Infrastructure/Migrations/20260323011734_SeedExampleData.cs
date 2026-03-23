using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedExampleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PeruShopHub.Infrastructure.Persistence.Seeds.SeedData.sql";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""OrderCosts"";
                DELETE FROM ""OrderItems"";
                DELETE FROM ""Orders"";
                DELETE FROM ""ProductVariants"";
                DELETE FROM ""Products"";
                DELETE FROM ""Categories"";
                DELETE FROM ""Customers"";
                DELETE FROM ""Supplies"";
                DELETE FROM ""Notifications"";
                DELETE FROM ""SystemUsers"";
                DELETE FROM ""MarketplaceConnections"";
                DELETE FROM ""FileUploads"";
            ");
        }
    }
}
