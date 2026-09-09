using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;

namespace SmartSupermarket.Backend.Features.Products.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return category == null ? null : MapToDto(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return categories.Select(MapToDto);
    }

    public async Task<CategoryPagedResult> GetPagedAsync(string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _categoryRepository.GetPagedAsync(search, page, pageSize, cancellationToken);

        return new CategoryPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            throw new ArgumentException("Tên danh mục không được để trống.");
        }

        string name = request.CategoryName.Trim();

        if (await _categoryRepository.ExistsNameAsync(name, null, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã tồn tại trên hệ thống.");
        }

        var category = new Category
        {
            CategoryName = name,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            throw new ArgumentException("Tên danh mục không được để trống.");
        }

        string name = request.CategoryName.Trim();

        if (await _categoryRepository.ExistsNameAsync(name, id, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã được sử dụng bởi danh mục khác.");
        }

        category.CategoryName = name;
        category.Description = request.Description?.Trim();

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        if (category.Products != null && category.Products.Any())
        {
            throw new InvalidOperationException($"Không thể xóa danh mục đang có {category.Products.Count} sản phẩm liên kết (BR-PROD-05).");
        }

        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            ProductCount = category.Products?.Count ?? 0,
            CreatedAt = category.CreatedAt
        };
    }
}
