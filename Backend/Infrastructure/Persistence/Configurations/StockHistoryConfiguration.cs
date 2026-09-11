using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class StockHistoryConfiguration : IEntityTypeConfiguration<StockHistory>
{
    public void Configure(EntityTypeBuilder<StockHistory> builder)
    {
        builder.ToTable("StockHistory");

        builder.HasKey(s => s.StockHistoryId);

        builder.Property(s => s.ChangeType)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(s => s.QuantityChange)
            .IsRequired();

        builder.Property(s => s.QuantityBefore)
            .IsRequired();

        builder.Property(s => s.QuantityAfter)
            .IsRequired();

        builder.Property(s => s.Note)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
