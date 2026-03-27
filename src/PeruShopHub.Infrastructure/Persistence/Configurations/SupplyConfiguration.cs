using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Sku).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Category).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Supplier).HasMaxLength(300);
        builder.Property(s => s.Status).HasMaxLength(50).IsRequired();

        builder.Property(s => s.UnitCost).HasPrecision(18, 4);

        builder.Property(s => s.IsActive).HasDefaultValue(true);
    }
}
