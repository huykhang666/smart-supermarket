using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public class ShelfLifeProcessor : IShelfLifeProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ShelfLifeProcessor> _logger;

    public ShelfLifeProcessor(AppDbContext dbContext, ILogger<ShelfLifeProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ShelfLifeScanResponse> ExecuteShelfLifeAndPricingScanAsync(
        ShelfLifeScanRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("-- Quét dữ liệu ---(FlashSale: {IsFlash}, DryRun: {DryRun})", 
            request.IsEveningFlashSale, request.DryRun);

        var now = DateTime.UtcNow;
        var today = now.Date;

        var response = new ShelfLifeScanResponse
        {
            ScannedAt = now,
            IsDryRun = request.DryRun,
            AffectedItems = new List<ShelfLifeItemDetailResponse>()
        };

        // Quét hạn dùng và phân loại theo 3 bậc FEFO
        var thresholdDate = today.AddDays(3);
        var batches = await _dbContext.InventoryBatches
            .Include(b => b.Product)
            .ThenInclude(p => p.Category)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= thresholdDate)
            .ToListAsync(cancellationToken);

        response.TotalProductsScanned = batches.Count;

        foreach (var batch in batches)
        {
            if (batch.Product == null) continue;

            var product = batch.Product;
            int daysRemaining = (batch.ExpiryDate.Date - today).Days;

            // Bậc 1: Quá hạn hôm nay -> Khóa bán và tạo xuất hủy
            if (daysRemaining <= 0)
            {
                if (!request.DryRun)
                {
                    batch.Status = "Expired";

                    // Ghi lịch sử xuất hủy
                    _dbContext.StockHistories.Add(new StockHistory
                    {
                        ProductId = product.ProductId,
                        BranchId = request.BranchId,
                        ChangeType = StockChangeType.Expired,
                        QuantityChange = -batch.Quantity,
                        QuantityBefore = batch.Quantity,
                        QuantityAfter = 0,
                        ExpiryDate = DateOnly.FromDateTime(batch.ExpiryDate),
                        Note = $"[Tự động] Khóa bán lô {batch.BatchCode} hết hạn ngày {batch.ExpiryDate:dd/MM/yyyy}",
                        CreatedAt = now,
                        CreatedByUserId = 1
                    });

                    // Kiểm tra còn lô nào khác còn hạn hay không
                    bool hasOtherBatches = await _dbContext.InventoryBatches
                        .AnyAsync(b => b.ProductId == product.ProductId &&
                                       b.BatchId != batch.BatchId &&
                                       b.Quantity > 0 &&
                                       b.ExpiryDate > today, cancellationToken);

                    if (!hasOtherBatches)
                    {
                        product.Status = ProductStatus.Inactive;
                        product.UpdatedAt = now;
                    }
                }

                response.ExpiredAndLockedCount++;
                response.AffectedItems.Add(new ShelfLifeItemDetailResponse
                {
                    ProductId = product.ProductId,
                    Barcode = product.Barcode,
                    ProductName = product.ProductName,
                    CategoryName = product.Category?.CategoryName ?? "Chưa phân loại",
                    BatchCode = batch.BatchCode,
                    AffectedQuantity = batch.Quantity,
                    Unit = product.Unit,
                    ManufacturingDate = batch.ManufacturingDate,
                    StorageLocation = batch.StorageLocation,
                    OriginalPrice = product.Price,
                    NewPrice = 0,
                    DiscountPercent = 100,
                    ExpiryDate = batch.ExpiryDate,
                    DaysRemaining = daysRemaining,
                    ActionTaken = "LOCK_EXPIRED"
                });

                _logger.LogWarning("Khóa bán lô hết hạn: {Name} (Lô: {Batch})", product.ProductName, batch.BatchCode);
            }
            // Bậc 2: Còn <= 1 ngày -> Giảm 50%
            else if (daysRemaining <= 1)
            {
                decimal newPrice = Math.Round(product.Price * 0.5m, 0);

                response.Discounted50PercentCount++;
                response.AffectedItems.Add(new ShelfLifeItemDetailResponse
                {
                    ProductId = product.ProductId,
                    Barcode = product.Barcode,
                    ProductName = product.ProductName,
                    CategoryName = product.Category?.CategoryName ?? "Chưa phân loại",
                    BatchCode = batch.BatchCode,
                    AffectedQuantity = batch.Quantity,
                    Unit = product.Unit,
                    ManufacturingDate = batch.ManufacturingDate,
                    StorageLocation = batch.StorageLocation,
                    OriginalPrice = product.Price,
                    NewPrice = newPrice,
                    DiscountPercent = 50,
                    ExpiryDate = batch.ExpiryDate,
                    DaysRemaining = daysRemaining,
                    ActionTaken = "DISCOUNT_50"
                });
            }
            // Bậc 3: Còn 2 - 3 ngày -> Giảm 30%
            else if (daysRemaining <= 3)
            {
                decimal newPrice = Math.Round(product.Price * 0.7m, 0);

                response.Discounted30PercentCount++;
                response.AffectedItems.Add(new ShelfLifeItemDetailResponse
                {
                    ProductId = product.ProductId,
                    Barcode = product.Barcode,
                    ProductName = product.ProductName,
                    CategoryName = product.Category?.CategoryName ?? "Chưa phân loại",
                    BatchCode = batch.BatchCode,
                    AffectedQuantity = batch.Quantity,
                    Unit = product.Unit,
                    ManufacturingDate = batch.ManufacturingDate,
                    StorageLocation = batch.StorageLocation,
                    OriginalPrice = product.Price,
                    NewPrice = newPrice,
                    DiscountPercent = 30,
                    ExpiryDate = batch.ExpiryDate,
                    DaysRemaining = daysRemaining,
                    ActionTaken = "DISCOUNT_30"
                });
            }
        }

        // Khâu 3: Flash Sale sau 18h xả hàng tươi sống
        if (request.IsEveningFlashSale)
        {
            var freshProducts = await _dbContext.Products
                .Include(p => p.Category)
                .Where(p => p.Status == ProductStatus.Active &&
                            (p.Category.CategoryName.Contains("Rau") ||
                             p.Category.CategoryName.Contains("Thịt") ||
                             p.Category.CategoryName.Contains("Bánh") ||
                             p.Category.CategoryName.Contains("Tươi sống")))
                .ToListAsync(cancellationToken);

            foreach (var item in freshProducts)
            {
                if (response.AffectedItems.Any(a => a.ProductId == item.ProductId)) continue;

                decimal flashPrice = Math.Round(item.Price * 0.8m, 0); // Giảm 20%
                response.FlashSaleActivatedCount++;
                response.AffectedItems.Add(new ShelfLifeItemDetailResponse
                {
                    ProductId = item.ProductId,
                    Barcode = item.Barcode,
                    ProductName = item.ProductName,
                    CategoryName = item.Category?.CategoryName ?? "Tươi sống",
                    BatchCode = "FLASH-SALE",
                    AffectedQuantity = 10,
                    Unit = item.Unit,
                    ManufacturingDate = item.ManufacturingDate,
                    StorageLocation = "Khu tươi sống",
                    OriginalPrice = item.Price,
                    NewPrice = flashPrice,
                    DiscountPercent = 20,
                    ExpiryDate = item.ExpiryDate,
                    DaysRemaining = null,
                    ActionTaken = "FLASH_SALE_EVENING"
                });
            }
        }

        // Khâu 4: Lưu thay đổi nếu không phải DryRun
        if (!request.DryRun)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        response.Message = $"Quét hoàn tất: {response.ExpiredAndLockedCount} lô hết hạn (xuất hủy), " +
                           $"{response.Discounted50PercentCount} lô giảm 50%, " +
                           $"{response.Discounted30PercentCount} lô giảm 30%, " +
                           $"{response.FlashSaleActivatedCount} sp Flash Sale. (DryRun: {request.DryRun})";

        return response;
    }
}
