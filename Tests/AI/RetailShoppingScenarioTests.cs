using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Features.AI.Services.Implementations;
using SmartSupermarket.Backend.Features.AI.Tools;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class RetailShoppingScenarioTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly RetailTool _retailTool;
    private readonly RetailShoppingIntelligenceService _retailService;
    private readonly AiShoppingConciergeService _conciergeService;

    public RetailShoppingScenarioTests()
    {
        var dbName = $"RetailShoppingDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        _dbContext = new AppDbContext(options);
        _llmClientMock = new Mock<ILlmClient>();

        _llmClientMock
            .Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), default))
            .ReturnsAsync((string)null!);

        SeedRetailProducts();

        _retailTool = new RetailTool(_dbContext);
        _retailService = new RetailShoppingIntelligenceService(_dbContext, _retailTool, _llmClientMock.Object);
        _conciergeService = new AiShoppingConciergeService(_dbContext, _llmClientMock.Object, _retailService);
    }

    private void SeedRetailProducts()
    {
        var catPersonal = new Category { CategoryId = 8, CategoryName = "Chăm sóc cá nhân", Slug = "cham-soc-ca-nhan" };
        var catHousehold = new Category { CategoryId = 7, CategoryName = "Hóa phẩm & Giặt xả", Slug = "hoa-pham-giat-xa" };
        var catKitchen = new Category { CategoryId = 6, CategoryName = "Đồ dùng gia đình & Nhà bếp", Slug = "do-dung-gia-dinh-nha-bep" };

        _dbContext.Categories.AddRange(catPersonal, catHousehold, catKitchen);

        // Personal care
        var pClear = new Product { ProductId = 71, ProductName = "Dầu gội đầu Clear Men sạch gàu mát lạnh 630g", Price = 172000m, CategoryId = 8, Unit = "Chai 630g", Barcode = "8934588071" };
        var pPantene = new Product { ProductId = 72, ProductName = "Dầu gội Pantene phục hồi hư tổn chai 650ml", Price = 159000m, CategoryId = 8, Unit = "Chai 650ml", Barcode = "8934588072" };
        var pClearHerbal = new Product { ProductId = 172, ProductName = "Dầu gội Clear thảo dược sạch gàu nuôi dưỡng tóc 630g", Price = 179000m, CategoryId = 8, Unit = "Chai 630g", Barcode = "8934588172" };
        var pBobby = new Product { ProductId = 180, ProductName = "Khăn giấy ướt không mùi em bé Bobby gói 80 miếng", Price = 32000m, CategoryId = 8, Unit = "Gói 80 miếng", Barcode = "8934588180" };

        // Household
        var pSunlightCan = new Product { ProductId = 63, ProductName = "Nước rửa chén Sunlight chanh sạch dầu mỡ can 3.6kg", Price = 105000m, CategoryId = 7, Unit = "Can 3.6kg", Barcode = "8934588063" };
        var pSunlightChai = new Product { ProductId = 161, ProductName = "Nước rửa chén Sunlight chanh tươi khử mùi tanh chai 750g", Price = 29000m, CategoryId = 7, Unit = "Chai 750g", Barcode = "8934588161" };

        // Cookware & Kitchen
        var pChaoSunhouse = new Product { ProductId = 159, ProductName = "Chảo chống dính vân đá Sunhouse đáy từ 24cm", Price = 165000m, CategoryId = 6, Unit = "Cái 24cm", Barcode = "8934588159" };
        var pChaoElmich = new Product { ProductId = 54, ProductName = "Chảo chống dính đáy từ Elmich 24cm", Price = 245000m, CategoryId = 6, Unit = "Cái 24cm", Barcode = "8934588054" };
        var pNoiSunhouse = new Product { ProductId = 53, ProductName = "Nồi nhôm chống dính Sunhouse 20cm", Price = 165000m, CategoryId = 6, Unit = "Cái", Barcode = "8934588053" };
        var pNoiComComet = new Product { ProductId = 201, ProductName = "Nồi cơm điện nắp gài mini Comet 0.8L", Price = 250000m, CategoryId = 6, Unit = "Cái 0.8L", Barcode = "8934588201" };
        var pNoiComSharp = new Product { ProductId = 202, ProductName = "Nồi cơm điện Sharp 1.8L", Price = 690000m, CategoryId = 6, Unit = "Cái 1.8L", Barcode = "8934588202" };
        var pGiayAnAn = new Product { ProductId = 203, ProductName = "Giấy vệ sinh An An 10 cuộn không lõi", Price = 45000m, CategoryId = 8, Unit = "Lốc 10 cuộn", Barcode = "8934588203" };
        var pGiayPulppy = new Product { ProductId = 204, ProductName = "Giấy vệ sinh Pulppy 10 cuộn 3 lớp lụa", Price = 85000m, CategoryId = 8, Unit = "Lốc 10 cuộn", Barcode = "8934588204" };

        _dbContext.Products.AddRange(pClear, pPantene, pClearHerbal, pBobby, pSunlightCan, pSunlightChai, pChaoSunhouse, pChaoElmich, pNoiSunhouse, pNoiComComet, pNoiComSharp, pGiayAnAn, pGiayPulppy);

        var now = DateTime.UtcNow;
        _dbContext.InventoryBatches.AddRange(
            new InventoryBatch { BatchId = 101, ProductId = 71, Quantity = 40, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 102, ProductId = 72, Quantity = 35, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 103, ProductId = 172, Quantity = 25, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 104, ProductId = 180, Quantity = 100, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 105, ProductId = 63, Quantity = 50, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 106, ProductId = 161, Quantity = 60, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(2), Status = "Good" },
            new InventoryBatch { BatchId = 107, ProductId = 159, Quantity = 20, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(3), Status = "Good" },
            new InventoryBatch { BatchId = 108, ProductId = 54, Quantity = 15, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(3), Status = "Good" },
            new InventoryBatch { BatchId = 109, ProductId = 53, Quantity = 18, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(3), Status = "Good" },
            new InventoryBatch { BatchId = 110, ProductId = 201, Quantity = 25, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(5), Status = "Good" },
            new InventoryBatch { BatchId = 111, ProductId = 202, Quantity = 12, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(5), Status = "Good" },
            new InventoryBatch { BatchId = 112, ProductId = 203, Quantity = 80, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(3), Status = "Good" },
            new InventoryBatch { BatchId = 113, ProductId = 204, Quantity = 60, ManufacturingDate = now.AddMonths(-1), ExpiryDate = now.AddYears(3), Status = "Good" }
        );

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Scenario1_DauGoiReMaOn_RecommendsTier1AndTier2_HighlightsBestValue()
    {
        // Act: “Mua dầu gội nào rẻ mà ổn?”
        var request = new MealPlanChatRequest { UserPrompt = "Mua dầu gội nào rẻ mà ổn?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.False(response.HasMealSuggestion);
        Assert.Contains("Chăm sóc cá nhân", response.ShoppingCategory);
        Assert.NotEmpty(response.RetailRecommendations);

        // Verify products evaluated
        var recommended = response.RetailRecommendations.FirstOrDefault(r => r.IsRecommendedChoice);
        Assert.NotNull(recommended);
        Assert.Equal(RetailTier.Tier2BalancedValue, recommended.Tier);
        Assert.Contains("Clear Men", recommended.ProductName);
        Assert.NotEmpty(recommended.PricePerUnitMetric);

        // Verify Pantene as Tier 1 cheapest
        var tier1 = response.RetailRecommendations.FirstOrDefault(r => r.Tier == RetailTier.Tier1CheapestAcceptable);
        Assert.NotNull(tier1);
        Assert.Contains("Pantene", tier1.ProductName);
        Assert.True(tier1.FinalPrice < recommended.FinalPrice);

        // Verify XAI response structure
        Assert.Contains("Gợi ý tốt nhất cho bạn", response.AssistantMessage);
        Assert.Contains("Phân tích so sánh các lựa chọn tại siêu thị", response.AssistantMessage);
        Assert.Contains("Lời khuyên mua sắm thông minh", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario2_NoiComChaoDungLau_RecommendsTier3DurableHighPerformance()
    {
        // Act: “Nồi cơm nào dùng lâu, giá hợp lý?” or “Chảo nào dùng lâu, giá hợp lý?”
        var request = new MealPlanChatRequest { UserPrompt = "Chảo nào dùng lâu, giá hợp lý?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.NotEmpty(response.RetailRecommendations);

        var durableOption = response.RetailRecommendations.FirstOrDefault(r => r.Tier == RetailTier.Tier3DurableHighPerformance);
        Assert.NotNull(durableOption);
        Assert.Contains("Elmich", durableOption.ProductName);
        Assert.Contains("đáy từ", durableOption.ProductName.ToLower());
        Assert.Contains("Bảo hành", durableOption.PricePerUnitMetric);
    }

    [Fact]
    public async Task Scenario3_NuocRuaChenTietKiem_ShowsUnitMetric_29kPerKg()
    {
        // Act: “Nước rửa chén nào tiết kiệm mà tốt?”
        var request = new MealPlanChatRequest { UserPrompt = "Nước rửa chén nào tiết kiệm mà tốt?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);

        // Recommended is the 3.6kg can because price per kg is lowest
        var canItem = response.RetailRecommendations.FirstOrDefault(r => r.ProductName.Contains("3.6kg"));
        Assert.NotNull(canItem);
        Assert.True(canItem.IsRecommendedChoice);
        Assert.Equal(RetailTier.Tier2BalancedValue, canItem.Tier);
        Assert.Contains("29", canItem.PricePerUnitMetric); // ~29.167 đ/kg

        var chaiItem = response.RetailRecommendations.FirstOrDefault(r => r.ProductName.Contains("750g"));
        Assert.NotNull(chaiItem);
        Assert.Equal(RetailTier.Tier1CheapestAcceptable, chaiItem.Tier);

        // Trade-off explanation must mention 25% savings
        Assert.Contains("25%", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario4_GiayVeSinhDangTien_ReturnsComparisonTable()
    {
        // Act: “Giấy vệ sinh loại nào đáng tiền?”
        var request = new MealPlanChatRequest { UserPrompt = "Giấy vệ sinh loại nào đáng tiền?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.NotNull(response.ComparisonTable);
        Assert.NotEmpty(response.ComparisonTable.Rows);
        Assert.Contains(response.ComparisonTable.Headers, h => h.Contains("Tier 2"));
    }

    [Fact]
    public async Task Scenario5_DoEmBePhanKhucNao_RecommendsSafeGentleTier2()
    {
        // Act: “Mua đồ cho em bé thì nên chọn phân khúc nào?”
        var request = new MealPlanChatRequest { UserPrompt = "Mua đồ cho em bé thì nên chọn phân khúc nào?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.Contains("Mẹ & Bé", response.ShoppingCategory);

        var babyItem = response.RetailRecommendations.FirstOrDefault(r => r.ProductName.Contains("Bobby"));
        Assert.NotNull(babyItem);
        Assert.Equal(RetailTier.Tier2BalancedValue, babyItem.Tier);
        Assert.Contains("cồn", babyItem.Pros[0].ToLower());

        // Safety warning in guidance
        Assert.Contains("tuyệt đối không chọn hàng giá rẻ trôi nổi", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario6_SoSanhCacPhanKhucGia_ReturnsFullMultiTierTable()
    {
        // Act: “Nên mua loại rẻ, loại tiêu chuẩn hay loại cao cấp?”
        var request = new MealPlanChatRequest { UserPrompt = "Nên mua loại rẻ, loại tiêu chuẩn hay loại cao cấp?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.NotNull(response.ComparisonTable);
        Assert.True(response.ComparisonTable.Rows.Count >= 3);
        Assert.Equal(4, response.ComparisonTable.Headers.Count);
    }

    [Fact]
    public async Task Scenario7_MixedCart_RetailItemCanBeAddedToCart()
    {
        // Act
        var request = new MealPlanChatRequest { UserPrompt = "Mua dầu gội nào rẻ mà ổn?" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert: Retail recommendations are also mapped into Ingredients list with full product data
        Assert.NotNull(response.Ingredients);
        Assert.NotEmpty(response.Ingredients);
        var firstItem = response.Ingredients[0];
        Assert.True(firstItem.ProductId > 0);
        Assert.NotEmpty(firstItem.ProductName);
        Assert.NotEmpty(firstItem.Barcode);
        Assert.True(firstItem.FinalPrice > 0);
        Assert.Equal(1, firstItem.QuantityToBuy);
    }

    [Fact]
    public async Task Scenario8_CoNoiCom500kKhong_RoutesToRetail_NotMealPlanning()
    {
        // Act: “có nồi cơm 500k không”
        var request = new MealPlanChatRequest { UserPrompt = "có nồi cơm 500k không" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert: MUST route to Retail Recommendation, NOT Meal Planning
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.False(response.HasMealSuggestion);
        Assert.Empty(response.MealOptions);
        Assert.Contains("Đồ dùng gia đình", response.ShoppingCategory);

        // Best choice should be Comet 0.8L (250k <= 500k)
        var comet = response.RetailRecommendations.FirstOrDefault(r => r.ProductName.Contains("Comet"));
        Assert.NotNull(comet);
        Assert.True(comet.IsWithinBudget);
        Assert.True(comet.FinalPrice <= 500000m);

        // Sharp 1.8L (690k) should be marked over budget
        var sharp = response.RetailRecommendations.FirstOrDefault(r => r.ProductName.Contains("Sharp"));
        if (sharp != null)
        {
            Assert.False(sharp.IsWithinBudget);
            Assert.True(sharp.FinalPrice > 500000m);
        }

        // Assistant message should contain smart advice for nồi cơm
        Assert.Contains("Comet", response.AssistantMessage);
    }

    [Fact]
    public async Task Scenario9_MuaChao300k_RoutesToRetail_NotMealPlanning()
    {
        // Act: “mua chảo 300k”
        var request = new MealPlanChatRequest { UserPrompt = "mua chảo 300k" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.False(response.HasMealSuggestion);
        Assert.Contains("Đồ dùng gia đình", response.ShoppingCategory);

        var first = response.RetailRecommendations.FirstOrDefault();
        Assert.NotNull(first);
        Assert.True(first.IsWithinBudget);
        Assert.True(first.FinalPrice <= 300000m);
    }

    [Fact]
    public async Task Scenario10_MuaDauGoi200k_RoutesToRetail_NotMealPlanning()
    {
        // Act: “mua dầu gội 200k”
        var request = new MealPlanChatRequest { UserPrompt = "mua dầu gội 200k" };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRetailRecommendation);
        Assert.False(response.HasMealSuggestion);
        Assert.Contains("Chăm sóc cá nhân", response.ShoppingCategory);
        Assert.All(response.RetailRecommendations.Where(r => r.IsWithinBudget), r => Assert.True(r.FinalPrice <= 200000m));
    }

    [Fact]
    public async Task Scenario11_NauLauBo300k_PreservesMealPlanning()
    {
        // Act: “Mình có 300k, nấu lẩu bò cho 4 người.”
        var request = new MealPlanChatRequest { UserPrompt = "Mình có 300k, nấu lẩu bò cho 4 người." };
        var response = await _conciergeService.PlanMealAndShopAsync(request);

        // Assert: Cooking context triggers Meal Planning
        Assert.NotNull(response);
        Assert.True(response.HasMealSuggestion);
        Assert.Equal(4, response.Servings);
    }
}
