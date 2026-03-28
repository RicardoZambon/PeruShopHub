using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class SkuProfitabilityViewConfiguration : IEntityTypeConfiguration<SkuProfitabilityView>
{
    public void Configure(EntityTypeBuilder<SkuProfitabilityView> builder)
    {
        builder.ToView("sku_profitability");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).HasMaxLength(100);
        builder.Property(v => v.Name).HasMaxLength(500);
        builder.Property(v => v.TotalRevenue).HasPrecision(18, 4);
        builder.Property(v => v.CostCmv).HasPrecision(18, 4);
        builder.Property(v => v.CostCommissions).HasPrecision(18, 4);
        builder.Property(v => v.CostShipping).HasPrecision(18, 4);
        builder.Property(v => v.CostTaxes).HasPrecision(18, 4);
        builder.Property(v => v.CostOther).HasPrecision(18, 4);
        builder.Property(v => v.TotalCosts).HasPrecision(18, 4);
        builder.Property(v => v.TotalProfit).HasPrecision(18, 4);
        builder.Property(v => v.AvgMargin).HasPrecision(18, 4);

        builder.Property(v => v.TenantId).IsRequired();
    }
}
