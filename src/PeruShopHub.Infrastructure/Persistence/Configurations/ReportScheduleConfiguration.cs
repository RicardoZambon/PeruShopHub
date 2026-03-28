using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Frequency).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Recipients).HasMaxLength(1000).IsRequired();

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
    }
}
