using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("OrderDetails");

        builder.HasKey(od => od.OrderDetailId);

        builder.Property(od => od.Quantity)
            .IsRequired();

        builder.Property(od => od.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(od => od.SubTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(od => od.Product)
            .WithMany()
            .HasForeignKey(od => od.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}