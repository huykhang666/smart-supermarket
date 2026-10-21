using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;
using SmartSupermarket.Backend.Features.Inventory.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.Inventory.Controllers;

[ApiController]
[Route("api/v1/inventory/shelf-life")]
public class ShelfLifeController : ControllerBase
{
    private readonly IShelfLifeProcessor _shelfLifeProcessor;

    public ShelfLifeController(IShelfLifeProcessor shelfLifeProcessor)
    {
        _shelfLifeProcessor = shelfLifeProcessor;
    }

    // Lấy danh sách sản phẩm cận hạn & hết hạn (chỉ đọc, không sửa DB)
    [HttpGet("near-expiry")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<ShelfLifeScanResponse>>> GetNearExpiry(
        [FromQuery] bool isEveningFlashSale = false,
        [FromQuery] int branchId = 1,
        CancellationToken cancellationToken = default)
    {
        var request = new ShelfLifeScanRequest
        {
            IsEveningFlashSale = isEveningFlashSale,
            BranchId = branchId,
            DryRun = true
        };

        var result = await _shelfLifeProcessor.ExecuteShelfLifeAndPricingScanAsync(request, cancellationToken);
        return Ok(ApiResult<ShelfLifeScanResponse>.Success(result, "Lấy danh sách hàng cận hạn thành công."));
    }

    // Kích hoạt quét thực tế và cập nhật giá / khóa bán
    [HttpPost("trigger-scan")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<ShelfLifeScanResponse>>> TriggerScan(
        [FromBody] ShelfLifeScanRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new ShelfLifeScanRequest();

        var result = await _shelfLifeProcessor.ExecuteShelfLifeAndPricingScanAsync(request, cancellationToken);
        return Ok(ApiResult<ShelfLifeScanResponse>.Success(result, "Kích hoạt quét hạn sử dụng và điều chỉnh giá thành công."));
    }
}
