using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs;

#region 1. DTO Phân tích Rủi ro Sản phẩm (Cận date, Cháy hàng, Bán ế)
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
    public string RiskType { get; set; } = string.Empty; // "NEAR_EXPIRY", "OUT_OF_STOCK_RISK", "SLOW_MOVING", "NEGATIVE_MARGIN"
    public string UrgencyLevel { get; set; } = "MEDIUM"; // "CRITICAL", "HIGH", "MEDIUM", "LOW"
    public string AiAnalysisReasoning { get; set; } = string.Empty;
    public decimal SuggestedDiscountPercent { get; set; } // Ví dụ: 15%
    public decimal SuggestedNewPrice { get; set; }
    public string RecommendedAction { get; set; } = string.Empty; // "APPLY_MARKDOWN", "CREATE_RESTOCK_ORDER", "BUNDLE_PROMOTION"
}
#endregion

#region 2. DTO Phân tích Rổ hàng & Bán kèm (Market Basket Analysis)
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
    public double CorrelationScore { get; set; } // Support / Confidence / Lift
    public string SuggestedComboName { get; set; } = string.Empty; // Vd: "Combo Bữa xế tiện lợi"
    public decimal ComboPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string PromotionalReason { get; set; } = string.Empty;
}
#endregion

#region 3. DTO Tạo Phiếu Nhập Hàng Nháp 1-Click (Draft Import Receipt)
public class AiDraftImportOrder
{
    public int SuggestedSupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string OrderNote { get; set; } = string.Empty;
    public List<DraftImportOrderItem> Items { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
}

public class DraftImportOrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int RecommendedImportQuantity { get; set; }
    public decimal ExpectedCostPrice { get; set; }
    public decimal SubTotalCost => RecommendedImportQuantity * ExpectedCostPrice;
    public string UrgencyReason { get; set; } = string.Empty;
}
#endregion
