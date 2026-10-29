using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class CartIntegrationTests : IAsyncLifetime
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
    public async Task AddToCart_ShouldPersistProductIdsInSessionContext()
    {
        var sessionId = "cart-session-test";
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 1, ProductName = "Trứng gà", RegularPrice = 11400m, FinalPrice = 11400m, QuantityToBuy = 1 },
            new() { ProductId = 2, ProductName = "Rau muống", RegularPrice = 14000m, FinalPrice = 14000m, QuantityToBuy = 1 }
        };

        var success = await _fixture.CartTool.AddToCartAsync(sessionId, items);
        Assert.True(success);

        var context = await _fixture.ContextService.GetOrCreateContextAsync(sessionId);
        Assert.NotNull(context.CurrentCartItemProductIds);
        Assert.Equal(2, context.CurrentCartItemProductIds.Count);
        Assert.Contains(1, context.CurrentCartItemProductIds);
        Assert.Contains(2, context.CurrentCartItemProductIds);
    }

    [Fact]
    public async Task EstimateCartTotal_ShouldCalculateSubTotalDiscountAndFinalTotal()
    {
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 3, ProductName = "Thịt heo xay", RegularPrice = 45000m, FinalPrice = 45000m, QuantityToBuy = 1 },
            new() { ProductId = 5, ProductName = "Nước mắm Nam Ngư", RegularPrice = 25000m, FinalPrice = 25000m, QuantityToBuy = 1 }
        }; // SubTotal = 70,000đ. Voucher GIAM10K applies -> Discount = 10,000đ, FinalTotal = 60,000đ

        var estimation = await _fixture.CartTool.EstimateCartTotalAsync(items, voucherCode: "GIAM10K");

        Assert.NotNull(estimation);
        Assert.Equal(70000m, estimation.SubTotal);
        Assert.Equal(10000m, estimation.DiscountAmount);
        Assert.Equal(60000m, estimation.FinalTotal);
        Assert.Equal(2, estimation.TotalItemCount);
    }

    [Fact]
    public async Task CartAgent_ExecuteAsync_WhenAddingMealToCart_ShouldSucceedAndInjectItems()
    {
        var sessionId = "cart-agent-flow";
        var context = new AgentContextDto
        {
            SessionId = sessionId,
            UserPrompt = "Thêm mâm cơm này vào giỏ hàng giúp mình",
            Intent = "AddToCart"
        };

        // Seed a meal plan in agent's memory
        var mealPlan = new MealPlanChatResponse
        {
            DishName = "Cơm Trứng & Rau Muống",
            Ingredients = new List<MealIngredientItemDto>
            {
                new() { ProductId = 1, ProductName = "Trứng gà", FinalPrice = 11400m, RegularPrice = 11400m, QuantityToBuy = 1 },
                new() { ProductId = 2, ProductName = "Rau muống", FinalPrice = 14000m, RegularPrice = 14000m, QuantityToBuy = 1 }
            }
        };
        context.SharedData = new Dictionary<string, object>
        {
            ["LastMealPlan"] = mealPlan
        };

        var result = await _fixture.CartAgent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("Đã thêm thành công", result.Output);

        // Verify items were persisted into session context
        var updatedContext = await _fixture.ContextService.GetOrCreateContextAsync(sessionId);
        Assert.NotNull(updatedContext.CurrentCartItemProductIds);
        Assert.Equal(2, updatedContext.CurrentCartItemProductIds.Count);
    }
}
