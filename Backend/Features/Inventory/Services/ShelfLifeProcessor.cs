using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

/// <summary>
/// Khung xử lý quét hạn sử dụng và tự động điều chỉnh giá động.
/// </summary>
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
        _logger.LogInformation("--- BẮT ĐẦU QUÉT HẠN SỬ DỤNG & GIÁ ĐỘNG (FlashSale: {IsFlash}, DryRun: {DryRun}) ---", 
            request.IsEveningFlashSale, request.DryRun);

        var now = DateTime.UtcNow;
        var today = now.Date;

        var response = new ShelfLifeScanResponse
        {
            ScannedAt = now,
            IsDryRun = request.DryRun,
            AffectedItems = new List<ShelfLifeItemDetailResponse>()
        };

        // 1. Quét danh sách các Lô Hàng trong kho còn tồn (Quantity > 0)
        var batches = await _dbContext.InventoryBatches
            .Include(b => b.Product)
            .ThenInclude(p => p.Category)
            .Where(b => b.Quantity > 0)
            .ToListAsync(cancellationToken);

        response.TotalProductsScanned = batches.Count;

        foreach (var batch in batches)
        {
            if (batch.Product == null) continue;

            var product = batch.Product;
            int daysRemaining = (batch.ExpiryDate.Date - today).Days;

            // BẬC 1: Quá hạn hôm nay -> KHÓA BÁN (Expired)
            if (daysRemaining <= 0)
            {
                if (!request.DryRun)
                {
                    batch.Status = "Expired";
                    product.Status = ProductStatus.Inactive;
                    product.UpdatedAt = now;
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

                _logger.LogWarning("Khóa bán sản phẩm quá hạn: {Name} (Lô: {Batch}, Hạn: {Exp:dd/MM/yyyy})", 
                    product.ProductName, batch.BatchCode, batch.ExpiryDate);
            }
            // BẬC 2: Còn <= 1 ngày -> Giảm 50%
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
            // BẬC 3: Còn 2 - 3 ngày -> Giảm 30%
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

        // 2. Xử lý Flash Sale chiều tối sau 18h (Hàng tươi sống, rau củ, bánh mì)
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

        // Lưu CSDL nếu không phải DryRun
        if (!request.DryRun)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        response.Message = $"Quét hoàn tất: {response.ExpiredAndLockedCount} sp hết hạn, " +
                           $"{response.Discounted50PercentCount} sp giảm 50%, " +
                           $"{response.Discounted30PercentCount} sp giảm 30%, " +
                           $"{response.FlashSaleActivatedCount} sp Flash Sale. (DryRun: {request.DryRun})";

        _logger.LogInformation("--- KẾT THÚC TIẾN TRÌNH: {Message} ---", response.Message);
        return response;
    }
}
