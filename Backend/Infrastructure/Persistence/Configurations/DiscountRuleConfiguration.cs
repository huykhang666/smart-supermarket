using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.ToTable("DiscountRule");

        builder.HasKey(d => d.DiscountRuleId);

        builder.Property(d => d.DaysBeforeExpiry)
            .IsRequired();

        builder.Property(d => d.DiscountPercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.Description)
            .HasMaxLength(255);
    }
}
