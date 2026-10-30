using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public interface IRetailTool
{
    Task<List<Product>> SearchRetailProductsAsync(
        string keyword,
        string? category = null,
        decimal? maxPrice = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<RetailComparisonTableDto> GenerateCategoryComparisonAsync(
        string category,
        CancellationToken cancellationToken = default);
}
