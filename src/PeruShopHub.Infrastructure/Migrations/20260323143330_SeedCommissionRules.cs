using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCommissionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(
                "PeruShopHub.Infrastructure.Persistence.Seeds.SeedCommissionRules.sql");
            using var reader = new System.IO.StreamReader(stream!);
            var sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""CommissionRules"" WHERE ""Id"" IN (
                'a1b2c3d4-0001-4000-8000-000000000001',
                'a1b2c3d4-0002-4000-8000-000000000002',
                'a1b2c3d4-0003-4000-8000-000000000003',
                'a1b2c3d4-0004-4000-8000-000000000004',
                'a1b2c3d4-0005-4000-8000-000000000005',
                'a1b2c3d4-0006-4000-8000-000000000006',
                'a1b2c3d4-0007-4000-8000-000000000007',
                'a1b2c3d4-0008-4000-8000-000000000008',
                'a1b2c3d4-0009-4000-8000-000000000009',
                'a1b2c3d4-000a-4000-8000-00000000000a',
                'a1b2c3d4-000b-4000-8000-00000000000b',
                'a1b2c3d4-000c-4000-8000-00000000000c',
                'a1b2c3d4-000d-4000-8000-00000000000d'
            );");
        }
    }
}
