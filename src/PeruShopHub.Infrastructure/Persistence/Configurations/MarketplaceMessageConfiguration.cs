using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class MarketplaceMessageConfiguration : IEntityTypeConfiguration<MarketplaceMessage>
{
    public void Configure(EntityTypeBuilder<MarketplaceMessage> builder)
    {
        builder.ToTable("marketplace_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.ExternalPackId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.SenderType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Text).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.ExternalMessageId).HasMaxLength(100);

        builder.HasIndex(m => new { m.TenantId, m.OrderId });
        builder.HasIndex(m => new { m.TenantId, m.ExternalPackId });
        builder.HasIndex(m => new { m.TenantId, m.IsRead });
        builder.HasIndex(m => new { m.TenantId, m.ExternalMessageId }).IsUnique()
            .HasFilter("\"ExternalMessageId\" IS NOT NULL");
    }
}
