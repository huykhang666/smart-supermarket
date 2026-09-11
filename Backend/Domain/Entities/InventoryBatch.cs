using System.ComponentModel.DataAnnotations;

namespace SmartSupermarket.Backend.Domain.Entities;

public class InventoryBatch
{
    [Key]
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = "Good";
    public string StorageLocation { get; set; } = "Kệ A1";

    public Product Product { get; set; } = null!;
}
