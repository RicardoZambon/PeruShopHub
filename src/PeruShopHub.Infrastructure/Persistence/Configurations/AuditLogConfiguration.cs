using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.OldValue).HasColumnType("jsonb");
        builder.Property(a => a.NewValue).HasColumnType("jsonb");

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.CreatedAt });
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
    }
}
