namespace SmartSupermarket.Backend.Domain.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage"; // "Percentage", "FixedAmount", "BuyXGetY", "CategoryDiscount", "QuantityTier"
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; } = 0;
    public decimal? MaximumDiscountAmount { get; set; }

    // Diverse Promotion extensions (SCRUM-23: Mua kèm / Combo, Mua X tặng Y)
    public int? TriggerProductId { get; set; } // E.g. Mua Bàn chải (ID A)
    public int? TargetProductId { get; set; }  // E.g. Giảm Kem đánh răng (ID B)
    public int? TargetCategoryId { get; set; }
    public int? RequiredQuantity { get; set; } // E.g. Buy X (e.g. 2)
    public int? GiftQuantity { get; set; }     // E.g. Get Y free (e.g. 1)

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
