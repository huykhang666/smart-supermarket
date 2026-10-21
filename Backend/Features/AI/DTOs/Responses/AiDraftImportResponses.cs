using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

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
