using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(np => np.Id);

        builder.Property(np => np.TenantId).IsRequired();
        builder.Property(np => np.UserId).IsRequired();
        builder.Property(np => np.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(np => np.EmailEnabled).IsRequired();
        builder.Property(np => np.InAppEnabled).IsRequired();

        builder.HasOne(np => np.User)
            .WithMany()
            .HasForeignKey(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(np => new { np.TenantId, np.UserId, np.Type }).IsUnique();
    }
}
