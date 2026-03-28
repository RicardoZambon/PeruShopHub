using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class TaxProfileConfiguration : IEntityTypeConfiguration<TaxProfile>
{
    public void Configure(EntityTypeBuilder<TaxProfile> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaxRegime).HasMaxLength(50).IsRequired();
        builder.Property(t => t.AliquotPercentage).HasPrecision(18, 4);
        builder.Property(t => t.State).HasMaxLength(2);

        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId).IsUnique();
    }
}
