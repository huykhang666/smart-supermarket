namespace SmartSupermarket.Backend.Domain.Entities;

public class ImportDetail
{
    public int ImportDetailId { get; set; }
    public int ImportReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation properties
    public ImportReceipt ImportReceipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
