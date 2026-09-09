using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Services;

namespace SmartSupermarket.Backend.Features.Products.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lấy tất cả danh mục sản phẩm (hoặc phân trang / tìm kiếm)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var paged = await _categoryService.GetPagedAsync(search, page, pageSize, cancellationToken);
            return Ok(ApiResult<CategoryPagedResult>.Success(paged, "Lấy danh sách danh mục thành công."));
        }

        var categories = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(ApiResult<IEnumerable<CategoryDto>>.Success(categories, "Lấy tất cả danh mục thành công."));
    }

    /// <summary>
    /// Lấy chi tiết 1 danh mục sản phẩm theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound(ApiResult<CategoryDto?>.Failure($"Không tìm thấy danh mục có ID = {id}."));
        }

        return Ok(ApiResult<CategoryDto>.Success(category, "Lấy thông tin danh mục thành công."));
    }

    /// <summary>
    /// Tạo mới Danh mục sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _categoryService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.CategoryId },
                ApiResult<CategoryDto>.Success(created, "Tạo mới danh mục thành công."));
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
    /// Cập nhật thông tin Danh mục sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResult<CategoryDto>.Success(updated, "Cập nhật danh mục thành công."));
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
    /// Xóa Danh mục sản phẩm (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _categoryService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(true, "Xóa danh mục thành công."));
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
}
