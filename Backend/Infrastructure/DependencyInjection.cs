using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSupermarket.Backend.Features.Auth.Repositories;
using SmartSupermarket.Backend.Features.Auth.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using SmartSupermarket.Backend.Infrastructure.Security;

using SmartSupermarket.Backend.Features.Products.Repositories;
using SmartSupermarket.Backend.Features.Products.Services;
using CategoryRepo = SmartSupermarket.Backend.Features.Categories.Repositories;
using CategorySvc = SmartSupermarket.Backend.Features.Categories.Services;
using SupplierRepo = SmartSupermarket.Backend.Features.Suppliers.Repositories;
using SupplierSvc = SmartSupermarket.Backend.Features.Suppliers.Services;

using SmartSupermarket.Backend.Features.Orders.Repositories;
using SmartSupermarket.Backend.Features.Orders.Services;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Services;
using SmartSupermarket.Backend.Features.Customers.Repositories;
using SmartSupermarket.Backend.Features.Customers.Services;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Infrastructure.External.AI;

namespace SmartSupermarket.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EF Core DbContext with PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("SmartSupermarketDb")
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Register Security Services & JWT Authentication / Authorization
        services.AddJwtAuthentication(configuration);
        services.AddScoped<JwtService>();
        services.AddScoped<PasswordHasher>();

        // Register Repositories & Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<CategoryRepo.ICategoryRepository, CategoryRepo.CategoryRepository>();
        services.AddScoped<SupplierRepo.ISupplierRepository, SupplierRepo.SupplierRepository>();
        services.AddScoped<SupplierRepo.IProductSupplierRepository, SupplierRepo.ProductSupplierRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<CategorySvc.ICategoryService, CategorySvc.CategoryService>();
        services.AddScoped<SupplierSvc.ISupplierService, SupplierSvc.SupplierService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();
        // AI Caching Subsystem (Redis with In-Memory fallback)
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = "SmartSupermarket:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddMemoryCache();

        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Cache.Interfaces.ICacheService, SmartSupermarket.Backend.Features.AI.Cache.Implementations.RedisCacheService>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Cache.Interfaces.IConversationCache, SmartSupermarket.Backend.Features.AI.Cache.Implementations.ConversationCache>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Cache.Interfaces.IKnowledgeCache, SmartSupermarket.Backend.Features.AI.Cache.Implementations.KnowledgeCache>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Cache.Interfaces.IPromptCache, SmartSupermarket.Backend.Features.AI.Cache.Implementations.PromptCache>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Cache.Interfaces.IResponseCache, SmartSupermarket.Backend.Features.AI.Cache.Implementations.ResponseCache>();

        // AI Monitoring & Observability Subsystem
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces.IAiMetricsCollector, SmartSupermarket.Backend.Features.AI.Monitoring.Implementations.AiMetricsCollector>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces.IAiMonitoringService, SmartSupermarket.Backend.Features.AI.Monitoring.Implementations.AiMonitoringService>();

        services.AddHttpClient<GeminiService>();
        services.AddHttpClient<GroqClient>();
        services.AddScoped<ILlmClient, GeminiService>();
        services.AddScoped<GeminiService>();
        services.AddHttpClient<SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.GeminiGateway>();
        services.AddHttpClient<SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.GroqGateway>();
        services.AddSingleton<SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.LocalFallbackGateway>();
        services.AddSingleton<SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.LocalGateway>();
        services.AddScoped<SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.ILLMGateway, SmartSupermarket.Backend.Infrastructure.External.AI.Gateway.LlmGateway>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Services.Prompt.IPromptBuilder, SmartSupermarket.Backend.Features.AI.Services.Prompt.PromptBuilder>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Context.IConversationContextService, SmartSupermarket.Backend.Features.AI.Services.Context.ConversationContextService>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Services.Intent.IIntentAnalyzer, SmartSupermarket.Backend.Features.AI.Services.Intent.IntentAnalyzer>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Services.Planning.IPlanner, SmartSupermarket.Backend.Features.AI.Services.Planning.Planner>();
        services.AddSingleton<SmartSupermarket.Backend.Features.AI.Dispatcher.IToolDispatcher, SmartSupermarket.Backend.Features.AI.Dispatcher.ToolDispatcher>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Orchestration.IAgentOrchestrator, SmartSupermarket.Backend.Features.AI.Orchestration.AgentOrchestrator>();

        // AI Repositories
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Repositories.IAiProductRepository, SmartSupermarket.Backend.Features.AI.Repositories.Implementations.AiProductRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Repositories.IAiInventoryBatchRepository, SmartSupermarket.Backend.Features.AI.Repositories.Implementations.AiInventoryBatchRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Repositories.IAiPromotionRepository, SmartSupermarket.Backend.Features.AI.Repositories.Implementations.AiPromotionRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Repositories.IAiRecipeRepository, SmartSupermarket.Backend.Features.AI.Repositories.Implementations.AiRecipeRepository>();

        // AI Domain Services
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiProductService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiProductService>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiInventoryService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiInventoryService>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiPromotionService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiPromotionService>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiRecipeService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiRecipeService>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiBudgetService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiBudgetService>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Services.Interfaces.IAiCartService, SmartSupermarket.Backend.Features.AI.Services.Implementations.AiCartService>();

        // AI Tools
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.IInventoryTool, SmartSupermarket.Backend.Features.AI.Tools.InventoryTool>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.IProductTool, SmartSupermarket.Backend.Features.AI.Tools.ProductTool>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.IPromotionTool, SmartSupermarket.Backend.Features.AI.Tools.PromotionTool>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.IRecipeTool, SmartSupermarket.Backend.Features.AI.Tools.RecipeTool>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.IBudgetTool, SmartSupermarket.Backend.Features.AI.Tools.BudgetTool>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Tools.ICartTool, SmartSupermarket.Backend.Features.AI.Tools.CartTool>();

        // AI Agents
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.ShoppingAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.InventoryAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.PromotionAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.RecipeAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.BudgetAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.CartAgent>();

        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.ShoppingAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.InventoryAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.PromotionAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.RecipeAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.BudgetAgent>();
        services.AddScoped<SmartSupermarket.Backend.Features.AI.Agents.Common.IAgent, SmartSupermarket.Backend.Features.AI.Agents.CartAgent>();

        services.AddScoped<IAiService, AiService>();
        services.AddScoped<IAiRetailService, AiRetailService>();
        services.AddScoped<IAiShoppingConciergeService, AiShoppingConciergeService>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.Interfaces.IShelfLifeProcessor, SmartSupermarket.Backend.Features.Inventory.Services.ShelfLifeProcessor>();
        services.AddHostedService<SmartSupermarket.Backend.Features.Inventory.BackgroundServices.ShelfLifeBackgroundService>();

        // Module 05_Inventory
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Repositories.IInventoryRepository, SmartSupermarket.Backend.Features.Inventory.Repositories.InventoryRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Repositories.IDiscountRuleRepository, SmartSupermarket.Backend.Features.Inventory.Repositories.DiscountRuleRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Repositories.IShelfLifeRepository, SmartSupermarket.Backend.Features.Inventory.Repositories.ShelfLifeRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.Interfaces.IInventoryService, SmartSupermarket.Backend.Features.Inventory.Services.InventoryService>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.Interfaces.IDiscountRuleService, SmartSupermarket.Backend.Features.Inventory.Services.DiscountRuleService>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.Interfaces.IStockAuditService, SmartSupermarket.Backend.Features.Inventory.Services.StockAuditService>();

        // Module 06_Import
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Repositories.IImportReceiptRepository, SmartSupermarket.Backend.Features.Import.Repositories.ImportReceiptRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Repositories.IImportDetailRepository, SmartSupermarket.Backend.Features.Import.Repositories.ImportDetailRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Services.IImportReceiptService, SmartSupermarket.Backend.Features.Import.Services.ImportReceiptService>();

        // Module 09_Payment
        services.Configure<SmartSupermarket.Backend.Infrastructure.Payments.ZaloPayOptions>(configuration.GetSection(SmartSupermarket.Backend.Infrastructure.Payments.ZaloPayOptions.SectionName));
        services.AddHttpClient<SmartSupermarket.Backend.Features.Payments.Services.IZaloPayService, SmartSupermarket.Backend.Features.Payments.Services.ZaloPayService>();
        services.AddScoped<SmartSupermarket.Backend.Features.Payments.Services.IPaymentService, SmartSupermarket.Backend.Features.Payments.Services.PaymentService>();

        // Phân hệ 8: Nhân viên & Ca trực (ShiftSession & Cash Balancing)
        services.AddScoped<SmartSupermarket.Backend.Features.Employees.Services.IShiftSessionService, SmartSupermarket.Backend.Features.Employees.Services.ShiftSessionService>();

        return services;
    }
}
