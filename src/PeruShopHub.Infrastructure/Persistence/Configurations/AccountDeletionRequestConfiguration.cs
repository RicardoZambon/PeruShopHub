using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class AccountDeletionRequestConfiguration : IEntityTypeConfiguration<AccountDeletionRequest>
{
    public void Configure(EntityTypeBuilder<AccountDeletionRequest> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.UserId, a.Status });
        builder.HasIndex(a => new { a.Status, a.ScheduledDeletionAt });
    }
}
