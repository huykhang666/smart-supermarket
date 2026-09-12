using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

public interface IDiscountRuleRepository
{
    Task<IEnumerable<DiscountRule>> GetAllActiveAsync();
    Task<DiscountRule?> GetApplicableRuleAsync(int daysUntilExpiry);
    Task<DiscountRule?> GetByIdAsync(int id);
    Task UpdateAsync(DiscountRule discountRule);
    Task SaveChangesAsync();
}
