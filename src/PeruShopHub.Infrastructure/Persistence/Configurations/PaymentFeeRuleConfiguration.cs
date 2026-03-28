using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class PaymentFeeRuleConfiguration : IEntityTypeConfiguration<PaymentFeeRule>
{
    public void Configure(EntityTypeBuilder<PaymentFeeRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.FeePercentage).HasPrecision(18, 4);
        builder.Property(r => r.InstallmentMin).IsRequired();
        builder.Property(r => r.InstallmentMax).IsRequired();

        builder.HasIndex(r => new { r.InstallmentMin, r.InstallmentMax });

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
    }
}
