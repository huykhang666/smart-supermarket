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

public class ShoppingAgent : BaseAgent
{
    public override string AgentName => "ShoppingAgent";
    public override string Role => "Chuyên viên tư vấn mua sắm & lập mâm cơm gia đình thông minh";

    public ShoppingAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<ShoppingAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var rawPrompt = context.UserPrompt?.Trim() ?? string.Empty;
        var intent = (context.Intent ?? string.Empty).ToLowerInvariant();

        _logger.LogInformation("[ShoppingAgent] Bắt đầu xử lý cho session {SessionId} với intent: {Intent}",
            context.SessionId, intent);

        if (intent.Contains("addtocart") || intent.Contains("cart") ||
            rawPrompt.Contains("thêm vào giỏ", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("chọn mâm", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("lấy mâm", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleAddToCartAsync(context, cancellationToken);
        }

        if (intent.Contains("productsearch") ||
            rawPrompt.Contains("tìm sản phẩm", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("giá của", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("có bán không", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleProductSearchAsync(context, cancellationToken);
        }

        if (intent.Contains("promotion") ||
            rawPrompt.Contains("khuyến mãi", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("voucher", StringComparison.OrdinalIgnoreCase) ||
            rawPrompt.Contains("giảm giá", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePromotionQueryAsync(context, cancellationToken);
        }

        return await HandleMealPlanningAsync(context, cancellationToken);
    }

    private async Task<AgentResultDto> HandleMealPlanningAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var dishName = ExtractDishName(context.UserPrompt);
        int servings = context.Servings > 0 ? context.Servings : 4;
        decimal targetBudget = context.TargetBudget > 0 ? context.TargetBudget : 300000m;

        _logger.LogInformation("[ShoppingAgent] Lập kế hoạch mâm cơm: {DishName}, khẩu phần: {Servings}, ngân sách: {Budget:N0} đ",
            dishName, servings, targetBudget);

        // 1. Gọi RecipeTool lấy nguyên liệu chuẩn Cookpad qua ToolDispatcher
        var recipeToolCall = await CallToolAsync<List<MealIngredientItemDto>>(
            "RecipeTool.GetIngredients",
            new Dictionary<string, object>
            {
                ["dishName"] = dishName,
                ["servings"] = servings
            },
            cancellationToken);

        var ingredients = recipeToolCall.Data ?? new List<MealIngredientItemDto>();

        // Nếu không có nguyên liệu từ công thức, tìm kiếm sản phẩm theo tên món qua ProductTool
        if (ingredients.Count == 0)
        {
            var searchToolCall = await CallToolAsync<List<Product>>(
                "ProductTool.Search",
                new Dictionary<string, object>
                {
                    ["keyword"] = dishName,
                    ["limit"] = 5
                },
                cancellationToken);

            if (searchToolCall.Data != null && searchToolCall.Data.Count > 0)
            {
                ingredients = searchToolCall.Data.Select(p => new MealIngredientItemDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Barcode = p.Barcode,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    Unit = p.Unit,
                    QuantityToBuy = 1,
                    RegularPrice = p.Price,
                    FinalPrice = p.Price,
                    DiscountPercent = 0,
                    InStockQuantity = 10
                }).ToList();
            }
        }

        // 2. Gọi InventoryTool & PromotionTool song song qua ToolDispatcher
        var nearExpiryTask = CallToolAsync<List<NearExpiryBatchDto>>(
            "InventoryTool.GetNearExpiry",
            new Dictionary<string, object> { ["daysThreshold"] = 3 },
            cancellationToken);

        var vouchersTask = CallToolAsync<List<Promotion>>(
            "PromotionTool.GetVouchers",
            new Dictionary<string, object>(),
            cancellationToken);

        var tipsTask = CallToolAsync<string>(
            "RecipeTool.GetCookingTips",
            new Dictionary<string, object> { ["dishName"] = dishName },
            cancellationToken);

        await Task.WhenAll(nearExpiryTask, vouchersTask, tipsTask);

        var nearExpiryBatches = nearExpiryTask.Result.Data ?? new List<NearExpiryBatchDto>();
        var activeVouchers = vouchersTask.Result.Data ?? new List<Promotion>();
        var cookingTips = tipsTask.Result.Data ?? "Ướp gia vị trước 15 phút để nguyên liệu ngấm đều và giữ trọn độ tươi ngon.";

        // 3. Khớp lô cận date (Flash Sale) & Kiểm tra tồn kho cho từng nguyên liệu qua ToolDispatcher
        decimal totalSaved = 0m;
        var resolvedIngredients = new List<MealIngredientItemDto>();

        foreach (var item in ingredients)
        {
            var stockCall = await CallToolAsync<int>(
                "InventoryTool.GetStock",
                new Dictionary<string, object> { ["productId"] = item.ProductId },
                cancellationToken);

            int stock = stockCall.Data;
            item.InStockQuantity = stock;

            // Nếu hết hàng, gọi InventoryTool để tìm sản phẩm thay thế cùng ngành hàng
            if (stock <= 0)
            {
                var altCall = await CallToolAsync<Product?>(
                    "InventoryTool.GetAlternativeProduct",
                    new Dictionary<string, object> { ["outOfStockProductId"] = item.ProductId },
                    cancellationToken);

                if (altCall.Data != null)
                {
                    var altProduct = altCall.Data;
                    _logger.LogInformation("[ShoppingAgent] Thay thế sản phẩm hết hàng {Old} bằng {New}",
                        item.ProductName, altProduct.ProductName);

                    item.ProductId = altProduct.ProductId;
                    item.ProductName = altProduct.ProductName;
                    item.RegularPrice = altProduct.Price;
                    item.FinalPrice = altProduct.Price;
                    item.Barcode = altProduct.Barcode;
                    item.Unit = altProduct.Unit;
                    item.PromotionNote = "Thay thế do sản phẩm gốc tạm hết hàng";
                }
            }

            // Kiểm tra áp dụng Flash Sale từ lô hàng cận date
            var matchedBatch = nearExpiryBatches.FirstOrDefault(b => b.ProductId == item.ProductId);
            if (matchedBatch != null && matchedBatch.DiscountPercent > 0)
            {
                item.IsPromotionOrNearExpiry = true;
                item.DiscountPercent = matchedBatch.DiscountPercent;
                item.FinalPrice = matchedBatch.FlashSalePrice;
                item.PromotionNote = $"Flash Sale cận date ({matchedBatch.DaysRemaining} ngày) - Giảm {matchedBatch.DiscountPercent}%";
                totalSaved += (item.RegularPrice - item.FinalPrice) * item.QuantityToBuy;
            }

            resolvedIngredients.Add(item);
        }

        // 4. Gọi BudgetTool để thẩm định và cân đối ngân sách qua ToolDispatcher
        var budgetCall = await CallToolAsync<BudgetEvaluationDto>(
            "BudgetTool.EvaluateBudget",
            new Dictionary<string, object>
            {
                ["targetBudget"] = targetBudget,
                ["items"] = resolvedIngredients
            },
            cancellationToken);

        var finalIngredients = budgetCall.Data?.RebalancedItems ?? resolvedIngredients;
        bool isWithinBudget = budgetCall.Data?.IsWithinBudget ?? true;

        // 5. Gọi CartTool để ước tính tổng hóa đơn và áp dụng Voucher qua ToolDispatcher
        string? topVoucherCode = activeVouchers.FirstOrDefault()?.PromotionCode;
        var cartEstimateCall = await CallToolAsync<CartEstimationDto>(
            "CartTool.EstimateCart",
            new Dictionary<string, object>
            {
                ["items"] = finalIngredients,
                ["voucherCode"] = topVoucherCode ?? "FRESH20"
            },
            cancellationToken);

        decimal estimatedTotal = cartEstimateCall.Data?.FinalTotal ?? finalIngredients.Sum(i => i.FinalPrice * i.QuantityToBuy);
        totalSaved += cartEstimateCall.Data?.DiscountAmount ?? 0m;

        // 6. Tổng hợp phản hồi tự nhiên qua LLM Gateway
        var responseText = await SynthesizeMealPlanResponseAsync(
            dishName,
            servings,
            targetBudget,
            estimatedTotal,
            totalSaved,
            finalIngredients,
            cookingTips,
            budgetCall.Data?.AdjustmentNote,
            cancellationToken);

        // Lưu mâm cơm vào bộ nhớ ngữ cảnh phiên (working memory)
        var mealResponse = new MealPlanChatResponse
        {
            AssistantMessage = responseText,
            DishName = dishName,
            Servings = servings,
            TotalEstimatedCost = estimatedTotal,
            TotalSavedAmount = totalSaved,
            IsWithinBudget = isWithinBudget,
            HasMealSuggestion = true,
            Ingredients = finalIngredients,
            CookingTips = cookingTips
        };

        SetMemory(context, "LastMealPlan", mealResponse);

        return AgentResultDto.Success(AgentName, responseText, mealResponse);
    }

    private async Task<AgentResultDto> HandleAddToCartAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var lastPlan = GetMemory<MealPlanChatResponse>(context, "LastMealPlan");
        var itemsToAdd = lastPlan?.Ingredients ?? new List<MealIngredientItemDto>();

        if (itemsToAdd.Count == 0)
        {
            return AgentResultDto.Success(
                AgentName,
                "Hiện tại giỏ của bạn chưa có danh sách mâm cơm nào được chọn. Bạn hãy nói cho mình biết bạn muốn nấu món gì nhé!",
                false);
        }

        var addResult = await CallToolAsync<bool>(
            "CartTool.AddToCart",
            new Dictionary<string, object>
            {
                ["sessionId"] = context.SessionId,
                ["items"] = itemsToAdd
            },
            cancellationToken);

        var msg = $"Đã thêm thành công trọn bộ {itemsToAdd.Count} nguyên liệu món '{lastPlan?.DishName}' vào giỏ hàng của bạn! Bạn có thể kiểm tra giỏ và tiến hành thanh toán bất cứ lúc nào.";
        return AgentResultDto.Success(AgentName, msg, addResult.Data);
    }

    private async Task<AgentResultDto> HandleProductSearchAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var keyword = context.UserPrompt.Trim();
        var searchCall = await CallToolAsync<List<Product>>(
            "ProductTool.Search",
            new Dictionary<string, object>
            {
                ["keyword"] = keyword,
                ["limit"] = 6
            },
            cancellationToken);

        var products = searchCall.Data ?? new List<Product>();
        if (products.Count == 0)
        {
            return AgentResultDto.Success(
                AgentName,
                $"Rất tiếc, siêu thị hiện chưa tìm thấy sản phẩm phù hợp với từ khóa '{keyword}'. Bạn có muốn tìm loại thực phẩm nào khác không?",
                products);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Dưới đây là các sản phẩm phù hợp tại siêu thị dành cho bạn:");
        foreach (var p in products)
        {
            sb.AppendLine($"- **{p.ProductName}**: {p.Price:N0} đ / {p.Unit}");
        }

        return AgentResultDto.Success(AgentName, sb.ToString(), products);
    }

    private async Task<AgentResultDto> HandlePromotionQueryAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var vouchersCall = await CallToolAsync<List<Promotion>>(
            "PromotionTool.GetVouchers",
            new Dictionary<string, object>(),
            cancellationToken);

        var flashSalesCall = await CallToolAsync<List<NearExpiryBatchDto>>(
            "PromotionTool.CheckFlashSale",
            new Dictionary<string, object>(),
            cancellationToken);

        var vouchers = vouchersCall.Data ?? new List<Promotion>();
        var flashSales = flashSalesCall.Data ?? new List<NearExpiryBatchDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Hôm nay siêu thị đang có các chương trình ưu đãi hấp dẫn dành cho bạn:");

        if (vouchers.Count > 0)
        {
            sb.AppendLine("\n**Mã giảm giá khả dụng:**");
            foreach (var v in vouchers.Take(3))
            {
                sb.AppendLine($"- Mã **{v.PromotionCode}**: {v.PromotionName} (Đơn từ {v.MinimumOrderAmount:N0} đ)");
            }
        }

        if (flashSales.Count > 0)
        {
            sb.AppendLine("\n**Flash Sale thực phẩm tươi cận date hôm nay:**");
            foreach (var fs in flashSales.Take(4))
            {
                sb.AppendLine($"- **{fs.ProductName}**: Giảm {fs.DiscountPercent}% còn {fs.FlashSalePrice:N0} đ (Hạn dùng còn {fs.DaysRemaining} ngày)");
            }
        }

        return AgentResultDto.Success(
            AgentName,
            sb.ToString(),
            new { Vouchers = vouchers, FlashSales = flashSales });
    }

    private async Task<string> SynthesizeMealPlanResponseAsync(
        string dishName,
        int servings,
        decimal targetBudget,
        decimal estimatedTotal,
        decimal totalSaved,
        List<MealIngredientItemDto> ingredients,
        string cookingTips,
        string? adjustmentNote,
        CancellationToken cancellationToken)
    {
        try
        {
            var systemPrompt = await _promptBuilder.BuildSystemPromptAsync("shopping", cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine($"Hãy viết câu trả lời tự nhiên, thân thiện (xưng hô 'mình' - 'bạn') để tư vấn mâm cơm sau:");
            sb.AppendLine($"- Tên món: {dishName}");
            sb.AppendLine($"- Khẩu phần: {servings} người");
            sb.AppendLine($"- Ngân sách khách đưa ra: {targetBudget:N0} đ");
            sb.AppendLine($"- Tổng chi phí thực tế: {estimatedTotal:N0} đ");
            sb.AppendLine($"- Số tiền tiết kiệm được nhờ Flash Sale/Voucher: {totalSaved:N0} đ");
            if (!string.IsNullOrWhiteSpace(adjustmentNote))
            {
                sb.AppendLine($"- Ghi chú điều chỉnh ngân sách: {adjustmentNote}");
            }
            sb.AppendLine($"- Mẹo nấu ăn: {cookingTips}");
            sb.AppendLine("- Danh sách nguyên liệu:");
            foreach (var item in ingredients)
            {
                sb.AppendLine($"  + {item.ProductName} ({item.QuantityToBuy} {item.Unit}): {item.FinalPrice:N0} đ {(item.IsPromotionOrNearExpiry ? $"[{item.PromotionNote}]" : "")}");
            }

            var prompt = sb.ToString();
            var response = await GenerateLlmAsync(prompt, systemPrompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response))
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ShoppingAgent] Lỗi khi sinh câu trả lời LLM, chuyển sang mẫu dự phòng.");
        }

        // Fallback văn bản thân thiện chuẩn mẫu khi LLM gặp sự cố
        var fallback = new StringBuilder();
        fallback.AppendLine($"Chào bạn, mình đã gợi ý mâm cơm **{dishName}** phù hợp cho {servings} người:");
        fallback.AppendLine($"- **Tổng chi phí ước tính**: {estimatedTotal:N0} đ {(targetBudget > 0 ? $"(Ngân sách dự kiến: {targetBudget:N0} đ)" : "")}");
        if (totalSaved > 0)
        {
            fallback.AppendLine($"- **Tiết kiệm được**: {totalSaved:N0} đ nhờ các ưu đãi Flash Sale.");
        }
        if (!string.IsNullOrWhiteSpace(adjustmentNote))
        {
            fallback.AppendLine($"- *Lưu ý*: {adjustmentNote}");
        }
        fallback.AppendLine("\n**Danh sách nguyên liệu:**");
        foreach (var item in ingredients)
        {
            var promoTag = item.IsPromotionOrNearExpiry ? $" *({item.PromotionNote})*" : "";
            fallback.AppendLine($"- {item.ProductName}: {item.QuantityToBuy} {item.Unit} - **{item.FinalPrice:N0} đ**{promoTag}");
        }
        fallback.AppendLine($"\n**Mẹo nấu ăn:** {cookingTips}");
        fallback.AppendLine("\nBạn có thể bấm **'Chọn mâm này'** để thêm toàn bộ nguyên liệu vào giỏ hàng ngay nhé!");

        return fallback.ToString();
    }

    private static string ExtractDishName(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "Mâm cơm gia đình thanh đạm";
        }

        var lower = prompt.ToLowerInvariant();
        if (lower.Contains("lẩu bò")) return "Lẩu bò nấm";
        if (lower.Contains("lẩu thái") || lower.Contains("lẩu hải sản")) return "Lẩu hải sản chua cay";
        if (lower.Contains("thịt kho") || lower.Contains("kho tàu")) return "Thịt kho tàu nước dừa";
        if (lower.Contains("canh chua")) return "Canh chua cá bông lau";
        if (lower.Contains("gà")) return "Gà hấp hành";

        return prompt.Length > 30 ? prompt[..30] : prompt;
    }
}
