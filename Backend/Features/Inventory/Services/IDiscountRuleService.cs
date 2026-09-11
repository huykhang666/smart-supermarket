using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public interface IDiscountRuleService
{
    Task<IEnumerable<DiscountRule>> GetAllAsync();
    Task<decimal> GetApplicableDiscountAsync(DateOnly? expiryDate);
    Task UpdateAsync(int id, int daysBeforeExpiry, decimal discountPercent, bool isActive, string description, int userId);
}
