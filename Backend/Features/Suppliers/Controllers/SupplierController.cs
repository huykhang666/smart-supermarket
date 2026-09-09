using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Suppliers.DTOs;
using SmartSupermarket.Backend.Features.Suppliers.Services;

namespace SmartSupermarket.Backend.Features.Suppliers.Controllers;

[ApiController]
[Route("api/v1/suppliers")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Lấy danh sách Nhà cung cấp (Phân trang, Tìm kiếm, Lọc trạng thái)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<SupplierPagedResult>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] byte? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.GetPagedAsync(search, status, page, pageSize, cancellationToken);
        return Ok(ApiResult<SupplierPagedResult>.Success(result, "Lấy danh sách nhà cung cấp thành công."));
    }

    /// <summary>
    /// Lấy danh sách rút gọn Nhà cung cấp cho Dropdown
    /// </summary>
    [HttpGet("dropdown")]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<IEnumerable<SupplierDropdownDto>>>> GetDropdown(CancellationToken cancellationToken = default)
    {
        var dropdown = await _supplierService.GetDropdownAsync(cancellationToken);
        return Ok(ApiResult<IEnumerable<SupplierDropdownDto>>.Success(dropdown, "Lấy danh sách dropdown thành công."));
    }

    /// <summary>
    /// Tìm kiếm Nhà cung cấp (Hỗ trợ tiếng Việt không dấu)
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<IEnumerable<SupplierDto>>>> Search(
        [FromQuery] string q,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var results = await _supplierService.SearchAsync(q ?? string.Empty, includeInactive, cancellationToken);
        return Ok(ApiResult<IEnumerable<SupplierDto>>.Success(results, "Tìm kiếm nhà cung cấp thành công."));
    }

    /// <summary>
    /// Báo cáo thống kê tổng quan Phân hệ Nhà cung cấp
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<SupplierStatisticsDto>>> GetStatistics(CancellationToken cancellationToken = default)
    {
        var stats = await _supplierService.GetSupplierStatisticsAsync(cancellationToken);
        return Ok(ApiResult<SupplierStatisticsDto>.Success(stats, "Lấy thống kê nhà cung cấp thành công."));
    }

    /// <summary>
    /// Chi tiết Nhà cung cấp theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<SupplierDto>>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            return NotFound(ApiResult<SupplierDto>.Failure($"Không tìm thấy nhà cung cấp có ID = {id}."));
        }
        return Ok(ApiResult<SupplierDto>.Success(supplier, "Lấy thông tin nhà cung cấp thành công."));
    }

    /// <summary>
    /// Danh sách sản phẩm do Nhà cung cấp phân phối (Many-to-Many)
    /// </summary>
    [HttpGet("{id:int}/products")]
    [Authorize(Roles = "Admin,Manager,Staff,AI")]
    public async Task<ActionResult<ApiResult<IEnumerable<ProductSupplierDto>>>> GetProducts(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _supplierService.GetSuppliedProductsBySupplierIdAsync(id, cancellationToken);
            return Ok(ApiResult<IEnumerable<ProductSupplierDto>>.Success(products, "Lấy danh sách sản phẩm thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<IEnumerable<ProductSupplierDto>>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tạo mới Nhà cung cấp
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<SupplierDto>>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _supplierService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.SupplierId }, ApiResult<SupplierDto>.Success(created, "Tạo mới nhà cung cấp thành công."));
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
    /// Cập nhật thông tin Nhà cung cấp
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<SupplierDto>>> Update(
        int id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
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
    /// Xóa mềm Nhà cung cấp (Chuyển Status = 2 Inactive)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _supplierService.SoftDeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Chuyển trạng thái nhà cung cấp sang Tạm ngừng hợp tác thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Khôi phục Nhà cung cấp đã xóa mềm (Chuyển Status = 1 Active)
    /// </summary>
    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<SupplierDto>>> Restore(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var restored = await _supplierService.RestoreAsync(id, cancellationToken);
            return Ok(ApiResult<SupplierDto>.Success(restored, "Khôi phục nhà cung cấp thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<SupplierDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật Logo Nhà cung cấp
    /// </summary>
    [HttpPost("{id:int}/logo")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<string>>> UploadLogo(
        int id,
        [FromBody] string logoUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await _supplierService.UploadLogoAsync(id, logoUrl, cancellationToken);
            return Ok(ApiResult<string>.Success(url, "Cập nhật logo nhà cung cấp thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<string>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tạo / Cập nhật Liên kết Sản phẩm - NCC (ProductSupplier)
    /// </summary>
    [HttpPost("products/link")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<bool>>> LinkProduct(
        [FromBody] LinkProductSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _supplierService.LinkProductSupplierAsync(request, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Tạo liên kết cung ứng sản phẩm thành công."));
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
    /// Hủy liên kết Sản phẩm khỏi NCC
    /// </summary>
    [HttpDelete("products/unlink")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<bool>>> UnlinkProduct(
        [FromQuery] int productId,
        [FromQuery] int supplierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _supplierService.UnlinkProductSupplierAsync(productId, supplierId, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Đã hủy liên kết sản phẩm thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật thông tin điều khoản liên kết ProductSupplier
    /// </summary>
    [HttpPut("products/update-link")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<bool>>> UpdateLink(
        [FromQuery] int productId,
        [FromQuery] int supplierId,
        [FromBody] UpdateLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _supplierService.UpdateProductSupplierLinkAsync(productId, supplierId, request, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Cập nhật liên kết cung ứng thành công."));
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
    /// Import hàng loạt Nhà cung cấp từ chuỗi JSON
    /// </summary>
    [HttpPost("import/json")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<ImportSupplierResultDto>>> ImportJson(
        [FromBody] string jsonContent,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.ImportFromJsonAsync(jsonContent, cancellationToken);
        return Ok(ApiResult<ImportSupplierResultDto>.Success(result, "Import JSON hoàn tất."));
    }

    /// <summary>
    /// Import hàng loạt Nhà cung cấp từ File Excel / CSV
    /// </summary>
    [HttpPost("import/excel")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResult<ImportSupplierResultDto>>> ImportExcel(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResult<ImportSupplierResultDto>.Failure("Vui lòng chọn file CSV/Excel hợp lệ."));
        }

        using var stream = file.OpenReadStream();
        var result = await _supplierService.ImportFromCsvAsync(stream, cancellationToken);
        return Ok(ApiResult<ImportSupplierResultDto>.Success(result, "Import Excel/CSV hoàn tất."));
    }
}
