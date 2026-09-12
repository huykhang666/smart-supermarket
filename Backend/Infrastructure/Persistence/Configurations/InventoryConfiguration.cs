using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventory");

        builder.HasKey(i => i.InventoryId);

        builder.HasIndex(i => new { i.ProductId, i.BranchId })
            .IsUnique();

        builder.Property(i => i.QuantityOnHand)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.MinStockLevel)
            .HasDefaultValue(10)
            .IsRequired();

        builder.Property(i => i.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
