using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Promotions.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _dbContext;

    public PromotionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id, cancellationToken);
    }

    public async Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var codeTrim = code.Trim().ToUpper();
        return await _dbContext.Promotions
            .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == codeTrim, cancellationToken);
    }

    public async Task<(IEnumerable<Promotion> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Promotions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTrim = search.Trim().ToLower();
            query = query.Where(p =>
                p.PromotionCode.ToLower().Contains(searchTrim) ||
                p.PromotionName.ToLower().Contains(searchTrim));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _dbContext.Promotions.AddAsync(promotion, cancellationToken);
    }

    public void Update(Promotion promotion)
    {
        _dbContext.Promotions.Update(promotion);
    }

    public void Delete(Promotion promotion)
    {
        _dbContext.Promotions.Remove(promotion);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
