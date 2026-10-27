using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IAiProductService
{
    Task<List<Product>> SearchProductsAsync(string keyword, int? categoryId = null, int limit = 15, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetAlternativeProductAsync(int originalProductId, decimal maxPrice, CancellationToken cancellationToken = default);
}
