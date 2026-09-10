namespace SmartSupermarket.Backend.Domain.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; } = 0;
    public decimal? MaximumDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
