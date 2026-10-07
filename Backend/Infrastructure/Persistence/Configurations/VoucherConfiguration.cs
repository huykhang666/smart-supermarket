using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("Vouchers");

        builder.HasKey(v => v.VoucherId);

        builder.Property(v => v.VoucherId)
            .ValueGeneratedOnAdd();

        builder.Property(v => v.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(v => v.Code)
            .IsUnique();

        builder.Property(v => v.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(v => v.MinimumOrderAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(v => v.ExpiryDate)
            .IsRequired();

        builder.Property(v => v.IsUsed)
            .HasDefaultValue(false);

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Vouchers)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(v => v.Order)
            .WithMany()
            .HasForeignKey(v => v.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
