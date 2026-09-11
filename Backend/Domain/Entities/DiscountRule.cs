namespace SmartSupermarket.Backend.Domain.Entities;

public class DiscountRule
{
    public int DiscountRuleId { get; set; }
    public int DaysBeforeExpiry { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
