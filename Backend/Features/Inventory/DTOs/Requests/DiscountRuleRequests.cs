namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;

public class CreateDiscountRuleRequest
{
    public int DaysBeforeExpiry { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateDiscountRuleRequest
{
    public int DaysBeforeExpiry { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
