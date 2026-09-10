using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.PromotionId);

        builder.Property(p => p.PromotionCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PromotionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.MinimumOrderAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.MaximumDiscountAmount)
            .HasColumnType("decimal(18,2)");
    }
}
