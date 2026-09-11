using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public class DiscountRuleRepository : IDiscountRuleRepository
{
    private readonly AppDbContext _dbContext;

    public DiscountRuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DiscountRule>> GetAllActiveAsync()
    {
        return await _dbContext.DiscountRules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.DaysBeforeExpiry)
            .ToListAsync();
    }

    public async Task<DiscountRule?> GetApplicableRuleAsync(int daysUntilExpiry)
    {
        return await _dbContext.DiscountRules
            .Where(r => r.IsActive && r.DaysBeforeExpiry >= daysUntilExpiry)
            .OrderBy(r => r.DaysBeforeExpiry)
            .FirstOrDefaultAsync();
    }

    public async Task<DiscountRule?> GetByIdAsync(int id)
    {
        return await _dbContext.DiscountRules.FindAsync(id);
    }

    public async Task UpdateAsync(DiscountRule discountRule)
    {
        _dbContext.DiscountRules.Update(discountRule);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
