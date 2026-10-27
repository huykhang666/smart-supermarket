using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Repositories;

public interface IAiProductRepository
{
    Task<List<Product>> SearchProductsAsync(
        string keyword,
        int? categoryId = null,
        int limit = 15,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetProductsByCategoryAsync(
        int categoryId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<Product?> GetAlternativeProductAsync(
        int originalProductId,
        int categoryId,
        decimal maxPrice,
        CancellationToken cancellationToken = default);
}
