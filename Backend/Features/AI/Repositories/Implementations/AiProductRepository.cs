using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Repositories.Implementations;

public class AiProductRepository : IAiProductRepository
{
    private readonly AppDbContext _context;

    public AiProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> SearchProductsAsync(
        string keyword,
        int? categoryId = null,
        int limit = 15,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(lowerKeyword) ||
                                     p.Barcode.ToLower().Contains(lowerKeyword));
        }

        return await query.Take(Math.Max(1, limit)).ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && p.Status == ProductStatus.Active)
            .Take(Math.Max(1, limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetAlternativeProductAsync(
        int originalProductId,
        int categoryId,
        decimal maxPrice,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.ProductId != originalProductId &&
                        p.CategoryId == categoryId &&
                        p.Status == ProductStatus.Active &&
                        p.Price <= maxPrice)
            .OrderByDescending(p => p.Price)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
