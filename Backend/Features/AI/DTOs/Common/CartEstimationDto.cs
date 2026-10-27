using System.Collections.Generic;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class CartEstimationDto
{
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
    public int TotalItemCount { get; set; }
    public List<MealIngredientItemDto> Items { get; set; } = new();
    public string? AppliedVoucherCode { get; set; }
}
