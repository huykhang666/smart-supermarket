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
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Lấy tất cả nhà cung cấp (hoặc phân trang / tìm kiếm)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var paged = await _supplierService.GetPagedAsync(search, page, pageSize, cancellationToken);
            return Ok(ApiResult<SupplierPagedResult>.Success(paged, "Lấy danh sách nhà cung cấp thành công."));
        }

        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        return Ok(ApiResult<IEnumerable<SupplierDto>>.Success(suppliers, "Lấy tất cả nhà cung cấp thành công."));
    }

    /// <summary>
    /// Lấy chi tiết 1 nhà cung cấp theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            return NotFound(ApiResult<SupplierDto?>.Failure($"Không tìm thấy nhà cung cấp có ID = {id}."));
        }

        return Ok(ApiResult<SupplierDto>.Success(supplier, "Lấy thông tin nhà cung cấp thành công."));
    }

    /// <summary>
    /// Tạo mới Nhà cung cấp (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _supplierService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.SupplierId },
                ApiResult<SupplierDto>.Success(created, "Tạo mới nhà cung cấp thành công."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<SupplierDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<SupplierDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật thông tin Nhà cung cấp (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<SupplierDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _supplierService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResult<SupplierDto>.Success(updated, "Cập nhật nhà cung cấp thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<SupplierDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<SupplierDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<SupplierDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xóa Nhà cung cấp (Yêu cầu Role Admin hoặc Manager)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _supplierService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(true, "Xóa nhà cung cấp thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }
}
