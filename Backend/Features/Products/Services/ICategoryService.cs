using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Products.Services;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryPagedResult> GetPagedAsync(string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> SearchAsync(string search, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> MoveCategoryProductsAsync(MoveCategoryRequest request, CancellationToken cancellationToken = default);
}
