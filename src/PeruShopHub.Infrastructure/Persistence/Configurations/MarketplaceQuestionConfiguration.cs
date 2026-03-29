using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class MarketplaceQuestionConfiguration : IEntityTypeConfiguration<MarketplaceQuestion>
{
    public void Configure(EntityTypeBuilder<MarketplaceQuestion> builder)
    {
        builder.ToTable("marketplace_questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.TenantId).IsRequired();
        builder.Property(q => q.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(q => q.ExternalItemId).HasMaxLength(100).IsRequired();
        builder.Property(q => q.BuyerName).HasMaxLength(200).IsRequired();
        builder.Property(q => q.QuestionText).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.AnswerText).HasMaxLength(2000);
        builder.Property(q => q.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(q => new { q.TenantId, q.ExternalId }).IsUnique();
        builder.HasIndex(q => new { q.TenantId, q.ExternalItemId });
        builder.HasIndex(q => new { q.TenantId, q.Status });
    }
}
