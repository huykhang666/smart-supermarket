namespace SmartSupermarket.Backend.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int OrderIndex { get; set; }
    public string? ImageUrl { get; set; }
    public byte Status { get; set; } = 1; // 1 = Active, 2 = Inactive
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}
