using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IAiPromotionService
{
    Task<List<Promotion>> GetActiveVouchersAsync(CancellationToken cancellationToken = default);
    Task<List<NearExpiryBatchDto>> GetFlashSaleItemsAsync(CancellationToken cancellationToken = default);
    Task<decimal> CalculateDiscountAsync(List<MealIngredientItemDto> items, string? voucherCode = null, CancellationToken cancellationToken = default);
}
