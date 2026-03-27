using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.MarketplaceId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.CategoryPattern).HasMaxLength(200);
        builder.Property(r => r.ListingType).HasMaxLength(100);

        builder.Property(r => r.Rate).HasPrecision(18, 4);

        builder.HasIndex(r => new { r.MarketplaceId, r.CategoryPattern, r.ListingType });

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
    }
}
