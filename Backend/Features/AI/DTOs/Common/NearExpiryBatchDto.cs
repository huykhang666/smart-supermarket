using System;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class NearExpiryBatchDto
{
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal FlashSalePrice { get; set; }
}
