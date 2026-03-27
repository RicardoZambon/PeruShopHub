using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        private static readonly string[] TenantTables = new[]
        {
            "Products", "ProductVariants", "ProductCostHistories", "Categories",
            "VariationFields", "Orders", "OrderItems", "OrderCosts", "Customers",
            "PurchaseOrders", "PurchaseOrderItems", "PurchaseOrderCosts",
            "Supplies", "StockMovements", "MarketplaceConnections", "CommissionRules",
            "Notifications", "FileUploads"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Tenants table first (referenced by FKs and data migration)
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            // 2. Create TenantUsers table
            migrationBuilder.CreateTable(
                name: "TenantUsers",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantUsers_SystemUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_UserId",
                table: "TenantUsers",
                column: "UserId");

            // 3. Add IsSuperAdmin to SystemUsers
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "SystemUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 4. Add non-tenant columns (IsActive, Version on entities that gained them)
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Supplies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Supplies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 5. Add TenantId as NULLABLE on all 18 entity tables
            foreach (var table in TenantTables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: true);
            }

            // 6. Insert demo tenant
            migrationBuilder.Sql(@"
                INSERT INTO ""Tenants"" (""Id"", ""Name"", ""Slug"", ""IsActive"", ""CreatedAt"")
                VALUES ('a0000000-0000-0000-0000-000000000001', 'Demo Shop', 'demo-shop', true, NOW());
            ");

            // 7. Set all existing data to demo tenant
            foreach (var table in TenantTables)
            {
                migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""TenantId"" = 'a0000000-0000-0000-0000-000000000001' WHERE ""TenantId"" IS NULL;");
            }

            // 8. Promote admin to super-admin
            migrationBuilder.Sql(@"
                UPDATE ""SystemUsers"" SET ""IsSuperAdmin"" = true WHERE ""Email"" = 'admin@perushophub.com';
            ");

            // 9. Create TenantUser records for all existing users
            migrationBuilder.Sql(@"
                INSERT INTO ""TenantUsers"" (""TenantId"", ""UserId"", ""Role"", ""CreatedAt"")
                SELECT 'a0000000-0000-0000-0000-000000000001', ""Id"",
                    CASE
                        WHEN ""Email"" = 'admin@perushophub.com' THEN 'Owner'
                        WHEN ""Email"" = 'gerente@perushophub.com' THEN 'Manager'
                        ELSE 'Viewer'
                    END,
                    NOW()
                FROM ""SystemUsers""
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""TenantUsers"" WHERE ""UserId"" = ""SystemUsers"".""Id""
                );
            ");

            // 10. Alter all TenantId columns to non-nullable now that data is populated
            foreach (var table in TenantTables)
            {
                migrationBuilder.AlterColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldNullable: true);
            }

            // 11. Create indexes on TenantId columns
            foreach (var table in TenantTables)
            {
                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_TenantId",
                    table: table,
                    column: "TenantId");
            }

            // 12. Drop Role column from SystemUsers (replaced by TenantUsers.Role)
            migrationBuilder.DropColumn(
                name: "Role",
                table: "SystemUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add Role column to SystemUsers
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "SystemUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Populate Role from TenantUsers data before dropping
            migrationBuilder.Sql(@"
                UPDATE ""SystemUsers"" su
                SET ""Role"" = COALESCE(
                    (SELECT tu.""Role"" FROM ""TenantUsers"" tu WHERE tu.""UserId"" = su.""Id"" LIMIT 1),
                    'Viewer'
                );
            ");

            // Drop TenantId indexes and columns from all entity tables
            foreach (var table in TenantTables)
            {
                migrationBuilder.DropIndex(
                    name: $"IX_{table}_TenantId",
                    table: table);

                migrationBuilder.DropColumn(
                    name: "TenantId",
                    table: table);
            }

            // Drop Version columns
            migrationBuilder.DropColumn(name: "Version", table: "Categories");
            migrationBuilder.DropColumn(name: "Version", table: "Products");
            migrationBuilder.DropColumn(name: "Version", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "Version", table: "Supplies");

            // Drop IsActive from Supplies
            migrationBuilder.DropColumn(name: "IsActive", table: "Supplies");

            // Drop IsSuperAdmin from SystemUsers
            migrationBuilder.DropColumn(name: "IsSuperAdmin", table: "SystemUsers");

            // Drop TenantUsers and Tenants tables
            migrationBuilder.DropTable(name: "TenantUsers");
            migrationBuilder.DropTable(name: "Tenants");
        }
    }
}
