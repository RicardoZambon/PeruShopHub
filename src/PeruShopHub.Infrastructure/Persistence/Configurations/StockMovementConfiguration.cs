using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.Property(m => m.CreatedBy).HasMaxLength(200);

        builder.Property(m => m.UnitCost).HasPrecision(18, 4);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Variant)
            .WithMany()
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(m => m.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
    }
}
