using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Promotions.DTOs.Requests;

public class CreatePromotionRequest
{
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; } = 0;
    public decimal? MaximumDiscountAmount { get; set; }
    public int? TriggerProductId { get; set; }
    public int? TargetCategoryId { get; set; }
    public int? TargetProductId { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? GiftQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePromotionRequest
{
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public int? TriggerProductId { get; set; }
    public int? TargetCategoryId { get; set; }
    public int? TargetProductId { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? GiftQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class ApplyPromotionRequest
{
    public string PromotionCode { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public List<PromotionCartItemDto>? Items { get; set; }
}
