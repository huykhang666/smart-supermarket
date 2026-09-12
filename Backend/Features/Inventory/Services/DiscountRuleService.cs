using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.Repositories;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public class DiscountRuleService : IDiscountRuleService
{
    private readonly IDiscountRuleRepository _discountRuleRepository;

    public DiscountRuleService(IDiscountRuleRepository discountRuleRepository)
    {
        _discountRuleRepository = discountRuleRepository;
    }

    public async Task<IEnumerable<DiscountRule>> GetAllAsync()
    {
        return await _discountRuleRepository.GetAllActiveAsync();
    }

    public async Task<decimal> GetApplicableDiscountAsync(DateOnly? expiryDate)
    {
        if (!expiryDate.HasValue)
            return 0m;

        int daysUntilExpiry = expiryDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

        if (daysUntilExpiry < 0)
            return 0m; // Expired

        var rule = await _discountRuleRepository.GetApplicableRuleAsync(daysUntilExpiry);
        return rule?.DiscountPercent ?? 0m;
    }

    public async Task UpdateAsync(int id, int daysBeforeExpiry, decimal discountPercent, bool isActive, string description, int userId)
    {
        var rule = await _discountRuleRepository.GetByIdAsync(id);
        if (rule == null)
            throw new KeyNotFoundException("Discount rule not found");

        rule.DaysBeforeExpiry = daysBeforeExpiry;
        rule.DiscountPercent = discountPercent;
        rule.IsActive = isActive;
        rule.Description = description;

        await _discountRuleRepository.UpdateAsync(rule);
        await _discountRuleRepository.SaveChangesAsync();
    }
}
