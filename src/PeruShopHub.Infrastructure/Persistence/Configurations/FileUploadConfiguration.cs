using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class FileUploadConfiguration : IEntityTypeConfiguration<FileUpload>
{
    public void Configure(EntityTypeBuilder<FileUpload> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(500).IsRequired();
        builder.Property(f => f.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(f => new { f.EntityType, f.EntityId });
    }
}
