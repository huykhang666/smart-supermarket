using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Domain.Entities;

public class StockHistory
{
    public int StockHistoryId { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public StockChangeType ChangeType { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public int? ReferenceId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
