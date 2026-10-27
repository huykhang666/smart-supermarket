using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Context;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiCartService : IAiCartService
{
    private readonly IConversationContextService _contextService;
    private readonly IAiPromotionService _promotionService;

    public AiCartService(
        IConversationContextService contextService,
        IAiPromotionService promotionService)
    {
        _contextService = contextService;
        _promotionService = promotionService;
    }

    public async Task<bool> AddToCartAsync(
        string sessionId,
        List<MealIngredientItemDto> items,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || items == null || items.Count == 0)
        {
            return false;
        }

        var context = await _contextService.GetOrCreateContextAsync(sessionId, null, cancellationToken);
        context.CurrentCartItemProductIds ??= new List<int>();

        foreach (var item in items)
        {
            if (!context.CurrentCartItemProductIds.Contains(item.ProductId))
            {
                context.CurrentCartItemProductIds.Add(item.ProductId);
            }
        }

        await _contextService.SaveContextAsync(context, cancellationToken);
        return true;
    }

    public async Task<CartEstimationDto> EstimateCartTotalAsync(
        List<MealIngredientItemDto> items,
        string? voucherCode = null,
        CancellationToken cancellationToken = default)
    {
        items ??= new List<MealIngredientItemDto>();
        decimal subTotal = items.Sum(i => i.RegularPrice * i.QuantityToBuy);
        decimal discountAmount = await _promotionService.CalculateDiscountAsync(items, voucherCode, cancellationToken);
        decimal finalTotal = Math.Max(0, subTotal - discountAmount);

        return new CartEstimationDto
        {
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            FinalTotal = finalTotal,
            TotalItemCount = items.Sum(i => i.QuantityToBuy),
            Items = items,
            AppliedVoucherCode = voucherCode
        };
    }
}
