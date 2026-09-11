using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<Domain.Entities.Inventory> Items, int TotalCount)> GetAllAsync(int branchId, int page, int pageSize, string? statusFilter)
    {
        var query = _dbContext.Inventories
            .Include(i => i.Product)
            .Where(i => i.BranchId == branchId);

        if (!string.IsNullOrEmpty(statusFilter))
        {
            if (statusFilter.Equals("low", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.QuantityOnHand <= i.MinStockLevel);
            else if (statusFilter.Equals("out", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.QuantityOnHand == 0);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.Product.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Domain.Entities.Inventory?> GetByProductAndBranchAsync(int productId, int branchId)
    {
        return await _dbContext.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);
    }

    public async Task<IEnumerable<Domain.Entities.Inventory>> GetLowStockAsync(int branchId, int top)
    {
        return await _dbContext.Inventories
            .Include(i => i.Product)
            .Where(i => i.BranchId == branchId && i.QuantityOnHand <= i.MinStockLevel)
            .OrderBy(i => i.QuantityOnHand)
            .Take(top)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockHistory>> GetExpiringAsync(int branchId, int withinDays)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(withinDays));

        return await _dbContext.StockHistories
            .Include(sh => sh.Product)
            .Where(sh => sh.BranchId == branchId 
                      && sh.ChangeType == StockChangeType.Import 
                      && sh.ExpiryDate.HasValue 
                      && sh.ExpiryDate.Value <= cutoffDate
                      && sh.ExpiryDate.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(sh => sh.ExpiryDate)
            .ToListAsync();
    }

    public async Task<(IEnumerable<StockHistory> Items, int TotalCount)> GetStockHistoryAsync(int productId, int branchId, StockChangeType? changeType, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _dbContext.StockHistories
            .Include(sh => sh.CreatedByUser)
            .Where(sh => sh.ProductId == productId && sh.BranchId == branchId);

        if (changeType.HasValue)
            query = query.Where(sh => sh.ChangeType == changeType.Value);
            
        if (fromDate.HasValue)
            query = query.Where(sh => sh.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            query = query.Where(sh => sh.CreatedAt <= toDate.Value);

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(sh => sh.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task UpsertAsync(Domain.Entities.Inventory inventory)
    {
        var existing = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == inventory.ProductId && i.BranchId == inventory.BranchId);

        if (existing == null)
        {
            await _dbContext.Inventories.AddAsync(inventory);
        }
        else
        {
            existing.QuantityOnHand = inventory.QuantityOnHand;
            existing.LastUpdated = DateTime.UtcNow;
            _dbContext.Inventories.Update(existing);
        }
    }

    public async Task AddStockHistoryAsync(StockHistory stockHistory)
    {
        await _dbContext.StockHistories.AddAsync(stockHistory);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
