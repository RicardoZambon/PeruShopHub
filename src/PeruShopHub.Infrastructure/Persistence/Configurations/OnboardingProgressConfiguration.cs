using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class OnboardingProgressConfiguration : IEntityTypeConfiguration<OnboardingProgress>
{
    public void Configure(EntityTypeBuilder<OnboardingProgress> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.StepsCompleted)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.TenantId).IsRequired();
        builder.HasIndex(o => o.TenantId).IsUnique();
    }
}
