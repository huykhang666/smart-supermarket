using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public interface IShelfLifeRepository
{
    Task<List<InventoryBatch>> GetBatchesNearExpiryAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);

    Task<bool> HasOtherValidBatchesAsync(int productId, int currentBatchId, DateTime today, CancellationToken cancellationToken = default);

    Task<List<Product>> GetFreshProductsForFlashSaleAsync(CancellationToken cancellationToken = default);

    Task AddStockHistoryAsync(StockHistory stockHistory, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
