using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public interface IInventoryRepository
{
    Task<(IEnumerable<Domain.Entities.Inventory> Items, int TotalCount)> GetAllAsync(int branchId, int page, int pageSize, string? statusFilter);
    Task<Domain.Entities.Inventory?> GetByProductAndBranchAsync(int productId, int branchId);
    Task<IEnumerable<Domain.Entities.Inventory>> GetLowStockAsync(int branchId, int top);
    Task<IEnumerable<StockHistory>> GetExpiringAsync(int branchId, int withinDays);
    Task<(IEnumerable<StockHistory> Items, int TotalCount)> GetStockHistoryAsync(int productId, int branchId, StockChangeType? changeType, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task UpsertAsync(Domain.Entities.Inventory inventory);
    Task AddStockHistoryAsync(StockHistory stockHistory);
    Task SaveChangesAsync();
}
