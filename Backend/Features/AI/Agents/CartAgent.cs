using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Agents.Common;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

namespace SmartSupermarket.Backend.Features.AI.Agents;

public class CartAgent : BaseAgent
{
    public override string AgentName => "CartAgent";
    public override string Role => "Trợ lý thao tác giỏ hàng 1-Click";

    public CartAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<CartAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var rawPrompt = context.UserPrompt?.Trim() ?? string.Empty;
        _logger.LogInformation("[CartAgent] Xử lý giỏ hàng cho session {SessionId}", context.SessionId);

        var lastMeal = GetMemory<MealPlanChatResponse>(context, "LastMealPlan");
        var items = lastMeal?.Ingredients ?? new List<MealIngredientItemDto>();

        // 1. Nếu yêu cầu thêm giỏ hàng (1-Click Add Cart)
        if (rawPrompt.Contains("thêm", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("chọn mâm", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("lấy", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("mua", StringComparison.OrdinalIgnoreCase))
        {
            if (items.Count == 0)
            {
                return AgentResultDto.Success(
                    AgentName,
                    "Hiện tại chưa có mâm cơm nào được chọn trong phiên trò chuyện. Bạn hãy bảo mình gợi ý món ăn trước nhé!",
                    false);
            }

            var addToolCall = await CallToolAsync<bool>(
                "CartTool.AddToCart",
                new Dictionary<string, object>
                {
                    ["sessionId"] = context.SessionId,
                    ["items"] = items
                },
                cancellationToken);

            var estimateToolCall = await CallToolAsync<CartEstimationDto>(
                "CartTool.EstimateCart",
                new Dictionary<string, object>
                {
                    ["items"] = items,
                    ["voucherCode"] = "FRESH20"
                },
                cancellationToken);

            var estimation = estimateToolCall.Data;
            var sb = new StringBuilder();
            sb.AppendLine($"Đã thêm thành công **{items.Count} nguyên liệu** cho món **{lastMeal?.DishName}** vào giỏ hàng của bạn!");
            if (estimation != null)
            {
                sb.AppendLine($"- **Tạm tính**: {estimation.FinalTotal:N0} đ");
                if (estimation.DiscountAmount > 0)
                {
                    sb.AppendLine($"- **Tiết kiệm**: {estimation.DiscountAmount:N0} đ");
                }
            }
            sb.AppendLine("\nBạn có thể mở giỏ hàng để kiểm tra hoặc tiến hành thanh toán bất cứ khi nào bạn sẵn sàng!");

            return AgentResultDto.Success(AgentName, sb.ToString(), addToolCall.Data);
        }

        // 2. Mặc định: Ước tính tổng tiền giỏ hàng hiện tại
        var estimateCall = await CallToolAsync<CartEstimationDto>(
            "CartTool.EstimateCart",
            new Dictionary<string, object>
            {
                ["items"] = items,
                ["voucherCode"] = "FRESH20"
            },
            cancellationToken);

        var cartEstimate = estimateCall.Data ?? new CartEstimationDto
        {
            SubTotal = items.Sum(i => i.RegularPrice * i.QuantityToBuy),
            FinalTotal = items.Sum(i => i.FinalPrice * i.QuantityToBuy),
            Items = items
        };

        var estimateSb = new StringBuilder();
        estimateSb.AppendLine($"Tổng hợp giỏ hàng tạm tính của bạn:");
        estimateSb.AppendLine($"- Số lượng món: {cartEstimate.TotalItemCount}");
        estimateSb.AppendLine($"- Tổng tiền gốc: {cartEstimate.SubTotal:N0} đ");
        estimateSb.AppendLine($"- Giảm giá: {cartEstimate.DiscountAmount:N0} đ");
        estimateSb.AppendLine($"- **Tổng thanh toán**: {cartEstimate.FinalTotal:N0} đ");

        return AgentResultDto.Success(AgentName, estimateSb.ToString(), cartEstimate);
    }
}
