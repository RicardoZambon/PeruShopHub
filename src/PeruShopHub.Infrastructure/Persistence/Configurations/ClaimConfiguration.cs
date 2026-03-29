using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<MarketplaceClaim>
{
    public void Configure(EntityTypeBuilder<MarketplaceClaim> builder)
    {
        builder.ToTable("claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ExternalOrderId).HasMaxLength(100);
        builder.Property(c => c.Type).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();
        builder.Property(c => c.BuyerComment).HasMaxLength(2000);
        builder.Property(c => c.SellerComment).HasMaxLength(2000);
        builder.Property(c => c.BuyerName).HasMaxLength(200);
        builder.Property(c => c.Resolution).HasMaxLength(500);
        builder.Property(c => c.ProductName).HasMaxLength(500);
        builder.Property(c => c.Amount).HasColumnType("numeric(18,4)");

        builder.HasIndex(c => new { c.TenantId, c.ExternalId }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.OrderId });
    }
}
