using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public interface IInventoryService
{
    Task<(IEnumerable<Domain.Entities.Inventory> Items, int TotalCount)> GetInventoryListAsync(int branchId, string? statusFilter, string? search, int page, int pageSize);
    Task<Domain.Entities.Inventory?> GetInventoryByProductAsync(int productId, int branchId);
    Task<IEnumerable<Domain.Entities.Inventory>> GetLowStockProductsAsync(int branchId, int top = 10);
    Task<IEnumerable<StockHistory>> GetExpiringProductsAsync(int branchId, int withinDays = 15);
    Task AdjustStockAsync(int productId, int branchId, int quantityChange, string note, int userId);
    Task<(IEnumerable<StockHistory> Items, int TotalCount)> GetStockHistoryAsync(int productId, int branchId, StockChangeType? changeType, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task AddStockAsync(int productId, int branchId, int quantity, DateOnly? expiryDate, int receiptId, int userId);
    Task DeductStockFEFOAsync(int productId, int branchId, int quantity, int orderId, int userId);
}
