using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public int EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public int BranchId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public int? VoucherId { get; set; }
    public decimal FinalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Completed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Employee { get; set; }
    public Customer? Customer { get; set; }
    public List<OrderDetail> OrderDetails { get; set; } = new();
    public List<OrderPromotion> OrderPromotions { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}