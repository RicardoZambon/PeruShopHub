using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class StockReconciliationReportConfiguration : IEntityTypeConfiguration<StockReconciliationReport>
{
    public void Configure(EntityTypeBuilder<StockReconciliationReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MarketplaceId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(50).IsRequired();
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.Report)
            .HasForeignKey(i => i.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => new { r.TenantId, r.StartedAt });
    }
}

public class StockReconciliationReportItemConfiguration : IEntityTypeConfiguration<StockReconciliationReportItem>
{
    public void Configure(EntityTypeBuilder<StockReconciliationReportItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Sku).HasMaxLength(100).IsRequired();
        builder.Property(i => i.ProductName).HasMaxLength(500).IsRequired();
        builder.Property(i => i.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Resolution).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(i => i.TenantId).IsRequired();
        builder.HasIndex(i => i.TenantId);
    }
}
