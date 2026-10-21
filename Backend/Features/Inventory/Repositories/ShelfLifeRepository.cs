using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public class ShelfLifeRepository : IShelfLifeRepository
{
    private readonly AppDbContext _dbContext;

    public ShelfLifeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// Lấy danh sách các lô hàng cận hạn (Quantity > 0 và ExpiryDate <= thresholdDate)
    public async Task<List<InventoryBatch>> GetBatchesNearExpiryAsync(DateTime thresholdDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .Include(b => b.Product)
            .ThenInclude(p => p.Category)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= thresholdDate)
            .ToListAsync(cancellationToken);
    }

    /// Kiểm tra xem sản phẩm có còn lô nào khác hợp lệ (còn hàng và chưa hết hạn) hay không
    public async Task<bool> HasOtherValidBatchesAsync(int productId, int currentBatchId, DateTime today, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .AnyAsync(b => b.ProductId == productId &&
                           b.BatchId != currentBatchId &&
                           b.Quantity > 0 &&
                           b.ExpiryDate > today, cancellationToken);
    }
    ///Lấy danh sách sản phẩm tươi sống (Rau, Thịt, Bánh) đang Active phục vụ Flash Sale

    public async Task<List<Product>> GetFreshProductsForFlashSaleAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active &&
                        (p.Category.CategoryName.Contains("Rau") ||
                         p.Category.CategoryName.Contains("Thịt") ||
                         p.Category.CategoryName.Contains("Bánh") ||
                         p.Category.CategoryName.Contains("Tươi sống")))
            .ToListAsync(cancellationToken);
    }

    /// Ghi nhật ký biến động kho (Xuất hủy hàng hết hạn)
    public async Task AddStockHistoryAsync(StockHistory stockHistory, CancellationToken cancellationToken = default)
    {
        await _dbContext.StockHistories.AddAsync(stockHistory, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
