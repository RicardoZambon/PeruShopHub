using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuProfitabilityMaterializedView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE MATERIALIZED VIEW IF NOT EXISTS sku_profitability AS
SELECT
    gen_random_uuid() AS ""Id"",
    oi.""TenantId"",
    oi.""ProductId"",
    oi.""Sku"",
    oi.""Name"",
    COUNT(DISTINCT oi.""OrderId"") AS ""TotalOrders"",
    SUM(oi.""Quantity"") AS ""TotalUnits"",
    SUM(oi.""Subtotal"") AS ""TotalRevenue"",
    COALESCE(SUM(
        CASE WHEN LOWER(oc.""Category"") IN ('product_cost', 'cmv')
        THEN oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ELSE 0 END
    ), 0) AS ""CostCmv"",
    COALESCE(SUM(
        CASE WHEN LOWER(oc.""Category"") IN ('marketplace_commission', 'commission')
        THEN oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ELSE 0 END
    ), 0) AS ""CostCommissions"",
    COALESCE(SUM(
        CASE WHEN LOWER(oc.""Category"") IN ('shipping_seller', 'shipping')
        THEN oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ELSE 0 END
    ), 0) AS ""CostShipping"",
    COALESCE(SUM(
        CASE WHEN LOWER(oc.""Category"") IN ('tax', 'taxes')
        THEN oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ELSE 0 END
    ), 0) AS ""CostTaxes"",
    COALESCE(SUM(
        CASE WHEN LOWER(oc.""Category"") NOT IN ('product_cost', 'cmv', 'marketplace_commission', 'commission', 'shipping_seller', 'shipping', 'tax', 'taxes')
        THEN oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ELSE 0 END
    ), 0) AS ""CostOther"",
    COALESCE(SUM(
        oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
    ), 0) AS ""TotalCosts"",
    SUM(oi.""Subtotal"") - COALESCE(SUM(
        oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
    ), 0) AS ""TotalProfit"",
    CASE WHEN SUM(oi.""Subtotal"") = 0 THEN 0
    ELSE ROUND(
        (SUM(oi.""Subtotal"") - COALESCE(SUM(
            oc.""Value"" * (CASE WHEN o.""TotalAmount"" = 0 THEN 0 ELSE oi.""Subtotal"" / o.""TotalAmount"" END)
        ), 0)) / SUM(oi.""Subtotal"") * 100, 2)
    END AS ""AvgMargin""
FROM ""OrderItems"" oi
INNER JOIN ""Orders"" o ON o.""Id"" = oi.""OrderId""
LEFT JOIN ""OrderCosts"" oc ON oc.""OrderId"" = o.""Id""
GROUP BY oi.""TenantId"", oi.""ProductId"", oi.""Sku"", oi.""Name"";
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ix_sku_profitability_tenant_sku
ON sku_profitability (""TenantId"", ""Sku"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS sku_profitability;");
        }
    }
}
