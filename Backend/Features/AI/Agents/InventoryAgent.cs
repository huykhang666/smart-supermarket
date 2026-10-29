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
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

namespace SmartSupermarket.Backend.Features.AI.Agents;

public class InventoryAgent : BaseAgent
{
    public override string AgentName => "InventoryAgent";
    public override string Role => "Quản trị tồn kho & Kiểm soát lô hàng cận date";

    public InventoryAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<InventoryAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var rawPrompt = context.UserPrompt?.Trim() ?? string.Empty;
        var intent = (context.Intent ?? string.Empty).ToLowerInvariant();

        _logger.LogInformation("[InventoryAgent] Bắt đầu kiểm tra kho cho session {SessionId}", context.SessionId);

        // 1. Kiểm tra lô hàng cận date nếu yêu cầu kiểm tra date / flash sale
        if (intent.Contains("nearexpiry") || intent.Contains("expiry") ||
            rawPrompt.Contains("cận date", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("hạn dùng", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("hết hạn", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleNearExpiryAuditAsync(context, cancellationToken);
        }

        // 2. Mặc định: Tra cứu tồn kho theo ProductId hoặc danh sách sản phẩm trong context
        return await HandleStockAuditAsync(context, cancellationToken);
    }

    private async Task<AgentResultDto> HandleNearExpiryAuditAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var toolCall = await CallToolAsync<List<NearExpiryBatchDto>>(
            "InventoryTool.GetNearExpiry",
            new Dictionary<string, object> { ["daysThreshold"] = 3 },
            cancellationToken);

        var batches = toolCall.Data ?? new List<NearExpiryBatchDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Báo cáo kiểm soát tồn kho các lô hàng cận date (dưới 3 ngày):");

        if (batches.Count == 0)
        {
            sb.AppendLine("- Toàn bộ các lô hàng trong kho đều đang ở trạng thái date an toàn tốt.");
        }
        else
        {
            foreach (var b in batches)
            {
                sb.AppendLine($"- Lô **{b.BatchCode}** ({b.ProductName}): Còn {b.Quantity} sản phẩm, hạn dùng còn {b.DaysRemaining} ngày (Đủ điều kiện Flash Sale giảm {b.DiscountPercent}%).");
            }
        }

        SetMemory(context, "LastNearExpiryBatches", batches);
        return AgentResultDto.Success(AgentName, sb.ToString(), batches);
    }

    private async Task<AgentResultDto> HandleStockAuditAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var cartProductIds = context.CurrentCartItemProductIds ?? new List<int>();
        var results = new List<object>();
        var sb = new StringBuilder();

        sb.AppendLine("Báo cáo tình trạng tồn kho sản phẩm:");

        if (cartProductIds.Count == 0)
        {
            // Kiểm tra tổng quát lô hàng cận date để báo cáo
            var nearExpiryCall = await CallToolAsync<List<NearExpiryBatchDto>>(
                "InventoryTool.GetNearExpiry",
                new Dictionary<string, object> { ["daysThreshold"] = 3 },
                cancellationToken);

            var nearExpiryList = nearExpiryCall.Data ?? new List<NearExpiryBatchDto>();
            sb.AppendLine($"- Kho đang vận hành ổn định. Phát hiện {nearExpiryList.Count} lô hàng cận date cần ưu tiên đẩy bán.");
            return AgentResultDto.Success(AgentName, sb.ToString(), nearExpiryList);
        }

        foreach (var productId in cartProductIds)
        {
            var stockCall = await CallToolAsync<int>(
                "InventoryTool.GetStock",
                new Dictionary<string, object> { ["productId"] = productId },
                cancellationToken);

            int stock = stockCall.Data;
            Product? altProduct = null;

            if (stock <= 0)
            {
                var altCall = await CallToolAsync<Product?>(
                    "InventoryTool.GetAlternativeProduct",
                    new Dictionary<string, object> { ["outOfStockProductId"] = productId },
                    cancellationToken);

                altProduct = altCall.Data;
            }

            results.Add(new
            {
                ProductId = productId,
                StockQuantity = stock,
                InStock = stock > 0,
                AlternativeProduct = altProduct
            });

            if (stock > 0)
            {
                sb.AppendLine($"- Sản phẩm ID {productId}: Còn hàng trong kho ({stock} cái/hộp).");
            }
            else
            {
                var altMsg = altProduct != null ? $", đề xuất đổi sang '{altProduct.ProductName}'" : "";
                sb.AppendLine($"- Sản phẩm ID {productId}: Tạm hết hàng{altMsg}.");
            }
        }

        return AgentResultDto.Success(AgentName, sb.ToString(), results);
    }
}
