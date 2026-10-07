namespace SmartSupermarket.Backend.Domain.Entities;

public class PointHistory
{
    public int PointHistoryId { get; set; }
    public int CustomerId { get; set; }
    public int? OrderId { get; set; }
    public int PointChange { get; set; }
    public byte Type { get; set; } = 1; // 1 = Tích lũy điểm, 3 = Thu hồi / Điều chỉnh điểm
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer? Customer { get; set; }
    public Order? Order { get; set; }
}
