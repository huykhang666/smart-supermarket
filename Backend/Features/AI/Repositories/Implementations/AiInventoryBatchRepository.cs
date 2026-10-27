using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Repositories.Implementations;

public class AiInventoryBatchRepository : IAiInventoryBatchRepository
{
    private readonly AppDbContext _context;

    public AiInventoryBatchRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetAvailableStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        var batchStock = await _context.InventoryBatches
            .AsNoTracking()
            .Where(b => b.ProductId == productId && b.Quantity > 0)
            .SumAsync(b => b.Quantity, cancellationToken);

        if (batchStock > 0)
        {
            return batchStock;
        }

        return await _context.Inventories
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .SumAsync(i => i.QuantityOnHand, cancellationToken);
    }

    public async Task<List<InventoryBatch>> GetNearExpiryBatchesAsync(int daysThreshold, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var thresholdDate = now.AddDays(daysThreshold);

        return await _context.InventoryBatches
            .AsNoTracking()
            .Include(b => b.Product)
            .Where(b => b.Quantity > 0 && b.ExpiryDate > now && b.ExpiryDate <= thresholdDate)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InventoryBatch>> GetBatchesByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryBatches
            .AsNoTracking()
            .Where(b => b.ProductId == productId && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(cancellationToken);
    }
}
