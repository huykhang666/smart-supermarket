using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Promotions.Repositories;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Promotion> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    void Update(Promotion promotion);
    void Delete(Promotion promotion);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
