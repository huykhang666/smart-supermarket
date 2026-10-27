using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Planning;

public class Planner : IPlanner
{
    public Task<ExecutionPlanDto> CreatePlanAsync(
        UserIntentDto intent,
        AgentContextDto context,
        CancellationToken cancellationToken = default)
    {
        var plan = new ExecutionPlanDto
        {
            PlanId = Guid.NewGuid().ToString("N"),
            IntentType = intent.Type,
            RequiresParallelExecution = false
        };

        switch (intent.Type)
        {
            case IntentType.Greeting:
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 1,
                    StepType = PlanStepType.LlmSynthesis,
                    TargetAgent = "ShoppingAgent",
                    Parameters = new Dictionary<string, object> { ["role"] = "greeting" }
                });
                break;

            case IntentType.CulinaryAdvice:
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 1,
                    StepType = PlanStepType.RecipeLookup,
                    TargetAgent = "RecipeAgent",
                    TargetTool = "RecipeTool.GetCookingTips"
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 2,
                    StepType = PlanStepType.LlmSynthesis,
                    TargetAgent = "ShoppingAgent"
                });
                break;

            case IntentType.PromotionInquiry:
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 1,
                    StepType = PlanStepType.PromotionScan,
                    TargetAgent = "PromotionAgent",
                    TargetTool = "PromotionTool.GetFlashSale"
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 2,
                    StepType = PlanStepType.LlmSynthesis,
                    TargetAgent = "ShoppingAgent"
                });
                break;

            case IntentType.CartAction:
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 1,
                    StepType = PlanStepType.CartOperation,
                    TargetAgent = "CartAgent",
                    TargetTool = "CartTool.AddToCart"
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 2,
                    StepType = PlanStepType.LlmSynthesis,
                    TargetAgent = "ShoppingAgent"
                });
                break;

            case IntentType.MealPlanning:
            default:
                // Đối với MealPlanning: Tra cứu công thức, kiểm tra kho và quét khuyến mãi có thể chạy song song
                plan.RequiresParallelExecution = true;
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 1,
                    StepType = PlanStepType.RecipeLookup,
                    TargetAgent = "RecipeAgent",
                    TargetTool = "RecipeTool.GetIngredients",
                    Parameters = new Dictionary<string, object>
                    {
                        ["dishTheme"] = intent.DishTheme ?? "Mâm cơm gia đình",
                        ["servings"] = intent.Servings ?? context.Servings
                    }
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 2,
                    StepType = PlanStepType.InventoryCheck,
                    TargetAgent = "InventoryAgent",
                    TargetTool = "InventoryTool.GetStockAndNearExpiry"
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 3,
                    StepType = PlanStepType.PromotionScan,
                    TargetAgent = "PromotionAgent",
                    TargetTool = "PromotionTool.CheckFlashSale"
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 4,
                    StepType = PlanStepType.BudgetRebalance,
                    TargetAgent = "BudgetAgent",
                    TargetTool = "ProductTool.GetAlternative",
                    Parameters = new Dictionary<string, object>
                    {
                        ["targetBudget"] = intent.TargetBudget ?? context.TargetBudget
                    }
                });
                plan.Steps.Add(new ExecutionStepDto
                {
                    Order = 5,
                    StepType = PlanStepType.LlmSynthesis,
                    TargetAgent = "ShoppingAgent"
                });
                break;
        }

        return Task.FromResult(plan);
    }
}
