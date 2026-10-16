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
public class AiRetailController : ControllerBase
{
    private readonly IAiRetailService _aiRetailService;
    private readonly IAiShoppingConciergeService _shoppingConciergeService;

    public AiRetailController(
        IAiRetailService aiRetailService,
        IAiShoppingConciergeService shoppingConciergeService)
    {
        _aiRetailService = aiRetailService;
        _shoppingConciergeService = shoppingConciergeService;
    }

    /// <summary>
    /// Phân tích rủi ro kho hàng (cận date, tồn kho thấp, hàng ế) và đề xuất mức giảm giá thông minh.
    /// </summary>
    [HttpPost("retail/product-risks")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiProductRiskReport>>> AnalyzeProductRisks(CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.AnalyzeProductRisksAsync(cancellationToken);
        return Ok(ApiResult<AiProductRiskReport>.Success(result, "Phân tích rủi ro sản phẩm bằng AI thành công."));
    }

    /// <summary>
    /// Phân tích rổ hàng (Market Basket Analysis) tìm các cặp sản phẩm thường mua kèm để tạo combo.
    /// </summary>
    [HttpPost("retail/market-basket")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiMarketBasketReport>>> AnalyzeMarketBasket(
        [FromQuery] int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.AnalyzeMarketBasketAsync(daysBack, cancellationToken);
        return Ok(ApiResult<AiMarketBasketReport>.Success(result, "Phân tích rổ hàng và gợi ý combo thành công."));
    }

    /// <summary>
    /// Tự động sinh phiếu nhập hàng nháp (1-Click Draft Import Order) cho các mặt hàng sắp hết.
    /// </summary>
    [HttpPost("retail/draft-import-order")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiDraftImportOrder>>> GenerateDraftImportOrder(
        [FromQuery] int supplierId = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiRetailService.GenerateDraftImportOrderAsync(supplierId, cancellationToken);
        return Ok(ApiResult<AiDraftImportOrder>.Success(result, "Tạo phiếu nhập kho nháp bằng AI thành công."));
    }

    /// <summary>
    /// Trợ lý Đi chợ & Tư vấn Bữa ăn Web AI (AI Shopping Concierge):
    /// Tiếp nhận nhu cầu món ăn + ngân sách -> Tra cứu kho hàng -> Gợi ý mâm nguyên liệu chuẩn khít giá.
    /// </summary>
    [HttpPost("shopping/meal-plan")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<MealPlanChatResponse>>> PlanMealAndShop(
        [FromBody] MealPlanChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _shoppingConciergeService.PlanMealAndShopAsync(request, cancellationToken);
        return Ok(ApiResult<MealPlanChatResponse>.Success(result, "Tư vấn thực đơn & giỏ hàng đi chợ thành công."));
    }
}
