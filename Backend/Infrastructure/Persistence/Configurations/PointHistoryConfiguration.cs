using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class PointHistoryConfiguration : IEntityTypeConfiguration<PointHistory>
{
    public void Configure(EntityTypeBuilder<PointHistory> builder)
    {
        builder.ToTable("PointHistories");

        builder.HasKey(p => p.PointHistoryId);

        builder.Property(p => p.PointHistoryId)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.CustomerId)
            .IsRequired();

        builder.Property(p => p.PointChange)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasDefaultValue((byte)1)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.PointHistories)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
