using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class OrderCostConfiguration : IEntityTypeConfiguration<OrderCost>
{
    public void Configure(EntityTypeBuilder<OrderCost> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Category).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Source).HasMaxLength(50).IsRequired();

        builder.Property(c => c.Value).HasPrecision(18, 4);
    }
}
