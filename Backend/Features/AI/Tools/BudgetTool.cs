using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class BudgetTool : IBudgetTool
{
    private readonly IAiBudgetService _budgetService;

    public BudgetTool(IAiBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    public Task<BudgetEvaluationDto> EvaluateBudgetAsync(decimal targetBudget, List<MealIngredientItemDto> items, CancellationToken cancellationToken = default)
    {
        return _budgetService.EvaluateBudgetAsync(targetBudget, items, cancellationToken);
    }

    public Task<MealIngredientItemDto?> ProposeCheaperAlternativeAsync(int currentProductId, decimal targetPrice, CancellationToken cancellationToken = default)
    {
        return _budgetService.ProposeCheaperAlternativeAsync(currentProductId, targetPrice, cancellationToken);
    }
}
