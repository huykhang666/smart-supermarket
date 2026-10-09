using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public interface IDiscountRuleRepository
{
    Task<IEnumerable<DiscountRule>> GetAllAsync();
    Task<IEnumerable<DiscountRule>> GetAllActiveAsync();
    Task<DiscountRule?> GetApplicableRuleAsync(int daysUntilExpiry);
    Task<DiscountRule?> GetByIdAsync(int id);
    Task AddAsync(DiscountRule discountRule);
    Task UpdateAsync(DiscountRule discountRule);
    Task DeleteAsync(DiscountRule discountRule);
    Task SaveChangesAsync();
}
