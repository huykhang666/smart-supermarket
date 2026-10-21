using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Promotions.DTOs.Responses;

public class PromotionDto
{
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public int? TriggerProductId { get; set; }
    public string? TriggerProductName { get; set; }
    public int? TargetCategoryId { get; set; }
    public string? TargetCategoryName { get; set; }
    public int? TargetProductId { get; set; }
    public string? TargetProductName { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? GiftQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PromotionCartItemDto
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ApplyPromotionResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public List<string> AppliedDetails { get; set; } = new();
}

public class PromotionPagedResult
{
    public IEnumerable<PromotionDto> Items { get; set; } = new List<PromotionDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
