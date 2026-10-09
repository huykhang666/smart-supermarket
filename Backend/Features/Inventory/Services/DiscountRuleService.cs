using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

public class DiscountRuleService : IDiscountRuleService
{
    private readonly IDiscountRuleRepository _discountRuleRepository;
    private readonly AppDbContext _dbContext;

    public DiscountRuleService(IDiscountRuleRepository discountRuleRepository, AppDbContext dbContext)
    {
        _discountRuleRepository = discountRuleRepository;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DiscountRule>> GetAllAsync(bool includeInactive = false)
    {
        return includeInactive
            ? await _discountRuleRepository.GetAllAsync()
            : await _discountRuleRepository.GetAllActiveAsync();
    }

    public async Task<DiscountRule?> GetByIdAsync(int id)
    {
        return await _discountRuleRepository.GetByIdAsync(id);
    }

    public async Task<DiscountRule> CreateAsync(int daysBeforeExpiry, decimal discountPercent, bool isActive, string? description)
    {
        if (daysBeforeExpiry <= 0)
            throw new ArgumentException("Số ngày trước khi hết hạn phải lớn hơn 0.");

        if (discountPercent <= 0 || discountPercent > 100)
            throw new ArgumentException("Phần trăm giảm giá phải nằm trong khoảng từ 1% đến 100%.");

        var rule = new DiscountRule
        {
            DaysBeforeExpiry = daysBeforeExpiry,
            DiscountPercent = discountPercent,
            IsActive = isActive,
            Description = description ?? $"Hàng còn {daysBeforeExpiry} ngày hết hạn giảm {discountPercent:N0}%"
        };

        await _discountRuleRepository.AddAsync(rule);
        await _discountRuleRepository.SaveChangesAsync();
        return rule;
    }

    public async Task UpdateAsync(int id, int daysBeforeExpiry, decimal discountPercent, bool isActive, string description, int userId)
    {
        var rule = await _discountRuleRepository.GetByIdAsync(id);
        if (rule == null)
            throw new KeyNotFoundException($"Không tìm thấy quy tắc giảm giá có ID = {id}");

        if (daysBeforeExpiry <= 0)
            throw new ArgumentException("Số ngày trước khi hết hạn phải lớn hơn 0.");

        if (discountPercent <= 0 || discountPercent > 100)
            throw new ArgumentException("Phần trăm giảm giá phải nằm trong khoảng từ 1% đến 100%.");

        rule.DaysBeforeExpiry = daysBeforeExpiry;
        rule.DiscountPercent = discountPercent;
        rule.IsActive = isActive;
        rule.Description = description;

        await _discountRuleRepository.UpdateAsync(rule);
        await _discountRuleRepository.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rule = await _discountRuleRepository.GetByIdAsync(id);
        if (rule == null)
            return false;

        await _discountRuleRepository.DeleteAsync(rule);
        await _discountRuleRepository.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetApplicableDiscountAsync(DateOnly? expiryDate)
    {
        if (!expiryDate.HasValue)
            return 0m;

        int daysUntilExpiry = expiryDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

        if (daysUntilExpiry < 0)
            return 0m; // Quá hạn

        var rule = await _discountRuleRepository.GetApplicableRuleAsync(daysUntilExpiry);
        return rule?.DiscountPercent ?? 0m;
    }

    public async Task<IEnumerable<ExpiringProductDto>> GetExpiringProductsAsync()
    {
        var now = DateTime.UtcNow;
        var thresholdDate = now.AddDays(30);

        var activeRules = (await _discountRuleRepository.GetAllActiveAsync()).ToList();

        // 1. Quét từ InventoryBatches (Lô hàng tồn kho thực tế)
        var batchQuery = await _dbContext.InventoryBatches
            .Include(b => b.Product)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= thresholdDate)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

        var result = new List<ExpiringProductDto>();

        foreach (var batch in batchQuery)
        {
            if (batch.Product == null) continue;

            int daysRemaining = (int)Math.Ceiling((batch.ExpiryDate - now).TotalDays);
            decimal discountPercent = 0m;

            if (daysRemaining >= 0 && activeRules.Any())
            {
                var matchedRule = activeRules
                    .Where(r => r.DaysBeforeExpiry >= daysRemaining)
                    .OrderBy(r => r.DaysBeforeExpiry)
                    .FirstOrDefault();

                if (matchedRule != null)
                {
                    discountPercent = matchedRule.DiscountPercent;
                }
            }

            decimal origPrice = batch.Product.Price;
            decimal clearancePrice = discountPercent > 0
                ? Math.Round(origPrice * (1 - (discountPercent / 100m)), 0)
                : origPrice;

            string alertLevel = daysRemaining < 0 ? "Expired"
                : daysRemaining <= 7 ? "Critical"
                : daysRemaining <= 15 ? "Warning"
                : "Notice";

            result.Add(new ExpiringProductDto
            {
                ProductId = batch.ProductId,
                ProductName = batch.Product.ProductName,
                Barcode = batch.Product.Barcode,
                BatchCode = batch.BatchCode,
                Quantity = batch.Quantity,
                ExpiryDate = batch.ExpiryDate,
                DaysRemaining = daysRemaining,
                OriginalPrice = origPrice,
                DiscountPercent = discountPercent,
                ClearancePrice = clearancePrice,
                AlertLevel = alertLevel
            });
        }

        // 2. Quét bổ sung từ bảng Product (nếu sản phẩm có ExpiryDate riêng và chưa có trong danh sách batch)
        var productExpiring = await _dbContext.Products
            .Where(p => p.ExpiryDate != null && p.ExpiryDate <= thresholdDate)
            .ToListAsync();

        foreach (var prod in productExpiring)
        {
            if (result.Any(r => r.ProductId == prod.ProductId)) continue;

            DateTime exp = prod.ExpiryDate!.Value;
            int daysRemaining = (int)Math.Ceiling((exp - now).TotalDays);
            decimal discountPercent = 0m;

            if (daysRemaining >= 0 && activeRules.Any())
            {
                var matchedRule = activeRules
                    .Where(r => r.DaysBeforeExpiry >= daysRemaining)
                    .OrderBy(r => r.DaysBeforeExpiry)
                    .FirstOrDefault();

                if (matchedRule != null)
                {
                    discountPercent = matchedRule.DiscountPercent;
                }
            }

            decimal origPrice = prod.Price;
            decimal clearancePrice = discountPercent > 0
                ? Math.Round(origPrice * (1 - (discountPercent / 100m)), 0)
                : origPrice;

            string alertLevel = daysRemaining < 0 ? "Expired"
                : daysRemaining <= 7 ? "Critical"
                : daysRemaining <= 15 ? "Warning"
                : "Notice";

            result.Add(new ExpiringProductDto
            {
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Barcode = prod.Barcode,
                BatchCode = "SP-GỐC",
                Quantity = 10,
                ExpiryDate = exp,
                DaysRemaining = daysRemaining,
                OriginalPrice = origPrice,
                DiscountPercent = discountPercent,
                ClearancePrice = clearancePrice,
                AlertLevel = alertLevel
            });
        }

        return result.OrderBy(r => r.DaysRemaining);
    }

    public async Task<CheckProductExpiryDiscountResponse?> CheckProductExpiryDiscountAsync(string barcodeOrId)
    {
        Product? product = null;
        if (int.TryParse(barcodeOrId, out int pId))
        {
            product = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == pId);
        }

        product ??= await _dbContext.Products.FirstOrDefaultAsync(p => p.Barcode == barcodeOrId);
        if (product == null) return null;

        var now = DateTime.UtcNow;

        // Ưu tiên lô FEFO sắp hết hạn nhất còn tồn kho
        var earliestBatch = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == product.ProductId && b.Quantity > 0 && b.ExpiryDate >= now)
            .OrderBy(b => b.ExpiryDate)
            .FirstOrDefaultAsync();

        DateTime? expiry = earliestBatch?.ExpiryDate ?? product.ExpiryDate;
        if (!expiry.HasValue)
        {
            return new CheckProductExpiryDiscountResponse
            {
                ProductId = product.ProductId,
                Barcode = product.Barcode,
                ProductName = product.ProductName,
                OriginalPrice = product.Price,
                ClearancePrice = product.Price,
                DiscountPercent = 0,
                IsNearExpiry = false
            };
        }

        int daysRemaining = (int)Math.Ceiling((expiry.Value - now).TotalDays);
        var activeRules = (await _discountRuleRepository.GetAllActiveAsync()).ToList();

        decimal discountPercent = 0m;
        string ruleDesc = "";

        if (daysRemaining >= 0 && daysRemaining <= 30 && activeRules.Any())
        {
            var matchedRule = activeRules
                .Where(r => r.DaysBeforeExpiry >= daysRemaining)
                .OrderBy(r => r.DaysBeforeExpiry)
                .FirstOrDefault();

            if (matchedRule != null)
            {
                discountPercent = matchedRule.DiscountPercent;
                ruleDesc = matchedRule.Description ?? $"Giảm {discountPercent:N0}% xả hàng cận date (còn {daysRemaining} ngày)";
            }
        }

        decimal clearancePrice = discountPercent > 0
            ? Math.Round(product.Price * (1 - (discountPercent / 100m)), 0)
            : product.Price;

        return new CheckProductExpiryDiscountResponse
        {
            ProductId = product.ProductId,
            Barcode = product.Barcode,
            ProductName = product.ProductName,
            OriginalPrice = product.Price,
            ExpiryDate = expiry,
            DaysRemaining = daysRemaining,
            DiscountPercent = discountPercent,
            ClearancePrice = clearancePrice,
            IsNearExpiry = discountPercent > 0,
            RuleDescription = ruleDesc
        };
    }
}
