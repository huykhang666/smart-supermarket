using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class ImportDetailConfiguration : IEntityTypeConfiguration<ImportDetail>
{
    public void Configure(EntityTypeBuilder<ImportDetail> builder)
    {
        builder.ToTable("ImportDetail");

        builder.HasKey(i => i.ImportDetailId);

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.CostPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.SubTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(i => i.ImportReceipt)
            .WithMany(r => r.ImportDetails)
            .HasForeignKey(i => i.ImportReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
