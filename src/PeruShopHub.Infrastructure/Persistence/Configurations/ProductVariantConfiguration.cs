using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Attributes).HasColumnType("jsonb").IsRequired();

        builder.Property(v => v.Price).HasPrecision(18, 4);
        builder.Property(v => v.PurchaseCost).HasPrecision(18, 4);
        builder.Property(v => v.Weight).HasPrecision(18, 4);
        builder.Property(v => v.Height).HasPrecision(18, 4);
        builder.Property(v => v.Width).HasPrecision(18, 4);
        builder.Property(v => v.Length).HasPrecision(18, 4);
    }
}
