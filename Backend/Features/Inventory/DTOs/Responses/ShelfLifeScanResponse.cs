using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;

public class ShelfLifeScanResponse
{
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public int TotalProductsScanned { get; set; }
    public int ExpiredAndLockedCount { get; set; }
    public int Discounted50PercentCount { get; set; }
    public int Discounted30PercentCount { get; set; }
    public int FlashSaleActivatedCount { get; set; }
    public bool IsDryRun { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ShelfLifeItemDetailResponse> AffectedItems { get; set; } = new();
}

public class ShelfLifeItemDetailResponse
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public int AffectedQuantity { get; set; }
    public string Unit { get; set; } = "Hộp";
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
}
