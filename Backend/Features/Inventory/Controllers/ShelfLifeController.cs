using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;
using SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;
using SmartSupermarket.Backend.Features.Inventory.Services;

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

    /// <summary>
    /// Kích hoạt quét hạn sử dụng & điều chỉnh giá tự động (Manual Trigger dành cho Demo / Quản trị viên).
    /// </summary>
    [HttpPost("trigger-scan")]
    [AllowAnonymous] // Phục vụ chạy thử nghiệm và demo kiểm thử
    public async Task<ActionResult<ApiResult<ShelfLifeScanResponse>>> TriggerScan(
        [FromBody] ShelfLifeScanRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new ShelfLifeScanRequest();
        var result = await _shelfLifeProcessor.ExecuteShelfLifeAndPricingScanAsync(request, cancellationToken);
        return Ok(ApiResult<ShelfLifeScanResponse>.Success(result, "Kích hoạt quét hạn sử dụng và điều chỉnh giá thành công."));
    }
}
