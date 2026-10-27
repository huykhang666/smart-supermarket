using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public interface IProductTool
{
    Task<List<Product>> SearchAsync(string keyword, int? categoryId = null, int limit = 15, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetAlternativeAsync(int originalProductId, decimal maxPrice, CancellationToken cancellationToken = default);
}
