using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class AiProductRiskReport
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public int TotalScannedItems { get; set; }
    public int CriticalIssuesCount { get; set; }
    public List<ProductRiskItem> HighRiskItems { get; set; } = new();
    public List<ProductRiskItem> ModerateRiskItems { get; set; } = new();
    public string SummaryStrategy { get; set; } = string.Empty;
}

public class ProductRiskItem
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public decimal AverageSalesPerDay { get; set; }
    public string RiskType { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = "MEDIUM";
    public string AiAnalysisReasoning { get; set; } = string.Empty;
    public decimal SuggestedDiscountPercent { get; set; }
    public decimal SuggestedNewPrice { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}
