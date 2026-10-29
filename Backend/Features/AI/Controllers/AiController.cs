using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.Agents;
using SmartSupermarket.Backend.Features.AI.Agents.Common;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Orchestration;
using SmartSupermarket.Backend.Features.AI.Services;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly IAiShoppingConciergeService _shoppingConciergeService;
    private readonly ShoppingAgent _shoppingAgent;
    private readonly IEnumerable<IAgent> _agents;
    private readonly IAiService _legacyAiService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAgentOrchestrator agentOrchestrator,
        IAiShoppingConciergeService shoppingConciergeService,
        ShoppingAgent shoppingAgent,
        IEnumerable<IAgent> agents,
        IAiService legacyAiService,
        ILogger<AiController> logger)
    {
        _agentOrchestrator = agentOrchestrator;
        _shoppingConciergeService = shoppingConciergeService;
        _shoppingAgent = shoppingAgent;
        _agents = agents;
        _legacyAiService = legacyAiService;
        _logger = logger;
    }

    /// <summary>
    /// Tiếp nhận hội thoại tự nhiên từ khách hàng và điều phối xử lý qua AgentOrchestrator.
    /// </summary>
    [HttpPost("chat")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AiChatResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    public async Task<ActionResult<ApiResult<AiChatResponseDto>>> Chat(
        [FromBody] AiChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiResult<AiChatResponseDto>.Failure("Nội dung tin nhắn không được để trống."));
        }

        try
        {
            var response = await _agentOrchestrator.DispatchAsync(request, cancellationToken);
            return Ok(ApiResult<AiChatResponseDto>.Success(response, "Xử lý hội thoại AI thành công."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiController] Lỗi xử lý chat cho session: {SessionId}", request.SessionId);
            return StatusCode(500, ApiResult<AiChatResponseDto>.Failure($"Lỗi máy chủ nội bộ: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lập mâm cơm và giỏ hàng chuẩn vị Cookpad theo ngân sách và số người ăn.
    /// </summary>
    [HttpPost("shopping/meal-plan")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<MealPlanChatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    public async Task<ActionResult<ApiResult<MealPlanChatResponse>>> PlanMeal(
        [FromBody] MealPlanChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            return BadRequest(ApiResult<MealPlanChatResponse>.Failure("Yêu cầu tư vấn món ăn không được để trống."));
        }

        try
        {
            var response = await _shoppingConciergeService.PlanMealAndShopAsync(request, cancellationToken);
            return Ok(ApiResult<MealPlanChatResponse>.Success(response, "Tư vấn thực đơn & giỏ hàng đi chợ thành công."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiController] Lỗi khi tạo mâm cơm");
            return StatusCode(500, ApiResult<MealPlanChatResponse>.Failure($"Lỗi khi tạo mâm cơm: {ex.Message}"));
        }
    }

    /// <summary>
    /// Nạp nhanh 1-Click toàn bộ nguyên liệu mâm cơm vào giỏ hàng của phiên làm việc.
    /// </summary>
    [HttpPost("cart/inject")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    public async Task<ActionResult<ApiResult<object>>> InjectCart(
        [FromBody] AiCartInjectRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(ApiResult<object>.Failure("SessionId không được để trống."));
        }

        try
        {
            var context = new AgentContextDto
            {
                SessionId = request.SessionId,
                Intent = "AddToCart",
                UserPrompt = "thêm vào giỏ",
                CurrentCartItemProductIds = request.ProductIds ?? new List<int>()
            };

            var result = await _shoppingAgent.ExecuteAsync(context, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResult<object>.Failure(result.ErrorMessage ?? "Không thể thêm vào giỏ hàng."));
            }

            return Ok(ApiResult.Success(result.Data, result.Output));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiController] Lỗi nạp giỏ hàng cho session: {SessionId}", request.SessionId);
            return StatusCode(500, ApiResult<object>.Failure($"Lỗi máy chủ nội bộ: {ex.Message}"));
        }
    }

    /// <summary>
    /// Thực thi trực tiếp một tác nhân AI chuyên biệt (ShoppingAgent, InventoryAgent, PromotionAgent, RecipeAgent, BudgetAgent, CartAgent).
    /// </summary>
    [HttpPost("agent/execute")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AgentResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    public async Task<ActionResult<ApiResult<AgentResultDto>>> ExecuteAgent(
        [FromBody] AiAgentExecuteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(ApiResult<AgentResultDto>.Failure("Prompt yêu cầu tác nhân không được để trống."));
        }

        var targetAgentName = string.IsNullOrWhiteSpace(request.AgentName) ? "ShoppingAgent" : request.AgentName;
        var agent = _agents.FirstOrDefault(a => a.AgentName.Equals(targetAgentName, StringComparison.OrdinalIgnoreCase)) ?? _shoppingAgent;

        var context = new AgentContextDto
        {
            SessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId,
            UserPrompt = request.Prompt,
            Intent = request.Intent ?? "General",
            Servings = request.Servings ?? 2,
            TargetBudget = request.TargetBudget ?? 200000m,
            CurrentCartItemProductIds = request.ProductIds ?? new List<int>()
        };

        try
        {
            var result = await agent.ExecuteAsync(context, cancellationToken);
            return Ok(ApiResult<AgentResultDto>.Success(result, result.Output));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiController] Lỗi thực thi tác nhân {Agent}", targetAgentName);
            return StatusCode(500, ApiResult<AgentResultDto>.Failure($"Lỗi thực thi tác nhân: {ex.Message}"));
        }
    }

    /// <summary>
    /// Dự báo nhập hàng tự động theo dữ liệu lịch sử xuất bán.
    /// </summary>
    [HttpPost("import-forecast")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiResponse>>> ImportForecast(
        [FromBody] AiImportForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _legacyAiService.GenerateImportForecastAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI dự báo nhập hàng thành công"));
    }

    /// <summary>
    /// Tạo báo cáo phân tích doanh thu bán hàng thông minh.
    /// </summary>
    [HttpPost("revenue-report")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiResponse>>> RevenueReport(
        [FromBody] AiRevenueReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _legacyAiService.GenerateRevenueReportAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI tạo báo cáo doanh thu thành công"));
    }

    /// <summary>
    /// Phân tích hiệu suất tiêu thụ và tỷ suất lợi nhuận sản phẩm.
    /// </summary>
    [HttpPost("product-analysis")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiResponse>>> ProductAnalysis(
        [FromBody] AiProductAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _legacyAiService.GenerateProductAnalysisAsync(request, cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI phân tích sản phẩm thành công"));
    }

    /// <summary>
    /// Đề xuất mức chiết khấu tối ưu cho các sản phẩm cận hạn dùng.
    /// </summary>
    [HttpPost("expiry-markdown")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<AiResponse>>> ExpiryMarkdown(CancellationToken cancellationToken = default)
    {
        var result = await _legacyAiService.GenerateExpiryMarkdownAsync(cancellationToken);
        return Ok(ApiResult<AiResponse>.Success(result, "AI đề xuất giảm giá hàng cận date thành công"));
    }
}
