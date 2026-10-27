using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Repositories.Implementations;

public class AiPromotionRepository : IAiPromotionRepository
{
    private readonly AppDbContext _context;

    public AiPromotionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<Promotion?> GetPromotionByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cleanCode = code?.Trim().ToUpperInvariant() ?? string.Empty;

        return await _context.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == cleanCode && p.IsActive && p.StartDate <= now && p.EndDate >= now, cancellationToken);
    }

    public async Task<List<DiscountRule>> GetActiveDiscountRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DiscountRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.DaysBeforeExpiry)
            .ToListAsync(cancellationToken);
    }
}
