using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;

namespace SmartSupermarket.Backend.Features.Products.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public CategoryService(ICategoryRepository categoryRepository, IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
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

    public async Task<IEnumerable<CategoryDto>> SearchAsync(string search, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.SearchAsync(search, cancellationToken);
        return categories.Select(MapToDto);
    }

    public async Task<IEnumerable<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetTreeAsync(cancellationToken);
        
        // BR-CAT-05: Sắp xếp bảng chữ cái A-Z và tính toán ProductCount
        return categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryTreeDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ProductCount = c.Products?.Count ?? 0,
                CreatedAt = c.CreatedAt
            });
    }

    public async Task<IEnumerable<CategoryDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetDropdownAsync(cancellationToken);
        
        // BR-CAT-05: Chuẩn hóa dữ liệu dropdown tối giản
        return categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDropdownDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            });
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request.CategoryName, request.Description);

        string name = request.CategoryName.Trim();

        // BR-CAT-01: Tên danh mục duy nhất trên toàn hệ thống
        if (await _categoryRepository.ExistsNameAsync(name, null, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã tồn tại trên hệ thống (BR-CAT-01).");
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

        ValidateCategoryRequest(request.CategoryName, request.Description);

        string name = request.CategoryName.Trim();

        // BR-CAT-01: Tên danh mục duy nhất khi cập nhật
        if (await _categoryRepository.ExistsNameAsync(name, id, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã được sử dụng bởi danh mục khác (BR-CAT-01).");
        }

        category.CategoryName = name;
        category.Description = request.Description?.Trim();

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await SoftDeleteAsync(id, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        // BR-CAT-03: Kiểm tra sản phẩm liên kết trước khi xóa
        var products = await _categoryRepository.GetProductsByCategoryIdAsync(id, cancellationToken);
        var productList = products.ToList();
        if (productList.Any())
        {
            throw new InvalidOperationException($"Không thể xóa danh mục đang có {productList.Count} sản phẩm liên kết (BR-CAT-03). Vui lòng chuyển sản phẩm sang danh mục khác trước.");
        }

        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> MoveCategoryProductsAsync(MoveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // BR-CAT-04: Danh mục nguồn và đích phải khác nhau
        if (request.SourceCategoryId == request.TargetCategoryId)
        {
            throw new ArgumentException("Danh mục nguồn và danh mục đích phải khác nhau (BR-CAT-04).");
        }

        var sourceCategory = await _categoryRepository.GetByIdAsync(request.SourceCategoryId, cancellationToken);
        if (sourceCategory == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục nguồn với ID {request.SourceCategoryId}.");
        }

        var targetCategory = await _categoryRepository.GetByIdAsync(request.TargetCategoryId, cancellationToken);
        if (targetCategory == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục đích với ID {request.TargetCategoryId}.");
        }

        // Chuyển toàn bộ sản phẩm thuộc danh mục nguồn sang danh mục đích
        var sourceProducts = await _categoryRepository.GetProductsByCategoryIdAsync(request.SourceCategoryId, cancellationToken);
        foreach (var product in sourceProducts)
        {
            product.CategoryId = request.TargetCategoryId;
            product.UpdatedAt = DateTime.UtcNow;
            _productRepository.Update(product);
        }

        await _productRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateCategoryRequest(string categoryName, string? description)
    {
        // BR-CAT-01: Tên danh mục không được trống và max 100 ký tự
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Tên danh mục không được để trống (BR-CAT-01).");
        }

        if (categoryName.Trim().Length > 100)
        {
            throw new ArgumentException("Tên danh mục không được vượt quá 100 ký tự (BR-CAT-01).");
        }

        // BR-CAT-02: Mô tả tối đa 255 ký tự
        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 255)
        {
            throw new ArgumentException("Mô tả danh mục không được vượt quá 255 ký tự (BR-CAT-02).");
        }
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
