using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiPromotionService : IAiPromotionService
{
    private readonly IAiPromotionRepository _promotionRepository;
    private readonly IAiInventoryService _inventoryService;

    public AiPromotionService(
        IAiPromotionRepository promotionRepository,
        IAiInventoryService inventoryService)
    {
        _promotionRepository = promotionRepository;
        _inventoryService = inventoryService;
    }

    public async Task<List<Promotion>> GetActiveVouchersAsync(CancellationToken cancellationToken = default)
    {
        return await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
    }

    public async Task<List<NearExpiryBatchDto>> GetFlashSaleItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _inventoryService.GetNearExpiryBatchesAsync(3, cancellationToken);
    }

    public async Task<decimal> CalculateDiscountAsync(
        List<MealIngredientItemDto> items,
        string? voucherCode = null,
        CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0) return 0m;

        decimal totalDiscount = 0m;

        // Flash sale chiết khấu từng món
        foreach (var item in items)
        {
            if (item.DiscountPercent > 0)
            {
                var lineDiscount = item.RegularPrice * (item.DiscountPercent / 100m) * item.QuantityToBuy;
                totalDiscount += lineDiscount;
            }
        }

        // Voucher ngành hàng (Ví dụ: FRESH20 - Giảm 20k cho đơn hàng >= 100k)
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var cleanCode = voucherCode.Trim().ToUpperInvariant();
            if (cleanCode == "FRESH20")
            {
                var freshTotal = items
                    .Where(i => i.FinalPrice > 0)
                    .Sum(i => i.FinalPrice * i.QuantityToBuy);

                if (freshTotal >= 100000m)
                {
                    totalDiscount += 20000m;
                }
            }
            else
            {
                var promo = await _promotionRepository.GetPromotionByCodeAsync(cleanCode, cancellationToken);
                if (promo != null)
                {
                    var subtotal = items.Sum(i => i.FinalPrice * i.QuantityToBuy);
                    if (promo.DiscountType == "Percentage" && promo.DiscountValue > 0)
                    {
                        var discount = subtotal * (promo.DiscountValue / 100m);
                        if (promo.MaximumDiscountAmount.HasValue && discount > promo.MaximumDiscountAmount.Value)
                        {
                            discount = promo.MaximumDiscountAmount.Value;
                        }
                        totalDiscount += discount;
                    }
                    else if (promo.DiscountType == "FixedAmount" && promo.DiscountValue > 0)
                    {
                        totalDiscount += promo.DiscountValue;
                    }
                }
            }
        }

        return totalDiscount;
    }
}
