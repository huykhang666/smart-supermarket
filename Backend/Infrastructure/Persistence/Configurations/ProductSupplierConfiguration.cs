using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class ProductSupplierConfiguration : IEntityTypeConfiguration<ProductSupplier>
{
    public void Configure(EntityTypeBuilder<ProductSupplier> builder)
    {
        builder.ToTable("ProductSupplier");

        // Composite PK
        builder.HasKey(ps => new { ps.ProductId, ps.SupplierId });

        builder.Property(ps => ps.PurchasePrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ps => ps.SupplierProductCode)
            .HasMaxLength(50);

        builder.Property(ps => ps.LeadTime)
            .HasDefaultValue(1);

        builder.Property(ps => ps.MinimumOrderQuantity)
            .HasDefaultValue(1);

        builder.Property(ps => ps.Rating)
            .HasPrecision(3, 2)
            .HasDefaultValue(5.00m);

        builder.Property(ps => ps.IsDefault)
            .HasDefaultValue(false);

        builder.Property(ps => ps.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign keys
        builder.HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSuppliers)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Supplier)
            .WithMany(s => s.ProductSuppliers)
            .HasForeignKey(ps => ps.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(ps => ps.SupplierId);
        builder.HasIndex(ps => new { ps.ProductId, ps.IsDefault });
    }
}
