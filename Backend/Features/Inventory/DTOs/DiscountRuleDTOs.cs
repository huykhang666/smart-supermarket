using System;

namespace SmartSupermarket.Backend.Features.Inventory.DTOs;

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

public class ExpiringProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal ClearancePrice { get; set; }
    public string AlertLevel { get; set; } = "Notice"; // "Critical", "Warning", "Notice", "Expired"
}

public class CheckProductExpiryDiscountResponse
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal ClearancePrice { get; set; }
    public bool IsNearExpiry { get; set; }
    public string RuleDescription { get; set; } = string.Empty;
}
