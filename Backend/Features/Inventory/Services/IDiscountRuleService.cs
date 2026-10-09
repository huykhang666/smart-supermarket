using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.DTOs;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public interface IDiscountRuleService
{
    Task<IEnumerable<DiscountRule>> GetAllAsync(bool includeInactive = false);
    Task<DiscountRule?> GetByIdAsync(int id);
    Task<DiscountRule> CreateAsync(int daysBeforeExpiry, decimal discountPercent, bool isActive, string? description);
    Task UpdateAsync(int id, int daysBeforeExpiry, decimal discountPercent, bool isActive, string description, int userId);
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetApplicableDiscountAsync(DateOnly? expiryDate);
    Task<IEnumerable<ExpiringProductDto>> GetExpiringProductsAsync();
    Task<CheckProductExpiryDiscountResponse?> CheckProductExpiryDiscountAsync(string barcodeOrId);
}
