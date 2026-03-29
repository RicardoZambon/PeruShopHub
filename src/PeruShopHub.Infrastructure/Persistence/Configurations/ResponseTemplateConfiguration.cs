using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ResponseTemplateConfiguration : IEntityTypeConfiguration<ResponseTemplate>
{
    public void Configure(EntityTypeBuilder<ResponseTemplate> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Name).HasMaxLength(200).IsRequired();
        builder.Property(rt => rt.Category).HasMaxLength(100).IsRequired();
        builder.Property(rt => rt.Body).IsRequired();
        builder.Property(rt => rt.Placeholders).HasColumnType("jsonb");

        builder.Property(rt => rt.Version).IsConcurrencyToken();

        builder.Property(rt => rt.TenantId).IsRequired();
        builder.HasIndex(rt => rt.TenantId);
        builder.HasIndex(rt => new { rt.TenantId, rt.Name }).IsUnique();
    }
}
