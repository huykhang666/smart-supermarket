using System.Collections.Generic;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class BudgetEvaluationDto
{
    public decimal TargetBudget { get; set; }
    public decimal CurrentTotal { get; set; }
    public decimal Difference { get; set; }
    public bool IsWithinBudget { get; set; }
    public List<MealIngredientItemDto> RebalancedItems { get; set; } = new();
    public string? AdjustmentNote { get; set; }
}
