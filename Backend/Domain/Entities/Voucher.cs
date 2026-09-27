namespace SmartSupermarket.Backend.Domain.Entities;

public class Voucher
{
    public int VoucherId { get; set; }
    public int? CustomerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal MinimumOrderAmount { get; set; } = 0;
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer? Customer { get; set; }
    public Order? Order { get; set; }
}
