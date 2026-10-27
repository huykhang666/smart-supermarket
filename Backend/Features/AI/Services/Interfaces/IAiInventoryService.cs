using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IAiInventoryService
{
    Task<int> GetStockAsync(int productId, CancellationToken cancellationToken = default);
    Task<List<NearExpiryBatchDto>> GetNearExpiryBatchesAsync(int daysThreshold = 3, CancellationToken cancellationToken = default);
    Task<List<InventoryBatch>> GetBatchesByProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetAlternativeProductAsync(int outOfStockProductId, CancellationToken cancellationToken = default);
}
