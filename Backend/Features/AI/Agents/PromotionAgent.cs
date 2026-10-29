using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.Agents.Common;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

namespace SmartSupermarket.Backend.Features.AI.Agents;

public class PromotionAgent : BaseAgent
{
    public override string AgentName => "PromotionAgent";
    public override string Role => "Chuyên viên tối ưu ưu đãi & khuyến mãi";

    public PromotionAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<PromotionAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PromotionAgent] Tra cứu chương trình khuyến mãi cho session {SessionId}", context.SessionId);

        // 1. Gọi PromotionTool lấy vouchers & flash sale song song qua ToolDispatcher
        var vouchersTask = CallToolAsync<List<Promotion>>(
            "PromotionTool.GetVouchers",
            new Dictionary<string, object>(),
            cancellationToken);

        var flashSalesTask = CallToolAsync<List<NearExpiryBatchDto>>(
            "PromotionTool.CheckFlashSale",
            new Dictionary<string, object>(),
            cancellationToken);

        await Task.WhenAll(vouchersTask, flashSalesTask);

        var vouchers = vouchersTask.Result.Data ?? new List<Promotion>();
        var flashSales = flashSalesTask.Result.Data ?? new List<NearExpiryBatchDto>();

        // 2. Tính toán chiết khấu cho các sản phẩm trong mâm cơm (nếu có)
        var lastMeal = GetMemory<MealPlanChatResponse>(context, "LastMealPlan");
        var items = lastMeal?.Ingredients ?? new List<MealIngredientItemDto>();
        decimal calculatedDiscount = 0m;

        if (items.Count > 0)
        {
            var topVoucher = vouchers.FirstOrDefault()?.PromotionCode ?? "FRESH20";
            var discountCall = await CallToolAsync<decimal>(
                "PromotionTool.CalculateDiscount",
                new Dictionary<string, object>
                {
                    ["items"] = items,
                    ["voucherCode"] = topVoucher
                },
                cancellationToken);

            calculatedDiscount = discountCall.Data;
        }

        // 3. Định dạng câu trả lời thân thiện, truyền tải tinh thần săn deal thông minh
        var sb = new StringBuilder();
        sb.AppendLine("Tin vui ưu đãi hôm nay dành cho bạn:");

        if (flashSales.Count > 0)
        {
            sb.AppendLine("\n**Chương trình Flash Sale thực phẩm tươi hôm nay (Giảm 30% - 50%):**");
            foreach (var fs in flashSales.Take(5))
            {
                sb.AppendLine($"- **{fs.ProductName}**: Giảm {fs.DiscountPercent}% chỉ còn **{fs.FlashSalePrice:N0} đ** (Hạn dùng còn {fs.DaysRemaining} ngày). Thực phẩm tươi sạch trong ngày, giữ trọn độ tươi ngon nếu bạn chế biến ngay!");
            }
        }

        if (vouchers.Count > 0)
        {
            sb.AppendLine("\n**Voucher giảm giá áp dụng khi thanh toán:**");
            foreach (var v in vouchers.Take(3))
            {
                var discountDesc = v.DiscountType == "Percentage" ? $"{v.DiscountValue}%" : $"{v.DiscountValue:N0} đ";
                sb.AppendLine($"- Mã **{v.PromotionCode}**: Giảm {discountDesc} cho đơn hàng từ {v.MinimumOrderAmount:N0} đ.");
            }
        }

        if (calculatedDiscount > 0)
        {
            sb.AppendLine($"\n*Mẹo tiết kiệm*: Khi mua mâm cơm hôm nay, bạn sẽ tiết kiệm được **{calculatedDiscount:N0} đ** nhờ kết hợp Flash Sale và voucher!");
        }

        var resultData = new
        {
            Vouchers = vouchers,
            FlashSales = flashSales,
            EstimatedDiscount = calculatedDiscount
        };

        SetMemory(context, "LastPromotionData", resultData);
        return AgentResultDto.Success(AgentName, sb.ToString(), resultData);
    }
}
