using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class InventoryTool : IInventoryTool
{
    private readonly IAiInventoryService _inventoryService;

    public InventoryTool(IAiInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<int> GetStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _inventoryService.GetStockAsync(productId, cancellationToken);
    }

    public Task<List<NearExpiryBatchDto>> GetNearExpiryBatchesAsync(int daysThreshold = 3, CancellationToken cancellationToken = default)
    {
        return _inventoryService.GetNearExpiryBatchesAsync(daysThreshold, cancellationToken);
    }

    public Task<List<InventoryBatch>> GetBatchesByProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _inventoryService.GetBatchesByProductAsync(productId, cancellationToken);
    }

    public Task<Product?> GetAlternativeProductAsync(int outOfStockProductId, CancellationToken cancellationToken = default)
    {
        return _inventoryService.GetAlternativeProductAsync(outOfStockProductId, cancellationToken);
    }
}
