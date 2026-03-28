using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.MarketplaceId).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Title).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Status).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Price).HasPrecision(18, 4);
        builder.Property(l => l.CategoryId).HasMaxLength(100);
        builder.Property(l => l.Permalink).HasMaxLength(1000);
        builder.Property(l => l.ThumbnailUrl).HasMaxLength(1000);

        builder.HasIndex(l => new { l.TenantId, l.MarketplaceId, l.ExternalId }).IsUnique();
        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(l => l.TenantId).IsRequired();
    }
}
