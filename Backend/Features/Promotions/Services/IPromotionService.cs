using SmartSupermarket.Backend.Features.Promotions.DTOs;

namespace SmartSupermarket.Backend.Features.Promotions.Services;

public interface IPromotionService
{
    Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PromotionDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PromotionPagedResult> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionDto> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplyPromotionResponse> ApplyPromotionAsync(ApplyPromotionRequest request, CancellationToken cancellationToken = default);
}
