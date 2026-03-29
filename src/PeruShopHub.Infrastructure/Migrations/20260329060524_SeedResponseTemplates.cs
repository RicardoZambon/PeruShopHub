using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedResponseTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(
                "PeruShopHub.Infrastructure.Persistence.Seeds.SeedResponseTemplates.sql");
            using var reader = new System.IO.StreamReader(stream!);
            var sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""ResponseTemplates"" WHERE ""Id"" IN (
                'f1000001-0000-0000-0000-000000000001',
                'f1000001-0000-0000-0000-000000000002',
                'f1000001-0000-0000-0000-000000000003',
                'f1000001-0000-0000-0000-000000000004',
                'f1000001-0000-0000-0000-000000000005'
            );");
        }
    }
}
