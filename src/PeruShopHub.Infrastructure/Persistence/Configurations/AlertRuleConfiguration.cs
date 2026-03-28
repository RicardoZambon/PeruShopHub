using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Threshold).HasPrecision(18, 4);

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.TenantId, a.IsActive });

        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
