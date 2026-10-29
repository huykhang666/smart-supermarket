using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.Agents;
using SmartSupermarket.Backend.Features.AI.Agents.Common;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class TestableBaseAgent : BaseAgent
{
    public TestableBaseAgent(ILLMGateway llm, IToolDispatcher d, IPromptBuilder p, ILogger<TestableBaseAgent> l)
        : base(llm, d, p, l) { }

    public override string AgentName => "TestableBaseAgent";
    public override string Role => "Tester";

    public T? TestGetMemory<T>(AgentContextDto ctx, string key) => GetMemory<T>(ctx, key);
    public void TestSetMemory(AgentContextDto ctx, string key, object value) => SetMemory(ctx, key, value);

    protected override Task<AgentResultDto> ProcessInternalAsync(AgentContextDto context, CancellationToken cancellationToken)
    {
        return Task.FromResult(AgentResultDto.Success(AgentName, "OK"));
    }
}

public class AgentTests
{
    private readonly Mock<ILLMGateway> _llmGatewayMock = new();
    private readonly Mock<IToolDispatcher> _dispatcherMock = new();
    private readonly Mock<IPromptBuilder> _promptBuilderMock = new();

    public AgentTests()
    {
        _llmGatewayMock
            .Setup(g => g.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Đây là phản hồi AI mẫu.");

        _promptBuilderMock
            .Setup(p => p.BuildSystemPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System Prompt Mock");

        _promptBuilderMock
            .Setup(p => p.BuildUserPromptWithContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Prompt Mock");
    }

    [Fact]
    public void ShoppingAgent_ShouldHaveCorrectMetadata()
    {
        var loggerMock = new Mock<ILogger<ShoppingAgent>>();
        var agent = new ShoppingAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        Assert.Equal("ShoppingAgent", agent.AgentName);
        Assert.False(string.IsNullOrWhiteSpace(agent.Role));
    }

    [Fact]
    public async Task ShoppingAgent_ShouldUseToolDispatcher_WhenSearchingProducts()
    {
        var loggerMock = new Mock<ILogger<ShoppingAgent>>();
        var agent = new ShoppingAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        var searchResult = new List<Product>
        {
            new() { ProductId = 1, ProductName = "Trứng gà tươi", Price = 30000m }
        };

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<Product>>("ProductTool.Search", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<Product>>.Success("ProductTool.Search", searchResult, 10));

        var context = new AgentContextDto
        {
            SessionId = "test-session-1",
            UserPrompt = "Tìm sản phẩm trứng gà",
            Intent = "ProductSearch"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<List<Product>>("ProductTool.Search", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShoppingAgent_ShouldHandleMealPlanning_AndReturnCookpadOptions()
    {
        var loggerMock = new Mock<ILogger<ShoppingAgent>>();
        var agent = new ShoppingAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        var ingredients = new List<MealIngredientItemDto>
        {
            new() { ProductId = 1, ProductName = "Trứng gà", FinalPrice = 10000m, QuantityToBuy = 1 }
        };

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<MealIngredientItemDto>>("RecipeTool.GetIngredients", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<MealIngredientItemDto>>.Success("RecipeTool.GetIngredients", ingredients, 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<string>("RecipeTool.GetCookingTips", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<string>.Success("RecipeTool.GetCookingTips", "Mẹo nấu thơm ngon", 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<int>("InventoryTool.GetStock", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<int>.Success("InventoryTool.GetStock", 10, 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<NearExpiryBatchDto>>("InventoryTool.GetNearExpiry", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<NearExpiryBatchDto>>.Success("InventoryTool.GetNearExpiry", new List<NearExpiryBatchDto>(), 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<Promotion>>("PromotionTool.GetVouchers", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<Promotion>>.Success("PromotionTool.GetVouchers", new List<Promotion>(), 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<NearExpiryBatchDto>>("PromotionTool.CheckFlashSale", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<NearExpiryBatchDto>>.Success("PromotionTool.CheckFlashSale", new List<NearExpiryBatchDto>(), 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<BudgetEvaluationDto>("BudgetTool.EvaluateBudget", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<BudgetEvaluationDto>.Success("BudgetTool.EvaluateBudget", new BudgetEvaluationDto { TargetBudget = 50000m, CurrentTotal = 30000m, IsWithinBudget = true }, 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<CartEstimationDto>("CartTool.EstimateCart", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<CartEstimationDto>.Success("CartTool.EstimateCart", new CartEstimationDto { FinalTotal = 30000m, DiscountAmount = 2000m }, 5));

        var context = new AgentContextDto
        {
            SessionId = "meal-session",
            UserPrompt = "Gợi ý mâm cơm 2 người 50k",
            Intent = "MealPlanning",
            Servings = 2,
            TargetBudget = 50000m
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task InventoryAgent_ShouldDispatchNearExpiry_WhenAuditing()
    {
        var loggerMock = new Mock<ILogger<InventoryAgent>>();
        var agent = new InventoryAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        var batches = new List<NearExpiryBatchDto>
        {
            new() { BatchId = 10, ProductName = "Sữa tươi", DaysRemaining = 2 }
        };

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<NearExpiryBatchDto>>("InventoryTool.GetNearExpiry", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<NearExpiryBatchDto>>.Success("InventoryTool.GetNearExpiry", batches, 12));

        var context = new AgentContextDto
        {
            SessionId = "inv-session",
            UserPrompt = "Kiểm tra hàng cận hạn",
            Intent = "AuditNearExpiry"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<List<NearExpiryBatchDto>>("InventoryTool.GetNearExpiry", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PromotionAgent_ShouldDispatchActivePromotions()
    {
        var loggerMock = new Mock<ILogger<PromotionAgent>>();
        var agent = new PromotionAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<Promotion>>("PromotionTool.GetVouchers", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<Promotion>>.Success("PromotionTool.GetVouchers", new List<Promotion>(), 8));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<NearExpiryBatchDto>>("PromotionTool.CheckFlashSale", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<NearExpiryBatchDto>>.Success("PromotionTool.CheckFlashSale", new List<NearExpiryBatchDto>(), 8));

        var context = new AgentContextDto
        {
            SessionId = "promo-session",
            UserPrompt = "Hôm nay có khuyến mãi gì?",
            Intent = "PromotionInquiry"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<List<Promotion>>("PromotionTool.GetVouchers", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecipeAgent_ShouldDispatchRecipeIngredients()
    {
        var loggerMock = new Mock<ILogger<RecipeAgent>>();
        var agent = new RecipeAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        _dispatcherMock
            .Setup(d => d.DispatchAsync<List<MealIngredientItemDto>>("RecipeTool.GetIngredients", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<List<MealIngredientItemDto>>.Success("RecipeTool.GetIngredients", new List<MealIngredientItemDto>(), 5));

        _dispatcherMock
            .Setup(d => d.DispatchAsync<string>("RecipeTool.GetCookingTips", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<string>.Success("RecipeTool.GetCookingTips", "Mẹo nấu ngon", 5));

        var context = new AgentContextDto
        {
            SessionId = "recipe-session",
            UserPrompt = "Công thức nấu Canh chua cá lóc",
            Intent = "RecipeSearch"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<List<MealIngredientItemDto>>("RecipeTool.GetIngredients", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BudgetAgent_ShouldDispatchEvaluateBudget()
    {
        var loggerMock = new Mock<ILogger<BudgetAgent>>();
        var agent = new BudgetAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        var eval = new BudgetEvaluationDto { TargetBudget = 100000m, CurrentTotal = 85000m, Difference = 15000m, IsWithinBudget = true };
        _dispatcherMock
            .Setup(d => d.DispatchAsync<BudgetEvaluationDto>("BudgetTool.EvaluateBudget", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<BudgetEvaluationDto>.Success("BudgetTool.EvaluateBudget", eval, 6));

        var context = new AgentContextDto
        {
            SessionId = "budget-session",
            UserPrompt = "Đánh giá ngân sách 100k",
            Intent = "BudgetCheck"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<BudgetEvaluationDto>("BudgetTool.EvaluateBudget", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CartAgent_ShouldDispatchCalculateTotal()
    {
        var loggerMock = new Mock<ILogger<CartAgent>>();
        var agent = new CartAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);

        var estimation = new CartEstimationDto { SubTotal = 90000m, FinalTotal = 80000m, DiscountAmount = 10000m };
        _dispatcherMock
            .Setup(d => d.DispatchAsync<CartEstimationDto>("CartTool.EstimateCart", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResultDto<CartEstimationDto>.Success("CartTool.EstimateCart", estimation, 7));

        var context = new AgentContextDto
        {
            SessionId = "cart-session",
            UserPrompt = "Tính tổng giỏ hàng",
            Intent = "CartCalculation"
        };

        var result = await agent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        _dispatcherMock.Verify(d => d.DispatchAsync<CartEstimationDto>("CartTool.EstimateCart", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void BaseAgent_WorkingMemory_ShouldStoreAndRetrieveVariables()
    {
        var loggerMock = new Mock<ILogger<TestableBaseAgent>>();
        var agent = new TestableBaseAgent(_llmGatewayMock.Object, _dispatcherMock.Object, _promptBuilderMock.Object, loggerMock.Object);
        var context = new AgentContextDto { SessionId = "mem-session" };

        agent.TestSetMemory(context, "selectedDish", "Thịt kho tàu");
        agent.TestSetMemory(context, "guestCount", 4);

        Assert.Equal("Thịt kho tàu", agent.TestGetMemory<string>(context, "selectedDish"));
        Assert.Equal(4, agent.TestGetMemory<int>(context, "guestCount"));
        Assert.Null(agent.TestGetMemory<string>(context, "nonExistent"));
    }
}
