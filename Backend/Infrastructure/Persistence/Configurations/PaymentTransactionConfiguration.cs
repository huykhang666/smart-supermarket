using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(pt => pt.PaymentTransactionId);

        builder.Property(pt => pt.TransactionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(pt => pt.TransactionCode)
            .IsUnique();

        builder.Property(pt => pt.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pt => pt.Gateway)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pt => pt.GatewayTransactionId)
            .HasMaxLength(150);

        builder.Property(pt => pt.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pt => pt.PaymentUrl)
            .HasMaxLength(500);

        builder.Property(pt => pt.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(pt => pt.Order)
            .WithMany(o => o.PaymentTransactions)
            .HasForeignKey(pt => pt.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
