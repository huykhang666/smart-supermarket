using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Categories.DTOs;
using SmartSupermarket.Backend.Features.Categories.Services;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Categories.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lấy danh sách danh mục (Phân trang, Tìm kiếm, Lọc trạng thái)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResult<CategoryPagedResult>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] byte? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetPagedAsync(search, status, page, pageSize, cancellationToken);
        return Ok(ApiResult<CategoryPagedResult>.Success(result, "Lấy danh sách danh mục thành công"));
    }

    /// <summary>
    /// Lấy cây danh mục phân cấp (Tree View)
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResult<IEnumerable<CategoryTreeDto>>>> GetTree(CancellationToken cancellationToken = default)
    {
        var tree = await _categoryService.GetTreeAsync(cancellationToken);
        return Ok(ApiResult<IEnumerable<CategoryTreeDto>>.Success(tree, "Lấy cây danh mục thành công"));
    }

    /// <summary>
    /// Lấy danh sách danh mục rút gọn cho Dropdown
    /// </summary>
    [HttpGet("dropdown")]
    public async Task<ActionResult<ApiResult<IEnumerable<CategoryDropdownDto>>>> GetDropdown(CancellationToken cancellationToken = default)
    {
        var dropdown = await _categoryService.GetDropdownAsync(cancellationToken);
        return Ok(ApiResult<IEnumerable<CategoryDropdownDto>>.Success(dropdown, "Lấy danh sách dropdown thành công"));
    }

    /// <summary>
    /// Tìm kiếm danh mục (Hỗ trợ tìm tiếng Việt không dấu)
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResult<IEnumerable<CategoryDto>>>> Search(
        [FromQuery] string q,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var results = await _categoryService.SearchAsync(q ?? string.Empty, includeInactive, cancellationToken);
        return Ok(ApiResult<IEnumerable<CategoryDto>>.Success(results, "Tìm kiếm danh mục thành công"));
    }

    /// <summary>
    /// Chi tiết danh mục theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound(ApiResult<CategoryDto>.Failure($"Không tìm thấy danh mục có ID = {id}"));
        }
        return Ok(ApiResult<CategoryDto>.Success(category, "Lấy thông tin danh mục thành công"));
    }

    /// <summary>
    /// Chi tiết danh mục theo Slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> GetBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetBySlugAsync(slug, cancellationToken);
        if (category == null)
        {
            return NotFound(ApiResult<CategoryDto>.Failure($"Không tìm thấy danh mục với Slug = '{slug}'"));
        }
        return Ok(ApiResult<CategoryDto>.Success(category, "Lấy thông tin danh mục theo Slug thành công"));
    }

    /// <summary>
    /// Danh sách sản phẩm thuộc danh mục
    /// </summary>
    [HttpGet("{id:int}/products")]
    public async Task<ActionResult<ApiResult<IEnumerable<ProductDto>>>> GetProducts(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _categoryService.GetProductsByCategoryIdAsync(id, cancellationToken);
            return Ok(ApiResult<IEnumerable<ProductDto>>.Success(products, "Lấy danh sách sản phẩm thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<IEnumerable<ProductDto>>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tạo mới danh mục
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResult<CategoryDto>>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _categoryService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, ApiResult<CategoryDto>.Success(created, "Tạo danh mục thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<CategoryDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<CategoryDto>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CategoryDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật danh mục
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> Update(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResult<CategoryDto>.Success(updated, "Cập nhật danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CategoryDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<CategoryDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<CategoryDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xóa danh mục (Soft Delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _categoryService.SoftDeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Xóa danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Khôi phục danh mục đã xóa mềm
    /// </summary>
    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> Restore(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var restored = await _categoryService.RestoreAsync(id, cancellationToken);
            return Ok(ApiResult<CategoryDto>.Success(restored, "Khôi phục danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CategoryDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Di chuyển danh mục sang danh mục cha mới
    /// </summary>
    [HttpPost("{id:int}/move-parent")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> MoveParent(
        int id,
        [FromBody] MoveParentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _categoryService.MoveParentAsync(id, request.ParentId, cancellationToken);
            return Ok(ApiResult<CategoryDto>.Success(updated, "Di chuyển danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CategoryDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<CategoryDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Di chuyển toàn bộ sản phẩm từ danh mục nguồn sang danh mục đích (Many-to-Many join table update)
    /// </summary>
    [HttpPost("move-products")]
    public async Task<ActionResult<ApiResult<bool>>> MoveProducts(
        [FromBody] MoveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _categoryService.MoveCategoryProductsAsync(request, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Chuyển sản phẩm giữa các danh mục thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Sắp xếp thứ tự danh mục (OrderIndex)
    /// </summary>
    [HttpPatch("{id:int}/reorder")]
    public async Task<ActionResult<ApiResult<CategoryDto>>> Reorder(
        int id,
        [FromBody] ReorderCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _categoryService.ReorderAsync(id, request.OrderIndex, cancellationToken);
            return Ok(ApiResult<CategoryDto>.Success(updated, "Thay đổi thứ tự danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CategoryDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật ảnh danh mục
    /// </summary>
    [HttpPost("{id:int}/image")]
    public async Task<ActionResult<ApiResult<string>>> UploadImage(
        int id,
        [FromBody] string imageUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await _categoryService.UploadImageAsync(id, imageUrl, cancellationToken);
            return Ok(ApiResult<string>.Success(url, "Cập nhật ảnh danh mục thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<string>.Failure(ex.Message));
        }
    }
}
