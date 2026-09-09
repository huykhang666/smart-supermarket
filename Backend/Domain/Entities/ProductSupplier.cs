namespace SmartSupermarket.Backend.Domain.Entities;

public class ProductSupplier
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public decimal PurchasePrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int LeadTime { get; set; } = 1;
    public int MinimumOrderQuantity { get; set; } = 1;
    public decimal Rating { get; set; } = 5.00m;
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
