using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ProductCostHistoryConfiguration : IEntityTypeConfiguration<ProductCostHistory>
{
    public void Configure(EntityTypeBuilder<ProductCostHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Reason).HasMaxLength(500).IsRequired();

        builder.Property(h => h.PreviousCost).HasPrecision(18, 4);
        builder.Property(h => h.NewCost).HasPrecision(18, 4);
        builder.Property(h => h.UnitCostPaid).HasPrecision(18, 4);

        builder.HasOne(h => h.Product)
            .WithMany()
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Variant)
            .WithMany()
            .HasForeignKey(h => h.VariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.PurchaseOrder)
            .WithMany()
            .HasForeignKey(h => h.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
