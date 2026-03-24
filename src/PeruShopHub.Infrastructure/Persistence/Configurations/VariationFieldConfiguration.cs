using Microsoft.EntityFrameworkCore;
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
        builder.Property(v => v.Options).HasColumnType("jsonb");

        builder.HasOne(v => v.Category)
            .WithMany()
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.CategoryId);
    }
}
