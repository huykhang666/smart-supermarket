using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;
using SmartSupermarket.Backend.Features.AI.Tools;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class ToolTests
{
    [Fact]
    public async Task InventoryTool_GetStockAsync_ShouldCallInventoryService()
    {
        var mockService = new Mock<IAiInventoryService>();
        mockService.Setup(s => s.GetStockAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(25);

        var tool = new InventoryTool(mockService.Object);
        var stock = await tool.GetStockAsync(101);

        Assert.Equal(25, stock);
        mockService.Verify(s => s.GetStockAsync(101, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InventoryTool_GetNearExpiryBatchesAsync_ShouldCallInventoryService()
    {
        var mockService = new Mock<IAiInventoryService>();
        var batches = new List<NearExpiryBatchDto>
        {
            new() { BatchId = 1, DaysRemaining = 2, ProductName = "Sữa đặc" }
        };
        mockService.Setup(s => s.GetNearExpiryBatchesAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(batches);

        var tool = new InventoryTool(mockService.Object);
        var result = await tool.GetNearExpiryBatchesAsync(3);

        Assert.Single(result);
        Assert.Equal("Sữa đặc", result[0].ProductName);
        mockService.Verify(s => s.GetNearExpiryBatchesAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProductTool_SearchAsync_ShouldCallProductService()
    {
        var mockService = new Mock<IAiProductService>();
        var products = new List<Product>
        {
            new() { ProductId = 1, ProductName = "Rau muống" }
        };
        mockService.Setup(s => s.SearchProductsAsync("rau", null, 15, It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var tool = new ProductTool(mockService.Object);
        var result = await tool.SearchAsync("rau");

        Assert.Single(result);
        Assert.Equal("Rau muống", result[0].ProductName);
        mockService.Verify(s => s.SearchProductsAsync("rau", null, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProductTool_GetByIdAsync_ShouldCallProductService()
    {
        var mockService = new Mock<IAiProductService>();
        var product = new Product { ProductId = 42, ProductName = "Nước mắm" };
        mockService.Setup(s => s.GetProductByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var tool = new ProductTool(mockService.Object);
        var result = await tool.GetByIdAsync(42);

        Assert.NotNull(result);
        Assert.Equal("Nước mắm", result.ProductName);
    }

    [Fact]
    public async Task PromotionTool_GetActiveVouchersAsync_ShouldCallPromotionService()
    {
        var mockService = new Mock<IAiPromotionService>();
        var promos = new List<Promotion>
        {
            new() { PromotionId = 1, PromotionName = "Giảm 10k" }
        };
        mockService.Setup(s => s.GetActiveVouchersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(promos);

        var tool = new PromotionTool(mockService.Object);
        var result = await tool.GetActiveVouchersAsync();

        Assert.Single(result);
        Assert.Equal("Giảm 10k", result[0].PromotionName);
    }

    [Fact]
    public async Task PromotionTool_CheckFlashSaleAsync_ShouldCallPromotionService()
    {
        var mockService = new Mock<IAiPromotionService>();
        var sales = new List<NearExpiryBatchDto>
        {
            new() { BatchId = 5, DiscountPercent = 30 }
        };
        mockService.Setup(s => s.GetFlashSaleItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sales);

        var tool = new PromotionTool(mockService.Object);
        var result = await tool.CheckFlashSaleAsync();

        Assert.Single(result);
        Assert.Equal(30, result[0].DiscountPercent);
    }

    [Fact]
    public async Task RecipeTool_GetRecipeIngredientsAsync_ShouldCallRecipeService()
    {
        var mockService = new Mock<IAiRecipeService>();
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 10, ProductName = "Cá lóc" },
            new() { ProductId = 11, ProductName = "Dứa" }
        };
        mockService.Setup(s => s.GetRecipeIngredientsAsync("Canh chua", 2, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var tool = new RecipeTool(mockService.Object);
        var result = await tool.GetRecipeIngredientsAsync("Canh chua", 2);

        Assert.Equal(2, result.Count);
        mockService.Verify(s => s.GetRecipeIngredientsAsync("Canh chua", 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecipeTool_GetCookingTipsAsync_ShouldCallRecipeService()
    {
        var mockService = new Mock<IAiRecipeService>();
        mockService.Setup(s => s.GetCookingTipsAsync("Trứng chiên", It.IsAny<CancellationToken>())).ReturnsAsync("Đánh đều tay với chút tiêu");

        var tool = new RecipeTool(mockService.Object);
        var tip = await tool.GetCookingTipsAsync("Trứng chiên");

        Assert.Equal("Đánh đều tay với chút tiêu", tip);
    }

    [Fact]
    public async Task BudgetTool_EvaluateBudgetAsync_ShouldCallBudgetService()
    {
        var mockService = new Mock<IAiBudgetService>();
        var eval = new BudgetEvaluationDto { TargetBudget = 100000m, CurrentTotal = 90000m, Difference = 10000m, IsWithinBudget = true };
        mockService.Setup(s => s.EvaluateBudgetAsync(100000m, It.IsAny<List<MealIngredientItemDto>>(), It.IsAny<CancellationToken>())).ReturnsAsync(eval);

        var tool = new BudgetTool(mockService.Object);
        var result = await tool.EvaluateBudgetAsync(100000m, new List<MealIngredientItemDto>());

        Assert.True(result.IsWithinBudget);
        Assert.Equal(10000m, result.Difference);
    }

    [Fact]
    public async Task CartTool_AddToCartAsync_ShouldCallCartService()
    {
        var mockService = new Mock<IAiCartService>();
        mockService.Setup(s => s.AddToCartAsync("sess-1", It.IsAny<List<MealIngredientItemDto>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var tool = new CartTool(mockService.Object);
        var success = await tool.AddToCartAsync("sess-1", new List<MealIngredientItemDto>());

        Assert.True(success);
        mockService.Verify(s => s.AddToCartAsync("sess-1", It.IsAny<List<MealIngredientItemDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CartTool_EstimateCartTotalAsync_ShouldCallCartService()
    {
        var mockService = new Mock<IAiCartService>();
        var est = new CartEstimationDto { FinalTotal = 50000m, DiscountAmount = 5000m };
        mockService.Setup(s => s.EstimateCartTotalAsync(It.IsAny<List<MealIngredientItemDto>>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(est);

        var tool = new CartTool(mockService.Object);
        var result = await tool.EstimateCartTotalAsync(new List<MealIngredientItemDto>());

        Assert.Equal(50000m, result.FinalTotal);
        Assert.Equal(5000m, result.DiscountAmount);
    }
}
