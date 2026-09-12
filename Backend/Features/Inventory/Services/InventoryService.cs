using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Inventory.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly AppDbContext _dbContext;

    public InventoryService(IInventoryRepository inventoryRepository, AppDbContext dbContext)
    {
        _inventoryRepository = inventoryRepository;
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<Domain.Entities.Inventory> Items, int TotalCount)> GetInventoryListAsync(int branchId, string? statusFilter, string? search, int page, int pageSize)
    {
        // Simple search logic combined with repo
        return await _inventoryRepository.GetAllAsync(branchId, page, pageSize, statusFilter);
    }

    public async Task<Domain.Entities.Inventory?> GetInventoryByProductAsync(int productId, int branchId)
    {
        return await _inventoryRepository.GetByProductAndBranchAsync(productId, branchId);
    }

    public async Task<IEnumerable<Domain.Entities.Inventory>> GetLowStockProductsAsync(int branchId, int top = 10)
    {
        return await _inventoryRepository.GetLowStockAsync(branchId, top);
    }

    public async Task<IEnumerable<StockHistory>> GetExpiringProductsAsync(int branchId, int withinDays = 15)
    {
        return await _inventoryRepository.GetExpiringAsync(branchId, withinDays);
    }

    public async Task AdjustStockAsync(int productId, int branchId, int quantityChange, string note, int userId)
    {
        if (quantityChange == 0)
            throw new ArgumentException("Số lượng điều chỉnh phải khác 0.");

        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Lý do điều chỉnh không được để trống.");

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var inventory = await _inventoryRepository.GetByProductAndBranchAsync(productId, branchId) 
                            ?? new Domain.Entities.Inventory { ProductId = productId, BranchId = branchId, QuantityOnHand = 0 };

            int quantityBefore = inventory.QuantityOnHand;
            int quantityAfter = quantityBefore + quantityChange;

            if (quantityAfter < 0)
                throw new InvalidOperationException("Số lượng tồn kho sau khi điều chỉnh không thể âm.");

            inventory.QuantityOnHand = quantityAfter;
            inventory.LastUpdated = DateTime.UtcNow;

            await _inventoryRepository.UpsertAsync(inventory);

            var history = new StockHistory
            {
                ProductId = productId,
                BranchId = branchId,
                ChangeType = StockChangeType.Adjustment,
                QuantityChange = quantityChange,
                QuantityBefore = quantityBefore,
                QuantityAfter = quantityAfter,
                Note = note,
                CreatedByUserId = userId
            };

            await _inventoryRepository.AddStockHistoryAsync(history);
            await _inventoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(IEnumerable<StockHistory> Items, int TotalCount)> GetStockHistoryAsync(int productId, int branchId, StockChangeType? changeType, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        return await _inventoryRepository.GetStockHistoryAsync(productId, branchId, changeType, fromDate, toDate, page, pageSize);
    }

    public async Task AddStockAsync(int productId, int branchId, int quantity, DateOnly? expiryDate, int receiptId, int userId)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng nhập phải lớn hơn 0");

        var inventory = await _inventoryRepository.GetByProductAndBranchAsync(productId, branchId)
                        ?? new Domain.Entities.Inventory { ProductId = productId, BranchId = branchId, QuantityOnHand = 0 };

        int quantityBefore = inventory.QuantityOnHand;
        inventory.QuantityOnHand += quantity;
        inventory.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpsertAsync(inventory);

        var history = new StockHistory
        {
            ProductId = productId,
            BranchId = branchId,
            ChangeType = StockChangeType.Import,
            QuantityChange = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = inventory.QuantityOnHand,
            ExpiryDate = expiryDate,
            ReferenceId = receiptId,
            Note = $"Nhập hàng từ phiếu nhập {receiptId}",
            CreatedByUserId = userId
        };

        await _inventoryRepository.AddStockHistoryAsync(history);
        await _inventoryRepository.SaveChangesAsync();
    }

    public async Task DeductStockFEFOAsync(int productId, int branchId, int quantity, int orderId, int userId)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng xuất phải lớn hơn 0");

        var inventory = await _inventoryRepository.GetByProductAndBranchAsync(productId, branchId);
        if (inventory == null || inventory.QuantityOnHand < quantity)
            throw new InvalidOperationException("Kho không đủ số lượng xuất hàng.");

        // FEFO Logic
        var activeBatches = await _dbContext.StockHistories
            .Where(sh => sh.ProductId == productId && sh.BranchId == branchId 
                      && sh.ChangeType == StockChangeType.Import 
                      && (!sh.ExpiryDate.HasValue || sh.ExpiryDate > DateOnly.FromDateTime(DateTime.UtcNow)))
            .OrderBy(sh => sh.ExpiryDate) // FEFO
            .ThenBy(sh => sh.CreatedAt)   // FIFO fallback
            .ToListAsync();

        int quantityToDeduct = quantity;
        int currentTotalStock = inventory.QuantityOnHand;

        foreach (var batch in activeBatches)
        {
            if (quantityToDeduct == 0) break;

            // In a real scenario, we'd track "RemainingQuantity" per batch. 
            // For this implementation, we deduct directly from the overall inventory 
            // and record the deduction, assuming we just log which batch was theoretically used.
            int deductFromBatch = Math.Min(quantityToDeduct, batch.QuantityChange); // Approximated
            quantityToDeduct -= deductFromBatch;

            var history = new StockHistory
            {
                ProductId = productId,
                BranchId = branchId,
                ChangeType = StockChangeType.Sale,
                QuantityChange = -deductFromBatch,
                QuantityBefore = currentTotalStock,
                QuantityAfter = currentTotalStock - deductFromBatch,
                ReferenceId = orderId,
                Note = $"Xuất bán từ lô nhập {batch.StockHistoryId} (FEFO)",
                CreatedByUserId = userId
            };

            currentTotalStock -= deductFromBatch;
            await _inventoryRepository.AddStockHistoryAsync(history);
        }

        inventory.QuantityOnHand -= quantity;
        inventory.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpsertAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();
    }
}
