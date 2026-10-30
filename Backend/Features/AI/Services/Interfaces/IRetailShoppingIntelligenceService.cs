using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IRetailShoppingIntelligenceService
{
    Task<RetailShoppingResultDto> AdviseRetailShoppingAsync(
        string prompt,
        decimal? targetBudget = null,
        string? preferredTier = null,
        CancellationToken cancellationToken = default);

    Task<List<RetailProductRecommendationDto>> GetRecommendationsByCategoryAsync(
        string category,
        decimal? targetBudget = null,
        RetailTier? preferredTier = null,
        CancellationToken cancellationToken = default);

    Task<RetailComparisonTableDto> GenerateComparisonTableAsync(
        string category,
        CancellationToken cancellationToken = default);
}
