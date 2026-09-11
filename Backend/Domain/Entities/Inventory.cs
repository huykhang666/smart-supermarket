namespace SmartSupermarket.Backend.Domain.Entities;

public class Inventory
{
    public int InventoryId { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public int QuantityOnHand { get; set; }
    public int MinStockLevel { get; set; } = 10;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product Product { get; set; } = null!;
}
