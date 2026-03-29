using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class UserDataExportConfiguration : IEntityTypeConfiguration<UserDataExport>
{
    public void Configure(EntityTypeBuilder<UserDataExport> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FilePath).HasMaxLength(500);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
    }
}
