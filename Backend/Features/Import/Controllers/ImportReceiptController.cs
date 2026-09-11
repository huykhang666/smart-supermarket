using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Import.DTOs;
using SmartSupermarket.Backend.Features.Import.Services;
using System.Security.Claims;

namespace SmartSupermarket.Backend.Features.Import.Controllers;

[ApiController]
[Route("api/import-receipts")]
[Authorize(Roles = "Admin,Manager")]
public class ImportReceiptController : ControllerBase
{
    private readonly IImportReceiptService _receiptService;

    public ImportReceiptController(IImportReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && int.TryParse(idClaim.Value, out int id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetReceipts([FromQuery] int? branchId, [FromQuery] int? supplierId, [FromQuery] ImportStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _receiptService.GetImportReceiptsAsync(branchId, supplierId, status, null, null, page, pageSize);
        return Ok(ApiResult<object>.Success(new { Items = items, TotalCount = total }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReceiptById(int id)
    {
        var receipt = await _receiptService.GetImportReceiptByIdAsync(id);
        if (receipt == null) return NotFound(ApiResult<object>.Failure("Không tìm thấy phiếu nhập."));
        return Ok(ApiResult<ImportReceipt>.Success(receipt));
    }

    [HttpPost]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateImportReceiptRequest request)
    {
        var receipt = await _receiptService.CreateImportReceiptAsync(request.SupplierId, request.BranchId, request.Note, GetCurrentUserId());
        return Ok(ApiResult<ImportReceipt>.Success(receipt, "Tạo phiếu nháp thành công."));
    }

    [HttpPost("{id}/details")]
    public async Task<IActionResult> AddDetail(int id, [FromBody] AddImportDetailRequest request)
    {
        try
        {
            var detail = await _receiptService.AddImportDetailAsync(id, request.ProductId, request.Quantity, request.CostPrice, request.ExpiryDate, GetCurrentUserId());
            return Ok(ApiResult<ImportDetail>.Success(detail, "Thêm sản phẩm thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<object>.Failure(ex.Message));
        }
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmReceipt(int id)
    {
        try
        {
            await _receiptService.ConfirmImportReceiptAsync(id, GetCurrentUserId());
            return Ok(ApiResult<bool>.Success(true, "Xác nhận phiếu và cộng kho thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelReceipt(int id)
    {
        try
        {
            await _receiptService.CancelImportReceiptAsync(id, GetCurrentUserId());
            return Ok(ApiResult<bool>.Success(true, "Hủy phiếu thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }
}
