using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Promotions.DTOs;
using SmartSupermarket.Backend.Features.Promotions.Repositories;

namespace SmartSupermarket.Backend.Features.Promotions.Services;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;

    public PromotionService(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
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
                DiscountAmount = 0,
                FinalAmount = request.OrderTotalAmount
            };
        }

        decimal discountAmount = 0;
        if (promotion.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = request.OrderTotalAmount * (promotion.DiscountValue / 100m);
            if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
            {
                discountAmount = promotion.MaximumDiscountAmount.Value;
            }
        }
        else
        {
            discountAmount = promotion.DiscountValue;
        }

        if (discountAmount > request.OrderTotalAmount)
        {
            discountAmount = request.OrderTotalAmount;
        }

        decimal finalAmount = request.OrderTotalAmount - discountAmount;

        return new ApplyPromotionResponse
        {
            IsSuccess = true,
            Message = "Áp dụng mã khuyến mãi thành công.",
            PromotionCode = promotion.PromotionCode,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount
        };
    }

    private static PromotionDto MapToDto(Promotion promotion)
    {
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
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            IsActive = promotion.IsActive,
            CreatedAt = promotion.CreatedAt
        };
    }
}
