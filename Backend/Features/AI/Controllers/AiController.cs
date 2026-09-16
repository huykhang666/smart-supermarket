using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Features.AI.Services;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("import-forecast")]
    public async Task<ActionResult<ApiResult<AiResponse>>> ImportForecast(
        [FromBody] AiImportForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiService.GenerateImportForecastAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI dự báo nhập hàng thành công"));
    }

    [HttpPost("revenue-report")]
    public async Task<ActionResult<ApiResult<AiResponse>>> RevenueReport(
        [FromBody] AiRevenueReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiService.GenerateRevenueReportAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI tạo báo cáo doanh thu thành công"));
    }

    [HttpPost("product-analysis")]
    public async Task<ActionResult<ApiResult<AiResponse>>> ProductAnalysis(
        [FromBody] AiProductAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiService.GenerateProductAnalysisAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI phân tích sản phẩm thành công"));
    }
}
