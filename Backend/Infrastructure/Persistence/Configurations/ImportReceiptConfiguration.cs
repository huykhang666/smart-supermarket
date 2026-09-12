using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class ImportReceiptConfiguration : IEntityTypeConfiguration<ImportReceipt>
{
    public void Configure(EntityTypeBuilder<ImportReceipt> builder)
    {
        builder.ToTable("ImportReceipt");

        builder.HasKey(i => i.ImportReceiptId);

        builder.Property(i => i.ReceiptCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => i.ReceiptCode)
            .IsUnique();

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(i => i.Note)
            .HasMaxLength(500);

        builder.Property(i => i.ImportDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(i => i.Supplier)
            .WithMany()
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ImportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ImportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(i => i.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
