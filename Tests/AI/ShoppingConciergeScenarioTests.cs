using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class ShoppingConciergeScenarioTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly AiShoppingConciergeService _service;

    public ShoppingConciergeScenarioTests()
    {
        var dbName = $"ShoppingConciergeDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        _dbContext = new AppDbContext(options);
        _llmClientMock = new Mock<ILlmClient>();

        // Default mock: LLM returns null so deterministic render layer is exercised
        _llmClientMock
            .Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), default))
            .ReturnsAsync((string)null!);

        SeedDatabase();

        _service = new AiShoppingConciergeService(_dbContext, _llmClientMock.Object);
    }

    private void SeedDatabase()
    {
        var catThit = new Category { CategoryId = 9, CategoryName = "Thịt tươi sống", Slug = "thit" };
        var catRau = new Category { CategoryId = 4, CategoryName = "Rau củ", Slug = "rau" };
        _dbContext.Categories.AddRange(catThit, catRau);

        var pBoUc = new Product { ProductId = 10, ProductName = "Thịt bò Úc Pacow", Price = 109000m, CategoryId = 9, Unit = "Khay 250g", Barcode = "893010" };
        var pTom = new Product { ProductId = 11, ProductName = "Tôm thẻ tươi sống", Price = 80000m, CategoryId = 9, Unit = "Hộp 300g", Barcode = "893011" };
        var pNam = new Product { ProductId = 12, ProductName = "Nấm kim châm Hàn Quốc", Price = 15000m, CategoryId = 4, Unit = "Gói 150g", Barcode = "893012" };
        var pCaiThao = new Product { ProductId = 13, ProductName = "Cải thảo VietGAP", Price = 25000m, CategoryId = 4, Unit = "Bắp", Barcode = "893013" };
        var pBap = new Product { ProductId = 14, ProductName = "Bắp ngọt Mỹ", Price = 18000m, CategoryId = 4, Unit = "2 Trái", Barcode = "893014" };
        var pBoTa = new Product { ProductId = 15, ProductName = "Thịt Bò Ta Tươi Sạch", Price = 95000m, CategoryId = 9, Unit = "Khay 250g", Barcode = "893015" };

        _dbContext.Products.AddRange(pBoUc, pTom, pNam, pCaiThao, pBap, pBoTa);

        var now = DateTime.UtcNow;
        _dbContext.InventoryBatches.AddRange(
            new InventoryBatch { BatchId = 1, ProductId = 10, Quantity = 25, ManufacturingDate = now.AddDays(-2), ExpiryDate = now.AddDays(4), Status = "Good" },
            new InventoryBatch { BatchId = 2, ProductId = 11, Quantity = 20, ManufacturingDate = now.AddDays(-1), ExpiryDate = now.AddDays(3), Status = "Good" },
            new InventoryBatch { BatchId = 3, ProductId = 12, Quantity = 50, ManufacturingDate = now.AddDays(-3), ExpiryDate = now.AddDays(6), Status = "Good" },
            new InventoryBatch { BatchId = 4, ProductId = 13, Quantity = 30, ManufacturingDate = now.AddDays(-2), ExpiryDate = now.AddDays(5), Status = "Good" },
            new InventoryBatch { BatchId = 5, ProductId = 14, Quantity = 40, ManufacturingDate = now.AddDays(-1), ExpiryDate = now.AddDays(7), Status = "Good" },
            new InventoryBatch { BatchId = 6, ProductId = 15, Quantity = 35, ManufacturingDate = now.AddDays(-1), ExpiryDate = now.AddDays(8), Status = "Good" }
        );

        _dbContext.Promotions.Add(new Promotion
        {
            PromotionId = 1,
            PromotionCode = "SMART20",
            PromotionName = "Giảm 20k đơn từ 200k",
            DiscountType = "FixedAmount",
            DiscountValue = 20000m,
            MinimumOrderAmount = 200000m,
            IsActive = true,
            StartDate = now.AddDays(-10),
            EndDate = now.AddDays(10)
        });

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Scenario1_CreatePlan_LauBo_ShouldRankInBudgetFirst()
    {
        var request = new MealPlanChatRequest
        {
            UserPrompt = "Mình có 300k, nấu lẩu bò cho 4 người.",
            TargetBudget = 300000m,
            NumberOfPeople = 4
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.True(response.HasMealSuggestion);
        Assert.NotEmpty(response.MealOptions);
        Assert.True(response.MealOptions.First().TotalEstimatedCost <= 300000m);
        Assert.Contains("Trong ngân sách", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario2_ReplaceIngredient_BoUcToBoTa_ShouldUpdatePlanAndScrubBoUc()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Mình có 300k, nấu lẩu bò cho 4 người." },
            new() { Role = "assistant", Content = "Dạ, mình gửi bạn thực đơn lẩu bò..." }
        };

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Đổi bò Úc thành bò ta.",
            TargetBudget = 300000m,
            NumberOfPeople = 4,
            ConversationHistory = history
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.True(response.HasMealSuggestion);

        var primary = response.MealOptions.First();
        Assert.DoesNotContain("bò úc", primary.Description.ToLowerInvariant());
        Assert.DoesNotContain("bò úc", primary.DishName.ToLowerInvariant());
        Assert.Contains(primary.Ingredients, i => i.ProductName.Contains("Bò Ta") || i.ProductName.Contains("bò ta"));
        Assert.DoesNotContain(primary.Ingredients, i => i.ProductName.Contains("bò Úc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Đã đổi", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario3_RemoveIngredient_Nam_ShouldRemoveNamAndScrubNamFromAllText()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Mình có 300k, nấu lẩu bò cho 4 người." },
            new() { Role = "assistant", Content = "Dạ, mình gửi bạn thực đơn lẩu bò..." },
            new() { Role = "user", Content = "Đổi bò Úc thành bò ta." },
            new() { Role = "assistant", Content = "Đã đổi bò Úc thành bò ta..." }
        };

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Bỏ nấm.",
            TargetBudget = 300000m,
            NumberOfPeople = 4,
            ConversationHistory = history
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.True(response.HasMealSuggestion);

        var primary = response.MealOptions.First();
        // Strict requirement: "Bỏ nấm" thì response không được còn chữ "nấm" trong phần mô tả chính hay nguyên liệu
        Assert.DoesNotContain("nấm", primary.Description.ToLowerInvariant());
        Assert.DoesNotContain("nấm", primary.DishName.ToLowerInvariant());
        Assert.DoesNotContain(primary.Ingredients, i => i.ProductName.Contains("nấm", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Đã bỏ nguyên liệu 'nấm'", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario4_UpdateBudget_250k_ShouldRecalculateRankingWithStrictLabels()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Mình có 300k, nấu lẩu bò cho 4 người." },
            new() { Role = "assistant", Content = "Dạ, mình gửi bạn thực đơn lẩu bò..." },
            new() { Role = "user", Content = "Đổi bò Úc thành bò ta." },
            new() { Role = "assistant", Content = "Đã đổi bò Úc thành bò ta..." },
            new() { Role = "user", Content = "Bỏ nấm." },
            new() { Role = "assistant", Content = "Đã bỏ nấm..." }
        };

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Giảm còn 250k.",
            ConversationHistory = history
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.True(response.HasMealSuggestion);
        Assert.Contains("Đã cập nhật ngân sách mới: 250,000 đ", response.AssistantMessage);

        foreach (var opt in response.MealOptions)
        {
            if (opt.TotalEstimatedCost > 250000m)
            {
                Assert.Contains("Vượt ngân sách", opt.OptionName);
            }
        }
    }

    [Fact]
    public async Task Scenario5_QueryPromotion_FlashSale_ShouldReturnDedicatedPromoBranchWithoutMealTray()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Mình có 300k, nấu lẩu bò cho 4 người." },
            new() { Role = "assistant", Content = "Dạ, mình gửi bạn thực đơn lẩu bò..." }
        };

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Có flash sale nào không?",
            ConversationHistory = history
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        // Dedicated branch: does NOT duplicate whole meal tray
        Assert.False(response.HasMealSuggestion);
        Assert.Empty(response.MealOptions);
        Assert.Contains("Flash Sale", response.AssistantMessage);
        Assert.Contains("SMART20", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario6_AddToCart_ShouldReturnDedicatedAddToCartBranch()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Mình có 300k, nấu lẩu bò cho 4 người." },
            new() { Role = "assistant", Content = "Dạ, mình gửi bạn thực đơn lẩu bò..." }
        };

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Thêm tất cả vào giỏ.",
            ConversationHistory = history
        };

        var response = await _service.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        // Dedicated branch: confirms cart injection without duplicating meal plan cards
        Assert.False(response.HasMealSuggestion);
        Assert.Contains("Đã thêm toàn bộ nguyên liệu vào giỏ hàng thành công", response.AssistantMessage);
    }
}
