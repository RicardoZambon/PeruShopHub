using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class MarketplaceConnectionConfiguration : IEntityTypeConfiguration<MarketplaceConnection>
{
    public void Configure(EntityTypeBuilder<MarketplaceConnection> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MarketplaceId).HasMaxLength(100).IsRequired();
        builder.HasIndex(m => m.MarketplaceId).IsUnique();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Logo).HasMaxLength(500);
        builder.Property(m => m.SellerNickname).HasMaxLength(200);
    }
}
