namespace SmartSupermarket.Backend.Domain.Entities;

public class OrderPromotion
{
    public int OrderPromotionId { get; set; }
    public int OrderId { get; set; }
    public int PromotionId { get; set; }
    public decimal DiscountAmount { get; set; }

    public Order Order { get; set; } = null!;
    public Promotion Promotion { get; set; } = null!;
}