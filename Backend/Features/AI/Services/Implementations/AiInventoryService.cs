using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiInventoryService : IAiInventoryService
{
    private readonly IAiInventoryBatchRepository _batchRepository;
    private readonly IAiProductRepository _productRepository;

    public AiInventoryService(
        IAiInventoryBatchRepository batchRepository,
        IAiProductRepository productRepository)
    {
        _batchRepository = batchRepository;
        _productRepository = productRepository;
    }

    public async Task<int> GetStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _batchRepository.GetAvailableStockAsync(productId, cancellationToken);
    }

    public async Task<List<NearExpiryBatchDto>> GetNearExpiryBatchesAsync(int daysThreshold = 3, CancellationToken cancellationToken = default)
    {
        var rawBatches = await _batchRepository.GetNearExpiryBatchesAsync(daysThreshold, cancellationToken);
        var now = DateTime.UtcNow;
        var result = new List<NearExpiryBatchDto>();

        foreach (var b in rawBatches)
        {
            var remainingDays = Math.Max(0, (int)Math.Ceiling((b.ExpiryDate - now).TotalDays));
            var originalPrice = b.Product?.Price ?? 0m;
            decimal discountPercent = remainingDays <= 1 ? 50m : (remainingDays <= 3 ? 30m : 0m);
            var flashSalePrice = originalPrice * (1m - discountPercent / 100m);

            result.Add(new NearExpiryBatchDto
            {
                BatchId = b.BatchId,
                ProductId = b.ProductId,
                ProductName = b.Product?.ProductName ?? string.Empty,
                BatchCode = b.BatchCode,
                Quantity = b.Quantity,
                ExpiryDate = b.ExpiryDate,
                DaysRemaining = remainingDays,
                OriginalPrice = originalPrice,
                DiscountPercent = discountPercent,
                FlashSalePrice = flashSalePrice
            });
        }

        return result;
    }

    public async Task<List<InventoryBatch>> GetBatchesByProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _batchRepository.GetBatchesByProductIdAsync(productId, cancellationToken);
    }

    public async Task<Product?> GetAlternativeProductAsync(int outOfStockProductId, CancellationToken cancellationToken = default)
    {
        var originalProduct = await _productRepository.GetProductByIdAsync(outOfStockProductId, cancellationToken);
        if (originalProduct == null) return null;

        return await _productRepository.GetAlternativeProductAsync(
            outOfStockProductId,
            originalProduct.CategoryId,
            originalProduct.Price * 1.15m,
            cancellationToken);
    }
}
