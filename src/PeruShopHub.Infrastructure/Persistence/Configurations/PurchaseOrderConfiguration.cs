using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasKey(po => po.Id);

        builder.Property(po => po.Supplier).HasMaxLength(200);
        builder.Property(po => po.Status).HasMaxLength(50).IsRequired();
        builder.Property(po => po.Notes).HasMaxLength(2000);

        builder.Property(po => po.Subtotal).HasPrecision(18, 4);
        builder.Property(po => po.AdditionalCosts).HasPrecision(18, 4);
        builder.Property(po => po.Total).HasPrecision(18, 4);
    }
}
