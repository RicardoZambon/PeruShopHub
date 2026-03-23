using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ExternalOrderId).HasMaxLength(100).IsRequired();
        builder.HasIndex(o => o.ExternalOrderId).IsUnique();

        builder.Property(o => o.BuyerName).HasMaxLength(300).IsRequired();
        builder.Property(o => o.BuyerNickname).HasMaxLength(200);
        builder.Property(o => o.BuyerEmail).HasMaxLength(300);
        builder.Property(o => o.BuyerPhone).HasMaxLength(50);
        builder.Property(o => o.Status).HasMaxLength(50).IsRequired();
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.Carrier).HasMaxLength(200);
        builder.Property(o => o.LogisticType).HasMaxLength(100);
        builder.Property(o => o.PaymentMethod).HasMaxLength(100);
        builder.Property(o => o.PaymentStatus).HasMaxLength(50);

        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);
        builder.Property(o => o.Profit).HasPrecision(18, 4);
        builder.Property(o => o.PaymentAmount).HasPrecision(18, 4);

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Costs)
            .WithOne(c => c.Order)
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
