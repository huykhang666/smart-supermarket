using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public interface ICategoryRepository
{
    // CRUD
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Search
    Task<IEnumerable<Category>> SearchAsync(string search, CancellationToken cancellationToken = default);

    // Tree Query
    Task<IEnumerable<Category>> GetTreeAsync(CancellationToken cancellationToken = default);

    // Pagination
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Get Dropdown
    Task<IEnumerable<Category>> GetDropdownAsync(CancellationToken cancellationToken = default);

    // Get Products By Category
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);

    // Existence Check
    Task<bool> ExistsNameAsync(string categoryName, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
}
