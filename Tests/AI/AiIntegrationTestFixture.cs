using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.Agents;
using SmartSupermarket.Backend.Features.AI.Cache.Implementations;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.Controllers;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Implementations;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Features.AI.Orchestration;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Repositories.Implementations;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Features.AI.Services.Context;
using SmartSupermarket.Backend.Features.AI.Services.Implementations;
using SmartSupermarket.Backend.Features.AI.Services.Intent;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;
using SmartSupermarket.Backend.Features.AI.Services.Planning;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Features.AI.Tools;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Tests.AI;

public class AiIntegrationTestFixture : IAsyncDisposable
{
    public AppDbContext DbContext { get; }
    public IMemoryCache MemoryCache { get; }
    public IDistributedCache DistributedCache { get; }
    public ICacheService CacheService { get; }
    public IConversationCache ConversationCache { get; }
    public IKnowledgeCache KnowledgeCache { get; }
    public IPromptCache PromptCache { get; }
    public IResponseCache ResponseCache { get; }
    public IAiMonitoringService MonitoringService { get; }

    public IAiProductRepository ProductRepo { get; }
    public IAiInventoryBatchRepository InventoryRepo { get; }
    public IAiPromotionRepository PromotionRepo { get; }
    public IAiRecipeRepository RecipeRepo { get; }

    public IAiProductService ProductService { get; }
    public IAiInventoryService InventoryService { get; }
    public IAiPromotionService PromotionService { get; }
    public IAiRecipeService RecipeService { get; }
    public IAiBudgetService BudgetService { get; }
    public IAiCartService CartService { get; }
    public IConversationContextService ContextService { get; }

    public IInventoryTool InventoryTool { get; }
    public IProductTool ProductTool { get; }
    public IPromotionTool PromotionTool { get; }
    public IRecipeTool RecipeTool { get; }
    public IBudgetTool BudgetTool { get; }
    public ICartTool CartTool { get; }

    public IToolDispatcher ToolDispatcher { get; }
    public IPromptBuilder PromptBuilder { get; }
    public IIntentAnalyzer IntentAnalyzer { get; }
    public IPlanner Planner { get; }
    public ILLMGateway LlmGateway { get; }
    public IAgentOrchestrator AgentOrchestrator { get; }

    public ShoppingAgent ShoppingAgent { get; }
    public InventoryAgent InventoryAgent { get; }
    public PromotionAgent PromotionAgent { get; }
    public RecipeAgent RecipeAgent { get; }
    public BudgetAgent BudgetAgent { get; }
    public CartAgent CartAgent { get; }

    public AiController Controller { get; }

    public AiIntegrationTestFixture()
    {
        var dbName = $"AiIntegrationDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        DbContext = new AppDbContext(options);

        MemoryCache = new MemoryCache(new MemoryCacheOptions());
        DistributedCache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        CacheService = new RedisCacheService(DistributedCache, MemoryCache, NullLogger<RedisCacheService>.Instance);

        ConversationCache = new ConversationCache(CacheService, NullLogger<ConversationCache>.Instance);
        KnowledgeCache = new KnowledgeCache(CacheService, NullLogger<KnowledgeCache>.Instance);
        PromptCache = new PromptCache(CacheService, NullLogger<PromptCache>.Instance);
        ResponseCache = new ResponseCache(CacheService, NullLogger<ResponseCache>.Instance);
        MonitoringService = new AiMonitoringService(new AiMetricsCollector(), NullLogger<AiMonitoringService>.Instance);

        ProductRepo = new AiProductRepository(DbContext);
        InventoryRepo = new AiInventoryBatchRepository(DbContext);
        PromotionRepo = new AiPromotionRepository(DbContext);
        RecipeRepo = new AiRecipeRepository(DbContext);

        ProductService = new AiProductService(ProductRepo);
        InventoryService = new AiInventoryService(InventoryRepo, ProductRepo);
        PromotionService = new AiPromotionService(PromotionRepo, InventoryService);
        RecipeService = new AiRecipeService(RecipeRepo, KnowledgeCache);
        BudgetService = new AiBudgetService(ProductService);
        ContextService = new ConversationContextService(ConversationCache, NullLogger<ConversationContextService>.Instance);
        CartService = new AiCartService(ContextService, PromotionService);

        InventoryTool = new InventoryTool(InventoryService);
        ProductTool = new ProductTool(ProductService);
        PromotionTool = new PromotionTool(PromotionService);
        RecipeTool = new RecipeTool(RecipeService);
        BudgetTool = new BudgetTool(BudgetService);
        CartTool = new CartTool(CartService);

        ToolDispatcher = new ToolDispatcher(NullLogger<ToolDispatcher>.Instance, monitoringService: MonitoringService);

        RegisterBuiltInTools(ToolDispatcher);

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(AppContext.BaseDirectory);
        PromptBuilder = new PromptBuilder(mockEnv.Object, PromptCache, NullLogger<PromptBuilder>.Instance);

        IntentAnalyzer = new IntentAnalyzer();
        Planner = new Planner();

        var localGateway = new LocalFallbackGateway();
        var mockLlmGateway = new Mock<ILLMGateway>();
        mockLlmGateway
            .Setup(g => g.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns<string, string?, System.Threading.CancellationToken>((prompt, sys, ct) => localGateway.GenerateAsync(prompt, sys, false, ct));
        mockLlmGateway
            .Setup(g => g.GenerateJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns<string, string?, System.Threading.CancellationToken>((prompt, sys, ct) => localGateway.GenerateAsync(prompt, sys, true, ct));
        mockLlmGateway.SetupGet(g => g.ActiveProvider).Returns("Local-Rule-Engine");
        LlmGateway = mockLlmGateway.Object;

        AgentOrchestrator = new AgentOrchestrator(
            ContextService,
            IntentAnalyzer,
            Planner,
            PromptBuilder,
            LlmGateway,
            ToolDispatcher,
            NullLogger<AgentOrchestrator>.Instance,
            MonitoringService);

        ShoppingAgent = new ShoppingAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<ShoppingAgent>.Instance);
        InventoryAgent = new InventoryAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<InventoryAgent>.Instance);
        PromotionAgent = new PromotionAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<PromotionAgent>.Instance);
        RecipeAgent = new RecipeAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<RecipeAgent>.Instance);
        BudgetAgent = new BudgetAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<BudgetAgent>.Instance);
        CartAgent = new CartAgent(LlmGateway, ToolDispatcher, PromptBuilder, NullLogger<CartAgent>.Instance);

        var agents = new SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent[]
        {
            ShoppingAgent, InventoryAgent, PromotionAgent, RecipeAgent, BudgetAgent, CartAgent
        };

        var conciergeMock = new Mock<IAiShoppingConciergeService>();
        var legacyAiMock = new Mock<IAiService>();

        Controller = new AiController(
            AgentOrchestrator,
            conciergeMock.Object,
            ShoppingAgent,
            agents,
            legacyAiMock.Object,
            NullLogger<AiController>.Instance);
    }

    private void RegisterBuiltInTools(IToolDispatcher dispatcher)
    {
        dispatcher.RegisterHandler("ProductTool.Search", async (p, ct) =>
        {
            var kw = p.TryGetValue("keyword", out var val) ? val?.ToString() ?? "" : "";
            var lim = p.TryGetValue("limit", out var lVal) ? Convert.ToInt32(lVal) : 15;
            return await ProductTool.SearchAsync(kw, null, lim, ct);
        });

        dispatcher.RegisterHandler("ProductTool.GetById", async (p, ct) =>
        {
            var id = Convert.ToInt32(p["productId"]);
            return await ProductTool.GetByIdAsync(id, ct);
        });

        dispatcher.RegisterHandler("InventoryTool.GetStock", async (p, ct) =>
        {
            var id = Convert.ToInt32(p["productId"]);
            return await InventoryTool.GetStockAsync(id, ct);
        });

        dispatcher.RegisterHandler("InventoryTool.GetNearExpiry", async (p, ct) =>
        {
            var threshold = p.TryGetValue("daysThreshold", out var t) ? Convert.ToInt32(t) : 3;
            return await InventoryTool.GetNearExpiryBatchesAsync(threshold, ct);
        });

        dispatcher.RegisterHandler("PromotionTool.GetVouchers", async (p, ct) =>
        {
            return await PromotionTool.GetActiveVouchersAsync(ct);
        });

        dispatcher.RegisterHandler("PromotionTool.CheckFlashSale", async (p, ct) =>
        {
            return await PromotionTool.CheckFlashSaleAsync(ct);
        });

        dispatcher.RegisterHandler("RecipeTool.GetIngredients", async (p, ct) =>
        {
            var dish = p["dishName"]?.ToString() ?? "";
            var servings = p.TryGetValue("servings", out var s) ? Convert.ToInt32(s) : 2;
            return await RecipeTool.GetRecipeIngredientsAsync(dish, servings, ct);
        });

        dispatcher.RegisterHandler("RecipeTool.GetCookingTips", async (p, ct) =>
        {
            var dish = p["dishName"]?.ToString() ?? "";
            return await RecipeTool.GetCookingTipsAsync(dish, ct);
        });

        dispatcher.RegisterHandler("BudgetTool.EvaluateBudget", async (p, ct) =>
        {
            var target = Convert.ToDecimal(p["targetBudget"]);
            var items = (List<MealIngredientItemDto>)(p["items"]);
            return await BudgetTool.EvaluateBudgetAsync(target, items, ct);
        });

        dispatcher.RegisterHandler("CartTool.EstimateCart", async (p, ct) =>
        {
            var items = (List<MealIngredientItemDto>)(p["items"]);
            var voucher = p.TryGetValue("voucherCode", out var v) ? v?.ToString() : null;
            return await CartTool.EstimateCartTotalAsync(items, voucher, ct);
        });

        dispatcher.RegisterHandler("CartTool.AddToCart", async (p, ct) =>
        {
            var sess = p["sessionId"]?.ToString() ?? "";
            var items = (List<MealIngredientItemDto>)(p["items"]);
            return await CartTool.AddToCartAsync(sess, items, ct);
        });
    }

    public async Task SeedSampleDataAsync()
    {
        var now = DateTime.UtcNow;

        var catThit = new Category { CategoryId = 1, CategoryName = "Thịt tươi sống", Slug = "thit-tuoi" };
        var catRau = new Category { CategoryId = 2, CategoryName = "Rau củ quả", Slug = "rau-cu" };
        var catDoHop = new Category { CategoryId = 3, CategoryName = "Đồ hộp & ăn liền", Slug = "do-hop" };
        var catGiaVi = new Category { CategoryId = 4, CategoryName = "Gia vị", Slug = "gia-vi" };

        await DbContext.Categories.AddRangeAsync(catThit, catRau, catDoHop, catGiaVi);

        var pTrung = new Product { ProductId = 1, ProductName = "Trứng gà tươi Ba Huân", Price = 11400m, CostPrice = 9000m, CategoryId = 1, Status = ProductStatus.Active, Barcode = "893001" };
        var pRauMuong = new Product { ProductId = 2, ProductName = "Rau muống nước VietGAP", Price = 14000m, CostPrice = 9000m, CategoryId = 2, Status = ProductStatus.Active, Barcode = "893002" };
        var pThitHeo = new Product { ProductId = 3, ProductName = "Thịt heo xay VietGAP", Price = 45000m, CostPrice = 35000m, CategoryId = 1, Status = ProductStatus.Active, Barcode = "893003" };
        var pBo = new Product { ProductId = 4, ProductName = "Thịt bò nạc dăm", Price = 85000m, CostPrice = 65000m, CategoryId = 1, Status = ProductStatus.Active, Barcode = "893004" };
        var pNuocMam = new Product { ProductId = 5, ProductName = "Nước mắm Nam Ngư", Price = 25000m, CostPrice = 18000m, CategoryId = 4, Status = ProductStatus.Active, Barcode = "893005" };
        var pMi = new Product { ProductId = 6, ProductName = "Mì Omachi ngũ quả", Price = 8500m, CostPrice = 6000m, CategoryId = 3, Status = ProductStatus.Active, Barcode = "893006" };

        await DbContext.Products.AddRangeAsync(pTrung, pRauMuong, pThitHeo, pBo, pNuocMam, pMi);

        // Batches
        var batchNearExpiry = new InventoryBatch
        {
            BatchId = 1,
            ProductId = 2, // Rau muong
            BatchCode = "BATCH-RAU-01",
            Quantity = 15,
            ManufacturingDate = now.AddDays(-5),
            ExpiryDate = now.AddDays(2), // Near expiry (2 days left)
            Status = "Good",
            StorageLocation = "Kệ Rau 1"
        };

        var batchNormalTrung = new InventoryBatch
        {
            BatchId = 2,
            ProductId = 1,
            BatchCode = "BATCH-TRUNG-01",
            Quantity = 50,
            ManufacturingDate = now.AddDays(-2),
            ExpiryDate = now.AddDays(20),
            Status = "Good",
            StorageLocation = "Kệ Trứng 1"
        };

        var batchThit = new InventoryBatch
        {
            BatchId = 3,
            ProductId = 3,
            BatchCode = "BATCH-THIT-01",
            Quantity = 20,
            ManufacturingDate = now.AddDays(-1),
            ExpiryDate = now.AddDays(15),
            Status = "Good",
            StorageLocation = "Tủ Mát Thịt"
        };

        await DbContext.InventoryBatches.AddRangeAsync(batchNearExpiry, batchNormalTrung, batchThit);

        // Discount rule for near expiry
        var rule30 = new DiscountRule
        {
            DiscountRuleId = 1,
            Description = "Cận date 3 ngày giảm 30%",
            DaysBeforeExpiry = 3,
            DiscountPercent = 30,
            IsActive = true
        };
        await DbContext.DiscountRules.AddAsync(rule30);

        // Promotions
        var promoVoucher10k = new Promotion
        {
            PromotionId = 1,
            PromotionCode = "GIAM10K",
            PromotionName = "Giảm 10.000đ cho đơn từ 50k",
            DiscountType = "FixedAmount",
            DiscountValue = 10000m,
            MinimumOrderAmount = 50000m,
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(30),
            IsActive = true
        };

        var promoVoucher20Pct = new Promotion
        {
            PromotionId = 2,
            PromotionCode = "SALE20",
            PromotionName = "Giảm 20% cho thực phẩm tươi",
            DiscountType = "Percentage",
            DiscountValue = 20m,
            MaximumDiscountAmount = 20000m,
            MinimumOrderAmount = 30000m,
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(30),
            IsActive = true
        };

        var promoFresh20 = new Promotion
        {
            PromotionId = 3,
            PromotionCode = "FRESH20",
            PromotionName = "Giảm 20.000đ cho đơn hàng tươi từ 100k",
            DiscountType = "FixedAmount",
            DiscountValue = 20000m,
            MinimumOrderAmount = 100000m,
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(30),
            IsActive = true
        };

        await DbContext.Promotions.AddRangeAsync(promoVoucher10k, promoVoucher20Pct, promoFresh20);
        await DbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        MemoryCache.Dispose();
    }
}
