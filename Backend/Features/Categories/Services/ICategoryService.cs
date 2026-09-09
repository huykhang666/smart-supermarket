using SmartSupermarket.Backend.Features.Categories.DTOs;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Categories.Services;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<CategoryPagedResult> GetPagedAsync(string? search, byte? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> RestoreAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> MoveParentAsync(int id, int? newParentId, CancellationToken cancellationToken = default);
    Task<bool> MoveCategoryProductsAsync(MoveCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> ReorderAsync(int id, int newOrderIndex, CancellationToken cancellationToken = default);
    Task<string> UploadImageAsync(int id, string imageUrl, CancellationToken cancellationToken = default);
}
