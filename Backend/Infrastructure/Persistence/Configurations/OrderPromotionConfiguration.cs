using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class OrderPromotionConfiguration : IEntityTypeConfiguration<OrderPromotion>
{
    public void Configure(EntityTypeBuilder<OrderPromotion> builder)
    {
        builder.ToTable("OrderPromotions");
        builder.HasKey(op => op.OrderPromotionId);

        builder.Property(op => op.DiscountAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(op => op.Order)
            .WithMany(o => o.OrderPromotions)
            .HasForeignKey(op => op.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(op => op.Promotion)
            .WithMany()
            .HasForeignKey(op => op.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}