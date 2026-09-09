namespace SmartSupermarket.Backend.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}
