using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Features.AI.Services;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize(Roles = "Admin,Manager")]
public class AiRetailController : ControllerBase
{
    private readonly IAiRetailService _aiRetailService;

    public AiRetailController(IAiRetailService aiRetailService)
    {
        _aiRetailService = aiRetailService;
    }

    // Phân tích rủi ro kho hàng và dự báo đứt hàng
    [HttpPost("retail/product-risks")]
    public async Task<ActionResult<ApiResult<AiProductRiskReport>>> AnalyzeProductRisks(CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.AnalyzeProductRisksAsync(cancellationToken);
        return Ok(ApiResult<AiProductRiskReport>.Success(result, "Phân tích rủi ro sản phẩm bằng AI thành công."));
    }

    // Phân tích rổ hàng và gợi ý combo bán kèm
    [HttpPost("retail/market-basket")]
    public async Task<ActionResult<ApiResult<AiMarketBasketReport>>> AnalyzeMarketBasket(
        [FromQuery] int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.AnalyzeMarketBasketAsync(daysBack, cancellationToken);
        return Ok(ApiResult<AiMarketBasketReport>.Success(result, "Phân tích rổ hàng và gợi ý combo thành công."));
    }

    // Đề xuất phiếu nhập hàng nháp
    [HttpPost("retail/draft-import-order")]
    public async Task<ActionResult<ApiResult<AiDraftImportOrder>>> GenerateDraftImportOrder(
        [FromQuery] int supplierId = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.GenerateDraftImportOrderAsync(supplierId, cancellationToken);
        return Ok(ApiResult<AiDraftImportOrder>.Success(result, "Tạo phiếu nhập kho nháp bằng AI thành công."));
    }

    // 1-Click lưu trực tiếp phiếu nhập hàng nháp vào CSDL
    [HttpPost("retail/commit-draft-import")]
    public async Task<ActionResult<ApiResult<object>>> CommitDraftImportOrder(
        [FromBody] AiDraftImportOrder draftOrder,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _aiRetailService.CommitDraftImportOrderAsync(draftOrder, cancellationToken);
        return Ok(ApiResult<object>.Success(new 
        { 
            receipt.ImportReceiptId, 
            receipt.ReceiptCode, 
            receipt.TotalAmount, 
            receipt.Status 
        }, "Đã lưu phiếu nhập kho thành công vào CSDL."));
    }
}
