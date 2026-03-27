using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class PurchaseOrderCostConfiguration : IEntityTypeConfiguration<PurchaseOrderCost>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderCost> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.DistributionMethod).HasMaxLength(50).IsRequired();

        builder.Property(c => c.Value).HasPrecision(18, 4);

        builder.HasOne(c => c.PurchaseOrder)
            .WithMany(po => po.Costs)
            .HasForeignKey(c => c.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId);
    }
}
