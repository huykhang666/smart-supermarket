using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class BudgetIntegrationTests : IAsyncLifetime
{
    private readonly AiIntegrationTestFixture _fixture = new();

    public async Task InitializeAsync()
    {
        await _fixture.SeedSampleDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task EvaluateBudget_WhenWithinBudget_ShouldReturnTrueWithCorrectDifference()
    {
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 1, ProductName = "Trứng gà", FinalPrice = 11400m, QuantityToBuy = 1 },
            new() { ProductId = 2, ProductName = "Rau muống", FinalPrice = 14000m, QuantityToBuy = 1 }
        }; // Total = 25,400đ <= Target 30,000đ

        var eval = await _fixture.BudgetTool.EvaluateBudgetAsync(targetBudget: 30000m, items);

        Assert.True(eval.IsWithinBudget);
        Assert.Equal(25400m, eval.CurrentTotal);
        Assert.Equal(-4600m, eval.Difference); // dư 4,600đ
    }

    [Fact]
    public async Task EvaluateBudget_WhenExceedingBudget_ShouldRebalanceWithCheaperAlternative()
    {
        // Thịt bò nạc dăm giá 85,000đ + Rau muống 14,000đ = 99,000đ.
        // Ngân sách 55,000đ -> vượt quá 55,000 + 5,500 = 60,500đ.
        // Hệ thống sẽ tìm sản phẩm thay thế cùng ngành hàng thịt: Thịt heo xay (45,000đ).
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 4, ProductName = "Thịt bò nạc dăm", FinalPrice = 85000m, QuantityToBuy = 1 },
            new() { ProductId = 2, ProductName = "Rau muống nước", FinalPrice = 14000m, QuantityToBuy = 1 }
        };

        var eval = await _fixture.BudgetTool.EvaluateBudgetAsync(targetBudget: 55000m, items);

        Assert.NotNull(eval);
        Assert.True(eval.IsWithinBudget);
        Assert.NotNull(eval.AdjustmentNote);
        Assert.Contains("tiết kiệm", eval.AdjustmentNote);
        // Kiểm tra xem sản phẩm đắt nhất (Thịt bò 85k) đã được thay bằng sản phẩm rẻ hơn
        Assert.DoesNotContain(eval.RebalancedItems, i => i.ProductId == 4);
    }

    [Fact]
    public async Task BudgetAgent_ExecuteAsync_ShouldGenerateFullBudgetReport()
    {
        var context = new AgentContextDto
        {
            SessionId = "budget-eval-sess",
            UserPrompt = "Cân đối và đánh giá ngân sách bữa ăn này",
            TargetBudget = 60000m,
            Intent = "BudgetEvaluation"
        };

        var meal = new MealPlanChatResponse
        {
            DishName = "Cơm Thịt Heo & Canh Rau Muống",
            Ingredients = new List<MealIngredientItemDto>
            {
                new() { ProductId = 3, ProductName = "Thịt heo xay", FinalPrice = 45000m, QuantityToBuy = 1 },
                new() { ProductId = 2, ProductName = "Rau muống nước", FinalPrice = 14000m, QuantityToBuy = 1 }
            }
        };

        context.SharedData = new Dictionary<string, object>
        {
            ["LastMealPlan"] = meal
        };

        var result = await _fixture.BudgetAgent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("Báo cáo cân đối chi tiêu", result.Output);
        Assert.Contains("60,000", result.Output);
    }
}
