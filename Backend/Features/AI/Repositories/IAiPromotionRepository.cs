using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Repositories;

public interface IAiPromotionRepository
{
    Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);

    Task<Promotion?> GetPromotionByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<DiscountRule>> GetActiveDiscountRulesAsync(CancellationToken cancellationToken = default);
}
