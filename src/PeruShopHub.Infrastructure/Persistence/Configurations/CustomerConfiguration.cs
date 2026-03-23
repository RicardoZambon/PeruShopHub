using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Nickname).HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(300);
        builder.HasIndex(c => c.Email);
        builder.Property(c => c.Phone).HasMaxLength(50);

        builder.Property(c => c.TotalSpent).HasPrecision(18, 4);
    }
}
