using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class PromotionTool : IPromotionTool
{
    private readonly IAiPromotionService _promotionService;

    public PromotionTool(IAiPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    public Task<List<Promotion>> GetActiveVouchersAsync(CancellationToken cancellationToken = default)
    {
        return _promotionService.GetActiveVouchersAsync(cancellationToken);
    }

    public Task<List<NearExpiryBatchDto>> CheckFlashSaleAsync(CancellationToken cancellationToken = default)
    {
        return _promotionService.GetFlashSaleItemsAsync(cancellationToken);
    }

    public Task<decimal> CalculateDiscountAsync(List<MealIngredientItemDto> items, string? voucherCode = null, CancellationToken cancellationToken = default)
    {
        return _promotionService.CalculateDiscountAsync(items, voucherCode, cancellationToken);
    }
}
