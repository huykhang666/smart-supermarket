using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Categories.DTOs;
using SmartSupermarket.Backend.Features.Categories.Repositories;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Categories.Services;

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

    public async Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug, cancellationToken);
        return category == null ? null : MapToDto(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(includeInactive, cancellationToken);
        return categories.Select(MapToDto);
    }

    public async Task<CategoryPagedResult> GetPagedAsync(
        string? search,
        byte? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _categoryRepository.GetPagedAsync(search, status, page, pageSize, cancellationToken);

        return new CategoryPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<CategoryDto>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.SearchAsync(search, includeInactive, cancellationToken);
        return categories.Select(MapToDto);
    }

    public async Task<IEnumerable<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var roots = await _categoryRepository.GetTreeAsync(cancellationToken);
        return roots.Select(MapToTreeDto);
    }

    public async Task<IEnumerable<CategoryDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetDropdownAsync(cancellationToken);
        return categories.Select(c => new CategoryDropdownDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Slug = c.Slug,
            ParentId = c.ParentId,
            OrderIndex = c.OrderIndex
        });
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {categoryId}.");
        }

        var products = await _categoryRepository.GetProductsByCategoryIdAsync(categoryId, cancellationToken);
        return products.Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Barcode = p.Barcode,
            CategoryId = p.CategoryId,
            CategoryName = category.CategoryName,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.SupplierName,
            Price = p.Price,
            CostPrice = p.CostPrice,
            ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? "/images/products/no-image.png" : p.ImageUrl,
            Unit = p.Unit,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request.CategoryName, request.Description);

        string name = request.CategoryName.Trim();

        // BR-CAT-01: Unique Name
        if (await _categoryRepository.ExistsNameAsync(name, null, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã tồn tại trên hệ thống (BR-CAT-01).");
        }

        // Slug handling
        string slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(name) : GenerateSlug(request.Slug);
        if (await _categoryRepository.ExistsSlugAsync(slug, null, cancellationToken))
        {
            // If auto-generated slug conflicts, append timestamp/index
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        // Parent validation (BR-CAT-05)
        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy danh mục cha với ID {request.ParentId.Value}.");
            }
        }

        var category = new Category
        {
            CategoryName = name,
            Slug = slug,
            Description = request.Description?.Trim(),
            ParentId = request.ParentId,
            OrderIndex = request.OrderIndex,
            ImageUrl = request.ImageUrl,
            Status = request.Status == 0 ? (byte)1 : request.Status,
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

        // BR-CAT-01: Unique Name
        if (await _categoryRepository.ExistsNameAsync(name, id, cancellationToken))
        {
            throw new InvalidOperationException($"Danh mục tên '{name}' đã được sử dụng bởi danh mục khác (BR-CAT-01).");
        }

        // Slug handling
        string slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(name) : GenerateSlug(request.Slug);
        if (await _categoryRepository.ExistsSlugAsync(slug, id, cancellationToken))
        {
            throw new InvalidOperationException($"Slug '{slug}' đã được sử dụng bởi danh mục khác.");
        }

        // BR-CAT-05: Parent Loop Check
        if (request.ParentId.HasValue)
        {
            await ValidateParentLoopAsync(id, request.ParentId.Value, cancellationToken);
        }

        category.CategoryName = name;
        category.Slug = slug;
        category.Description = request.Description?.Trim();
        category.ParentId = request.ParentId;
        category.OrderIndex = request.OrderIndex;
        category.ImageUrl = request.ImageUrl;
        category.Status = request.Status;
        category.UpdatedAt = DateTime.UtcNow;

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

        // BR-CAT-03: Check linked products
        var products = await _categoryRepository.GetProductsByCategoryIdAsync(id, cancellationToken);
        var productList = products.ToList();
        if (productList.Any())
        {
            throw new InvalidOperationException($"Không thể xóa danh mục đang có {productList.Count} sản phẩm liên kết (BR-CAT-03). Vui lòng chuyển sản phẩm sang danh mục khác trước.");
        }

        category.Status = 2; // Inactive / Soft deleted
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<CategoryDto> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        category.Status = 1; // Active
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<CategoryDto> MoveParentAsync(int id, int? newParentId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        if (newParentId.HasValue)
        {
            await ValidateParentLoopAsync(id, newParentId.Value, cancellationToken);
        }

        category.ParentId = newParentId;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<bool> MoveCategoryProductsAsync(MoveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // BR-CAT-04: Source and Target must be different
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

        // Many-to-Many product migration
        await _categoryRepository.MoveProductCategoryRecordsAsync(request.SourceCategoryId, request.TargetCategoryId, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<CategoryDto> ReorderAsync(int id, int newOrderIndex, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        category.OrderIndex = newOrderIndex;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<string> UploadImageAsync(int id, string imageUrl, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy danh mục với ID {id}.");
        }

        category.ImageUrl = imageUrl;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return category.ImageUrl;
    }

    private async Task ValidateParentLoopAsync(int categoryId, int newParentId, CancellationToken cancellationToken)
    {
        if (categoryId == newParentId)
        {
            throw new InvalidOperationException("Không thể chọn chính danh mục làm danh mục cha (BR-CAT-05).");
        }

        var parent = await _categoryRepository.GetByIdAsync(newParentId, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Danh mục cha với ID {newParentId} không tồn tại.");
        }

        var descendantIds = await _categoryRepository.GetDescendantCategoryIdsAsync(categoryId, cancellationToken);
        if (descendantIds.Contains(newParentId))
        {
            throw new InvalidOperationException("Không thể chọn danh mục con/cháu làm danh mục cha (tránh vòng lặp phân cấp BR-CAT-05).");
        }
    }

    private static void ValidateCategoryRequest(string categoryName, string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Tên danh mục không được để trống (BR-CAT-01).");
        }

        if (categoryName.Trim().Length > 100)
        {
            throw new ArgumentException("Tên danh mục không được vượt quá 100 ký tự (BR-CAT-01).");
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 255)
        {
            throw new ArgumentException("Mô tả danh mục không được vượt quá 255 ký tự (BR-CAT-02).");
        }
    }

    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Remove accents
        string normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd').Replace('Đ', 'D')
            .ToLowerInvariant();

        // 2. Remove invalid characters and replace spaces/hyphens
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"\s+", "-").Trim('-');
        result = Regex.Replace(result, @"-+", "-");

        return result;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            ParentName = category.Parent?.CategoryName,
            OrderIndex = category.OrderIndex,
            ImageUrl = category.ImageUrl,
            Status = category.Status,
            ProductCount = category.Products?.Count ?? category.ProductCategories?.Count ?? 0,
            SubCategoryCount = category.SubCategories?.Count ?? 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private static CategoryTreeDto MapToTreeDto(Category category)
    {
        return new CategoryTreeDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            OrderIndex = category.OrderIndex,
            ImageUrl = category.ImageUrl,
            Status = category.Status,
            ProductCount = category.Products?.Count ?? category.ProductCategories?.Count ?? 0,
            CreatedAt = category.CreatedAt,
            SubCategories = category.SubCategories
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.CategoryName)
                .Select(MapToTreeDto)
                .ToList()
        };
    }
}
