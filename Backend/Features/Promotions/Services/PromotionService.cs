using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Promotions.DTOs;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Promotions.Services;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly AppDbContext? _dbContext;

    public PromotionService(IPromotionRepository promotionRepository, AppDbContext? dbContext = null)
    {
        _promotionRepository = promotionRepository;
        _dbContext = dbContext;
    }

    public async Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        return promotion == null ? null : MapToDto(promotion);
    }

    public async Task<PromotionDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByCodeAsync(code, cancellationToken);
        return promotion == null ? null : MapToDto(promotion);
    }

    public async Task<PromotionPagedResult> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _promotionRepository.GetPagedAsync(search, isActive, page, pageSize, cancellationToken);
        return new PromotionPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _promotionRepository.GetByCodeAsync(request.PromotionCode, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Mã khuyến mãi '{request.PromotionCode}' đã tồn tại.");
        }

        var promotion = new Promotion
        {
            PromotionCode = request.PromotionCode.Trim().ToUpper(),
            PromotionName = request.PromotionName,
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaximumDiscountAmount = request.MaximumDiscountAmount,
            TriggerProductId = request.TriggerProductId,
            TargetCategoryId = request.TargetCategoryId,
            TargetProductId = request.TargetProductId,
            RequiredQuantity = request.RequiredQuantity,
            GiftQuantity = request.GiftQuantity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _promotionRepository.AddAsync(promotion, cancellationToken);
        await _promotionRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(promotion);
    }

    public async Task<PromotionDto> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promotion == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khuyến mãi có ID = {id}");
        }

        promotion.PromotionName = request.PromotionName;
        promotion.Description = request.Description;
        promotion.DiscountType = request.DiscountType;
        promotion.DiscountValue = request.DiscountValue;
        promotion.MinimumOrderAmount = request.MinimumOrderAmount;
        promotion.MaximumDiscountAmount = request.MaximumDiscountAmount;
        promotion.TriggerProductId = request.TriggerProductId;
        promotion.TargetCategoryId = request.TargetCategoryId;
        promotion.TargetProductId = request.TargetProductId;
        promotion.RequiredQuantity = request.RequiredQuantity;
        promotion.GiftQuantity = request.GiftQuantity;
        promotion.StartDate = request.StartDate;
        promotion.EndDate = request.EndDate;
        promotion.IsActive = request.IsActive;
        promotion.UpdatedAt = DateTime.UtcNow;

        _promotionRepository.Update(promotion);
        await _promotionRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(promotion);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promotion == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khuyến mãi có ID = {id}");
        }

        _promotionRepository.Delete(promotion);
        await _promotionRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ApplyPromotionResponse> ApplyPromotionAsync(ApplyPromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByCodeAsync(request.PromotionCode, cancellationToken);
        if (promotion == null || !promotion.IsActive)
        {
            return new ApplyPromotionResponse
            {
                IsSuccess = false,
                Message = "Mã khuyến mãi không tồn tại hoặc đã hết hạn sử dụng.",
                PromotionCode = request.PromotionCode,
                DiscountAmount = 0,
                FinalAmount = request.OrderTotalAmount
            };
        }

        DateTime now = DateTime.UtcNow;
        if (now < promotion.StartDate || now > promotion.EndDate)
        {
            return new ApplyPromotionResponse
            {
                IsSuccess = false,
                Message = "Mã khuyến mãi chưa đến hạn hoặc đã quá hạn áp dụng.",
                PromotionCode = promotion.PromotionCode,
                PromotionName = promotion.PromotionName,
                DiscountType = promotion.DiscountType,
                DiscountAmount = 0,
                FinalAmount = request.OrderTotalAmount
            };
        }

        if (request.OrderTotalAmount < promotion.MinimumOrderAmount)
        {
            return new ApplyPromotionResponse
            {
                IsSuccess = false,
                Message = $"Đơn hàng tối thiểu để áp dụng mã này là {promotion.MinimumOrderAmount:N0} VNĐ.",
                PromotionCode = promotion.PromotionCode,
                PromotionName = promotion.PromotionName,
                DiscountType = promotion.DiscountType,
                DiscountAmount = 0,
                FinalAmount = request.OrderTotalAmount
            };
        }

        decimal discountAmount = 0;
        var details = new List<string>();

        switch (promotion.DiscountType)
        {
            case "Percentage":
                discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
                if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                {
                    discountAmount = promotion.MaximumDiscountAmount.Value;
                    details.Add($"Giảm {promotion.DiscountValue:N0}% (Tối đa {promotion.MaximumDiscountAmount.Value:N0} đ)");
                }
                else
                {
                    details.Add($"Giảm {promotion.DiscountValue:N0}% trên tổng đơn hàng");
                }
                break;

            case "FixedAmount":
                discountAmount = promotion.DiscountValue;
                details.Add($"Giảm trực tiếp {discountAmount:N0} đ");
                break;

            case "BuyXGetY":
                int reqQty = promotion.RequiredQuantity.GetValueOrDefault(2);
                int giftQty = promotion.GiftQuantity.GetValueOrDefault(1);

                if (request.Items != null && request.Items.Any())
                {
                    var eligibleItems = promotion.TargetProductId.HasValue
                        ? request.Items.Where(i => i.ProductId == promotion.TargetProductId.Value).ToList()
                        : request.Items.ToList();

                    foreach (var item in eligibleItems)
                    {
                        int bundleSize = reqQty + giftQty;
                        int freeCount = (item.Quantity / bundleSize) * giftQty;
                        if (freeCount == 0 && item.Quantity >= reqQty)
                        {
                            freeCount = giftQty;
                        }

                        if (freeCount > 0)
                        {
                            decimal itemDisc = freeCount * item.UnitPrice;
                            discountAmount += itemDisc;
                            details.Add($"Tặng {freeCount} sản phẩm '{item.ProductName}': -{itemDisc:N0} đ");
                        }
                    }

                    if (discountAmount == 0)
                    {
                        return new ApplyPromotionResponse
                        {
                            IsSuccess = false,
                            Message = $"Giỏ hàng chưa đủ số lượng để áp dụng Mua {reqQty} Tặng {giftQty}.",
                            PromotionCode = promotion.PromotionCode,
                            PromotionName = promotion.PromotionName,
                            DiscountType = promotion.DiscountType,
                            DiscountAmount = 0,
                            FinalAmount = request.OrderTotalAmount
                        };
                    }
                }
                else
                {
                    discountAmount = promotion.DiscountValue > 0 ? promotion.DiscountValue : 20000m;
                    details.Add($"Áp dụng chương trình Mua {reqQty} Tặng {giftQty}");
                }
                break;

            case "CategoryDiscount":
                if (request.Items != null && request.Items.Any() && promotion.TargetCategoryId.HasValue)
                {
                    var catItems = request.Items.Where(i => i.CategoryId == promotion.TargetCategoryId.Value).ToList();
                    if (!catItems.Any())
                    {
                        return new ApplyPromotionResponse
                        {
                            IsSuccess = false,
                            Message = $"Giỏ hàng không có sản phẩm nào thuộc ngành hàng khuyến mãi (ID #{promotion.TargetCategoryId}).",
                            PromotionCode = promotion.PromotionCode,
                            PromotionName = promotion.PromotionName,
                            DiscountType = promotion.DiscountType,
                            DiscountAmount = 0,
                            FinalAmount = request.OrderTotalAmount
                        };
                    }

                    decimal catTotal = catItems.Sum(i => i.UnitPrice * i.Quantity);
                    discountAmount = catTotal * (promotion.DiscountValue / 100m);
                    if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                    {
                        discountAmount = promotion.MaximumDiscountAmount.Value;
                    }
                    details.Add($"Giảm {promotion.DiscountValue:N0}% cho {catItems.Count} món ngành hàng khuyến mãi");
                }
                else
                {
                    discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
                    details.Add($"Giảm {promotion.DiscountValue:N0}% theo ngành hàng");
                }
                break;

            case "QuantityTier":
                int minTier = promotion.RequiredQuantity.GetValueOrDefault(3);
                if (request.Items != null && request.Items.Any())
                {
                    var tierItems = promotion.TargetProductId.HasValue
                        ? request.Items.Where(i => i.ProductId == promotion.TargetProductId.Value && i.Quantity >= minTier).ToList()
                        : request.Items.Where(i => i.Quantity >= minTier).ToList();

                    if (!tierItems.Any())
                    {
                        return new ApplyPromotionResponse
                        {
                            IsSuccess = false,
                            Message = $"Chưa có mặt hàng nào đạt số lượng từ {minTier} sản phẩm trở lên để hưởng giá sỉ.",
                            PromotionCode = promotion.PromotionCode,
                            PromotionName = promotion.PromotionName,
                            DiscountType = promotion.DiscountType,
                            DiscountAmount = 0,
                            FinalAmount = request.OrderTotalAmount
                        };
                    }

                    foreach (var item in tierItems)
                    {
                        decimal itemTotal = item.UnitPrice * item.Quantity;
                        decimal tierDisc = itemTotal * (promotion.DiscountValue / 100m);
                        discountAmount += tierDisc;
                        details.Add($"Giảm sỉ {promotion.DiscountValue:N0}% cho {item.Quantity}x '{item.ProductName}': -{tierDisc:N0} đ");
                    }
                }
                else
                {
                    discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
                    details.Add($"Chiết khấu mua sỉ {promotion.DiscountValue:N0}%");
                }
                break;

            case "ComboDiscount":
                // Khuyến mãi Mua kèm / Combo: Mua TriggerProduct (ví dụ Bàn chải) giảm X% TargetProduct (ví dụ Kem đánh răng)
                if (request.Items != null && request.Items.Any() && promotion.TriggerProductId.HasValue && promotion.TargetProductId.HasValue)
                {
                    var triggerItem = request.Items.FirstOrDefault(i => i.ProductId == promotion.TriggerProductId.Value);
                    var targetItem = request.Items.FirstOrDefault(i => i.ProductId == promotion.TargetProductId.Value);

                    if (triggerItem == null || targetItem == null)
                    {
                        return new ApplyPromotionResponse
                        {
                            IsSuccess = false,
                            Message = $"Giỏ hàng cần có cả 2 sản phẩm Combo để được hưởng ưu đãi.",
                            PromotionCode = promotion.PromotionCode,
                            PromotionName = promotion.PromotionName,
                            DiscountType = promotion.DiscountType,
                            DiscountAmount = 0,
                            FinalAmount = request.OrderTotalAmount
                        };
                    }

                    int comboSets = Math.Min(triggerItem.Quantity, targetItem.Quantity);
                    decimal targetItemSubtotal = comboSets * targetItem.UnitPrice;
                    discountAmount = targetItemSubtotal * (promotion.DiscountValue / 100m);
                    if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                    {
                        discountAmount = promotion.MaximumDiscountAmount.Value;
                    }
                    details.Add($"Ưu đãi Combo: Mua '{triggerItem.ProductName}' giảm {promotion.DiscountValue:N0}% cho {comboSets}x '{targetItem.ProductName}': -{discountAmount:N0} đ");
                }
                else
                {
                    discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
                    details.Add($"Ưu đãi Combo giảm {promotion.DiscountValue:N0}%");
                }
                break;

            default:
                discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
                break;
        }

        if (discountAmount > request.OrderTotalAmount)
        {
            discountAmount = request.OrderTotalAmount;
        }

        decimal finalAmount = Math.Max(0, request.OrderTotalAmount - discountAmount);

        return new ApplyPromotionResponse
        {
            IsSuccess = true,
            Message = $"Áp dụng khuyến mãi '{promotion.PromotionName}' thành công.",
            PromotionCode = promotion.PromotionCode,
            PromotionName = promotion.PromotionName,
            DiscountType = promotion.DiscountType,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            AppliedDetails = details
        };
    }

    private PromotionDto MapToDto(Promotion promotion)
    {
        string? catName = null;
        string? prodName = null;
        string? trigProdName = null;
        if (_dbContext != null)
        {
            if (promotion.TargetCategoryId.HasValue)
            {
                catName = _dbContext.Categories.FirstOrDefault(c => c.CategoryId == promotion.TargetCategoryId.Value)?.CategoryName;
            }
            if (promotion.TargetProductId.HasValue)
            {
                prodName = _dbContext.Products.FirstOrDefault(p => p.ProductId == promotion.TargetProductId.Value)?.ProductName;
            }
            if (promotion.TriggerProductId.HasValue)
            {
                trigProdName = _dbContext.Products.FirstOrDefault(p => p.ProductId == promotion.TriggerProductId.Value)?.ProductName;
            }
        }

        return new PromotionDto
        {
            PromotionId = promotion.PromotionId,
            PromotionCode = promotion.PromotionCode,
            PromotionName = promotion.PromotionName,
            Description = promotion.Description ?? string.Empty,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MinimumOrderAmount = promotion.MinimumOrderAmount,
            MaximumDiscountAmount = promotion.MaximumDiscountAmount,
            TriggerProductId = promotion.TriggerProductId,
            TriggerProductName = trigProdName,
            TargetCategoryId = promotion.TargetCategoryId,
            TargetCategoryName = catName,
            TargetProductId = promotion.TargetProductId,
            TargetProductName = prodName,
            RequiredQuantity = promotion.RequiredQuantity,
            GiftQuantity = promotion.GiftQuantity,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            IsActive = promotion.IsActive,
            CreatedAt = promotion.CreatedAt
        };
    }
}
