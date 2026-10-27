using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public interface IBudgetTool
{
    Task<BudgetEvaluationDto> EvaluateBudgetAsync(decimal targetBudget, List<MealIngredientItemDto> items, CancellationToken cancellationToken = default);
    Task<MealIngredientItemDto?> ProposeCheaperAlternativeAsync(int currentProductId, decimal targetPrice, CancellationToken cancellationToken = default);
}
