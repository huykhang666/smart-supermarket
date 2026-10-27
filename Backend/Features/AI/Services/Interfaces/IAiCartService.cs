using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IAiCartService
{
    Task<bool> AddToCartAsync(string sessionId, List<MealIngredientItemDto> items, CancellationToken cancellationToken = default);
    Task<CartEstimationDto> EstimateCartTotalAsync(List<MealIngredientItemDto> items, string? voucherCode = null, CancellationToken cancellationToken = default);
}
