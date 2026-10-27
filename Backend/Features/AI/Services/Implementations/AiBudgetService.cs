using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiBudgetService : IAiBudgetService
{
    private readonly IAiProductService _productService;

    public AiBudgetService(IAiProductService productService)
    {
        _productService = productService;
    }

    public async Task<BudgetEvaluationDto> EvaluateBudgetAsync(
        decimal targetBudget,
        List<MealIngredientItemDto> items,
        CancellationToken cancellationToken = default)
    {
        items ??= new List<MealIngredientItemDto>();
        decimal currentTotal = items.Sum(i => i.FinalPrice * i.QuantityToBuy);
        decimal difference = currentTotal - targetBudget;
        bool isWithinBudget = difference <= targetBudget * 0.1m;

        var evaluation = new BudgetEvaluationDto
        {
            TargetBudget = targetBudget,
            CurrentTotal = currentTotal,
            Difference = difference,
            IsWithinBudget = isWithinBudget,
            RebalancedItems = new List<MealIngredientItemDto>(items)
        };

        if (!isWithinBudget && items.Count > 0)
        {
            var mostExpensive = items.OrderByDescending(i => i.FinalPrice).First();
            var maxAffordablePrice = mostExpensive.FinalPrice - difference;

            if (maxAffordablePrice > 0)
            {
                var cheaper = await ProposeCheaperAlternativeAsync(mostExpensive.ProductId, maxAffordablePrice, cancellationToken);
                if (cheaper != null)
                {
                    evaluation.RebalancedItems.Remove(mostExpensive);
                    evaluation.RebalancedItems.Add(cheaper);
                    evaluation.AdjustmentNote = $"Đã thay thế '{mostExpensive.ProductName}' bằng '{cheaper.ProductName}' để tiết kiệm {mostExpensive.FinalPrice - cheaper.FinalPrice:N0} đ.";
                    evaluation.CurrentTotal = evaluation.RebalancedItems.Sum(i => i.FinalPrice * i.QuantityToBuy);
                    evaluation.Difference = evaluation.CurrentTotal - targetBudget;
                    evaluation.IsWithinBudget = true;
                }
            }
        }

        return evaluation;
    }

    public async Task<MealIngredientItemDto?> ProposeCheaperAlternativeAsync(
        int currentProductId,
        decimal targetPrice,
        CancellationToken cancellationToken = default)
    {
        var alt = await _productService.GetAlternativeProductAsync(currentProductId, targetPrice, cancellationToken);
        if (alt == null) return null;

        return new MealIngredientItemDto
        {
            ProductId = alt.ProductId,
            ProductName = alt.ProductName,
            Barcode = alt.Barcode ?? string.Empty,
            ImageUrl = alt.ImageUrl ?? string.Empty,
            Unit = alt.Unit ?? "phần",
            QuantityToBuy = 1,
            RegularPrice = alt.Price,
            FinalPrice = alt.Price,
            DiscountPercent = 0,
            InStockQuantity = 10
        };
    }
}
