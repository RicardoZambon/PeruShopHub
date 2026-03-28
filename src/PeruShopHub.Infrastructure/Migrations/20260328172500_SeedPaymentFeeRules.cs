using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPaymentFeeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(
                "PeruShopHub.Infrastructure.Persistence.Seeds.SeedPaymentFeeRules.sql");
            using var reader = new System.IO.StreamReader(stream!);
            var sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""PaymentFeeRules"" WHERE ""Id"" IN (
                'b2c3d4e5-0001-4000-8000-000000000001',
                'b2c3d4e5-0002-4000-8000-000000000002',
                'b2c3d4e5-0003-4000-8000-000000000003',
                'b2c3d4e5-0004-4000-8000-000000000004',
                'b2c3d4e5-0005-4000-8000-000000000005',
                'b2c3d4e5-0006-4000-8000-000000000006'
            );");
        }
    }
}
