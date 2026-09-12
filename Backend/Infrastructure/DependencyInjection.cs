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

namespace SmartSupermarket.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EF Core DbContext with PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
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
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionService, PromotionService>();

        // Module 05_Inventory
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Repositories.IInventoryRepository, SmartSupermarket.Backend.Features.Inventory.Repositories.InventoryRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Repositories.IDiscountRuleRepository, SmartSupermarket.Backend.Features.Inventory.Repositories.DiscountRuleRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.IInventoryService, SmartSupermarket.Backend.Features.Inventory.Services.InventoryService>();
        services.AddScoped<SmartSupermarket.Backend.Features.Inventory.Services.IDiscountRuleService, SmartSupermarket.Backend.Features.Inventory.Services.DiscountRuleService>();

        // Module 06_Import
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Repositories.IImportReceiptRepository, SmartSupermarket.Backend.Features.Import.Repositories.ImportReceiptRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Repositories.IImportDetailRepository, SmartSupermarket.Backend.Features.Import.Repositories.ImportDetailRepository>();
        services.AddScoped<SmartSupermarket.Backend.Features.Import.Services.IImportReceiptService, SmartSupermarket.Backend.Features.Import.Services.ImportReceiptService>();

        return services;
    }
}
