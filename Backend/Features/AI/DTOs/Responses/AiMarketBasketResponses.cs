using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class AiMarketBasketReport
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public int AnalyzedOrdersCount { get; set; }
    public List<MarketBasketPairItem> TopCombinations { get; set; } = new();
    public string GeneralRecommendations { get; set; } = string.Empty;
}

public class MarketBasketPairItem
{
    public int ProductAId { get; set; }
    public string ProductAName { get; set; } = string.Empty;
    public int ProductBId { get; set; }
    public string ProductBName { get; set; } = string.Empty;
    public int PairOrderFrequency { get; set; }
    public double CorrelationScore { get; set; }
    public string SuggestedComboName { get; set; } = string.Empty;
    public decimal ComboPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string PromotionalReason { get; set; } = string.Empty;
}
