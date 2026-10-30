using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public enum RetailTier
{
    Tier1CheapestAcceptable = 1,
    Tier2BalancedValue = 2,
    Tier3DurableHighPerformance = 3,
    Tier4PremiumLuxury = 4
}

public class RetailProductRecommendationDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RegularPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int InStockQuantity { get; set; }
    public RetailTier Tier { get; set; } = RetailTier.Tier2BalancedValue;
    public string TierLabel { get; set; } = string.Empty;
    public string HighlightReason { get; set; } = string.Empty;
    public string PricePerUnitMetric { get; set; } = string.Empty;
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
    public bool IsRecommendedChoice { get; set; }
    public bool IsWithinBudget { get; set; } = true;
    public string CategoryName { get; set; } = string.Empty;
}

public class ComparisonRowDto
{
    public string Criteria { get; set; } = string.Empty;
    public string Tier1CheapValue { get; set; } = string.Empty;
    public string Tier2StandardValue { get; set; } = string.Empty;
    public string Tier3PremiumValue { get; set; } = string.Empty;
}

public class RetailComparisonTableDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<ComparisonRowDto> Rows { get; set; } = new();
}

public class RetailShoppingResultDto
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string ShoppingCategory { get; set; } = string.Empty;
    public string UseCase { get; set; } = string.Empty;
    public List<RetailProductRecommendationDto> Recommendations { get; set; } = new();
    public RetailComparisonTableDto? ComparisonTable { get; set; }
}
