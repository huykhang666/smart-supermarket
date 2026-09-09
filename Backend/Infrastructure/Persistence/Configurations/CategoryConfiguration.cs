using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");

        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.CategoryId)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CategoryName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.CategoryName)
            .IsUnique();

        builder.Property(c => c.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(255);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(255);

        builder.Property(c => c.OrderIndex)
            .HasDefaultValue(0);

        builder.Property(c => c.Status)
            .HasDefaultValue((byte)1)
            .HasSentinel((byte)0)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Self-referencing ParentId FK relationship
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
