using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Categories.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(string? search, byte? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetDropdownAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameAsync(string categoryName, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsSlugAsync(string slug, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
    Task<List<int>> GetDescendantCategoryIdsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
    Task MoveProductCategoryRecordsAsync(int sourceCategoryId, int targetCategoryId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
