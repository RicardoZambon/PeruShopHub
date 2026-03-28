using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.MarketplaceId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ListingType).HasMaxLength(100);
        builder.Property(r => r.TargetMarginPercent).HasPrecision(18, 4);
        builder.Property(r => r.SuggestedPrice).HasPrecision(18, 4);

        builder.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.ProductId, r.MarketplaceId }).IsUnique();
        builder.HasIndex(r => r.TenantId);
    }
}
