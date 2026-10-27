using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class CartTool : ICartTool
{
    private readonly IAiCartService _cartService;

    public CartTool(IAiCartService cartService)
    {
        _cartService = cartService;
    }

    public Task<bool> AddToCartAsync(string sessionId, List<MealIngredientItemDto> items, CancellationToken cancellationToken = default)
    {
        return _cartService.AddToCartAsync(sessionId, items, cancellationToken);
    }

    public Task<CartEstimationDto> EstimateCartTotalAsync(List<MealIngredientItemDto> items, string? voucherCode = null, CancellationToken cancellationToken = default)
    {
        return _cartService.EstimateCartTotalAsync(items, voucherCode, cancellationToken);
    }
}
