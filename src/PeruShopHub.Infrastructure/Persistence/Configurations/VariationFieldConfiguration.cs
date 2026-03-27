using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class VariationFieldConfiguration : IEntityTypeConfiguration<VariationField>
{
    public void Configure(EntityTypeBuilder<VariationField> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Type).HasMaxLength(20).IsRequired();

        builder.Property(v => v.Options)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
                new ValueComparer<string[]>(
                    (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                    v => v.ToArray()));

        builder.HasOne(v => v.Category)
            .WithMany()
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.CategoryId);

        builder.Property(v => v.TenantId).IsRequired();
        builder.HasIndex(v => v.TenantId);
    }
}
