using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsNameAsync(string categoryName, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
