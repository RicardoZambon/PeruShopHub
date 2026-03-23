using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UnitCost).HasPrecision(18, 4);
        builder.Property(i => i.TotalCost).HasPrecision(18, 4);
        builder.Property(i => i.AllocatedAdditionalCost).HasPrecision(18, 4);
        builder.Property(i => i.EffectiveUnitCost).HasPrecision(18, 4);

        builder.HasOne(i => i.PurchaseOrder)
            .WithMany(po => po.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
