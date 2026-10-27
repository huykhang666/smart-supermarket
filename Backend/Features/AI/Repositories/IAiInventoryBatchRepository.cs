using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Repositories;

public interface IAiInventoryBatchRepository
{
    Task<int> GetAvailableStockAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<List<InventoryBatch>> GetNearExpiryBatchesAsync(
        int daysThreshold,
        CancellationToken cancellationToken = default);

    Task<List<InventoryBatch>> GetBatchesByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default);
}
