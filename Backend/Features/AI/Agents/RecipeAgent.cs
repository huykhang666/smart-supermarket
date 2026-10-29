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

public class RecipeAgent : BaseAgent
{
    public override string AgentName => "RecipeAgent";
    public override string Role => "Chuyên gia ẩm thực & cẩm nang Cookpad";

    public RecipeAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<RecipeAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        var rawPrompt = context.UserPrompt?.Trim() ?? string.Empty;
        var dishName = ExtractDishName(rawPrompt);
        int servings = context.Servings > 0 ? context.Servings : 2;

        _logger.LogInformation("[RecipeAgent] Tra cứu công thức cho món '{DishName}' ({Servings} người)", dishName, servings);

        // 1. Gọi RecipeTool lấy nguyên liệu & mẹo nấu ăn song song qua ToolDispatcher
        var ingredientsTask = CallToolAsync<List<MealIngredientItemDto>>(
            "RecipeTool.GetIngredients",
            new Dictionary<string, object>
            {
                ["dishName"] = dishName,
                ["servings"] = servings
            },
            cancellationToken);

        var tipsTask = CallToolAsync<string>(
            "RecipeTool.GetCookingTips",
            new Dictionary<string, object> { ["dishName"] = dishName },
            cancellationToken);

        await Task.WhenAll(ingredientsTask, tipsTask);

        var ingredients = ingredientsTask.Result.Data ?? new List<MealIngredientItemDto>();
        var tips = tipsTask.Result.Data ?? "Nêm nếm vừa miệng, canh độ lửa chuẩn để món ăn giữ trọn vị ngon gia đình.";

        // 2. Tạo phản hồi hướng dẫn chuẩn đầu bếp Cookpad
        var sb = new StringBuilder();
        sb.AppendLine($"Công thức chế biến món **{dishName}** chuẩn vị cho {servings} người ăn:");

        sb.AppendLine("\n**1. Danh sách nguyên liệu chuẩn định lượng:**");
        if (ingredients.Count > 0)
        {
            foreach (var ing in ingredients)
            {
                sb.AppendLine($"- {ing.ProductName}: {ing.QuantityToBuy} {ing.Unit}");
            }
        }
        else
        {
            sb.AppendLine("- Nguyên liệu chính theo công thức gia đình tươi ngon có sẵn tại siêu thị.");
        }

        sb.AppendLine("\n**2. Bí quyết sơ chế & Nấu nướng chuẩn Cookpad:**");
        sb.AppendLine($"- {tips}");
        sb.AppendLine("- **Tỷ lệ ướp**: 1 thìa nước mắm cốt : 1/2 thìa hạt nêm : 1 chút tiêu thơm, ướp trước khi nấu 15 phút.");
        sb.AppendLine("- **Kỹ thuật lửa**: Lửa lớn khi xào hoặc luộc để giữ màu xanh và độ ngọt; lửa liu riu khi kho để thịt ngấm mềm.");

        var resultData = new
        {
            DishName = dishName,
            Servings = servings,
            Ingredients = ingredients,
            CookingTips = tips
        };

        SetMemory(context, "LastRecipe", resultData);
        return AgentResultDto.Success(AgentName, sb.ToString(), resultData);
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
        if (lower.Contains("rau muống")) return "Rau muống luộc dầm sấu";
        if (lower.Contains("sườn")) return "Sườn xào chua ngọt";

        return prompt.Length > 30 ? prompt[..30] : prompt;
    }
}
