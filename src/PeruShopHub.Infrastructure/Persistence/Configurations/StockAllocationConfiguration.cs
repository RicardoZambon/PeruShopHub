using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class StockAllocationConfiguration : IEntityTypeConfiguration<StockAllocation>
{
    public void Configure(EntityTypeBuilder<StockAllocation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.MarketplaceId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.AllocatedQuantity).IsRequired();
        builder.Property(a => a.ReservedQuantity).HasDefaultValue(0);

        builder.HasOne(a => a.Variant)
            .WithMany()
            .HasForeignKey(a => a.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ProductVariantId, a.MarketplaceId, a.TenantId }).IsUnique();

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);
    }
}
