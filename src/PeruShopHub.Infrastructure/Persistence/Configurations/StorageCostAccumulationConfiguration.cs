using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class StorageCostAccumulationConfiguration : IEntityTypeConfiguration<StorageCostAccumulation>
{
    public void Configure(EntityTypeBuilder<StorageCostAccumulation> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DailyCost).HasPrecision(18, 4);
        builder.Property(s => s.CumulativeCost).HasPrecision(18, 4);
        builder.Property(s => s.PenaltyMultiplier).HasPrecision(18, 4);
        builder.Property(s => s.SizeCategory).HasMaxLength(50).IsRequired();

        builder.Property(s => s.TenantId).IsRequired();
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.ProductId, s.Date }).IsUnique();

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
