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

public class BudgetAgent : BaseAgent
{
    public override string AgentName => "BudgetAgent";
    public override string Role => "Chuyên gia cân đối ngân sách & chi tiêu đi chợ";

    public BudgetAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger<BudgetAgent> logger)
        : base(llmGateway, toolDispatcher, promptBuilder, logger)
    {
    }

    protected override async Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken)
    {
        decimal targetBudget = context.TargetBudget > 0 ? context.TargetBudget : 200000m;
        _logger.LogInformation("[BudgetAgent] Thẩm định ngân sách {Budget:N0} đ cho session {SessionId}", targetBudget, context.SessionId);

        // Lấy danh sách nguyên liệu từ bộ nhớ làm việc hoặc tạo danh sách mặc định
        var lastMeal = GetMemory<MealPlanChatResponse>(context, "LastMealPlan");
        var items = lastMeal?.Ingredients ?? new List<MealIngredientItemDto>();

        // 1. Gọi BudgetTool để thẩm định ngân sách qua ToolDispatcher
        var budgetCall = await CallToolAsync<BudgetEvaluationDto>(
            "BudgetTool.EvaluateBudget",
            new Dictionary<string, object>
            {
                ["targetBudget"] = targetBudget,
                ["items"] = items
            },
            cancellationToken);

        var evaluation = budgetCall.Data ?? new BudgetEvaluationDto
        {
            TargetBudget = targetBudget,
            CurrentTotal = items.Sum(i => i.FinalPrice * i.QuantityToBuy),
            IsWithinBudget = true,
            RebalancedItems = items
        };

        // 2. Định dạng phản hồi phân tích chi tiêu
        var sb = new StringBuilder();
        sb.AppendLine($"Báo cáo cân đối chi tiêu đi chợ của bạn:");
        sb.AppendLine($"- **Ngân sách mục tiêu**: {targetBudget:N0} đ");
        sb.AppendLine($"- **Tổng chi phí dự kiến**: {evaluation.CurrentTotal:N0} đ");

        if (evaluation.IsWithinBudget)
        {
            var remaining = targetBudget - evaluation.CurrentTotal;
            sb.AppendLine($"- **Trạng thái**: Nằm trong ngân sách an toàn (còn dư khoảng {Math.Max(0, remaining):N0} đ).");
        }
        else
        {
            sb.AppendLine($"- **Trạng thái**: Vượt ngân sách khoảng {Math.Abs(evaluation.Difference):N0} đ.");
        }

        if (!string.IsNullOrWhiteSpace(evaluation.AdjustmentNote))
        {
            sb.AppendLine($"- **Đề xuất điều chỉnh**: {evaluation.AdjustmentNote}");
        }

        // Đưa ra gợi ý phân khúc theo ngân sách
        if (targetBudget <= 50000m)
        {
            sb.AppendLine("\n*Gợi ý phân khúc Tiết kiệm*: Bạn nên ưu tiên mâm cơm bình dân đủ chất như Trứng chiên + Rau muống luộc dầm sấu.");
        }
        else if (targetBudget <= 200000m)
        {
            sb.AppendLine("\n*Gợi ý phân khúc Gia đình Tiêu chuẩn*: Ngân sách rất thích hợp cho mâm cơm ấm cúng gồm Thịt kho tàu + Canh rau + Trứng.");
        }
        else
        {
            sb.AppendLine("\n*Gợi ý phân khúc Thịnh soạn*: Ngân sách thoải mái để bạn cùng gia đình thưởng thức các món lẩu bò nấm hoặc lẩu thái hải sản hấp dẫn.");
        }

        SetMemory(context, "LastBudgetEvaluation", evaluation);
        return AgentResultDto.Success(AgentName, sb.ToString(), evaluation);
    }
}
