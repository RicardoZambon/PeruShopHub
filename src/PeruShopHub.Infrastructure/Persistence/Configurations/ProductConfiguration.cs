using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(5000);
        builder.Property(p => p.CategoryId).HasMaxLength(100);
        builder.Property(p => p.Supplier).HasMaxLength(300);
        builder.Property(p => p.Status).HasMaxLength(50).IsRequired();

        builder.Property(p => p.Price).HasPrecision(18, 4);
        builder.Property(p => p.PurchaseCost).HasPrecision(18, 4);
        builder.Property(p => p.PackagingCost).HasPrecision(18, 4);
        builder.Property(p => p.Weight).HasPrecision(18, 4);
        builder.Property(p => p.Height).HasPrecision(18, 4);
        builder.Property(p => p.Width).HasPrecision(18, 4);
        builder.Property(p => p.Length).HasPrecision(18, 4);

        builder.Property(p => p.Version).IsConcurrencyToken();

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
