using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ResponseTimeSettingsConfiguration : IEntityTypeConfiguration<ResponseTimeSettings>
{
    public void Configure(EntityTypeBuilder<ResponseTimeSettings> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.QuestionThresholdHours).IsRequired();
        builder.Property(r => r.MessageThresholdHours).IsRequired();

        builder.HasIndex(r => r.TenantId).IsUnique();
    }
}
