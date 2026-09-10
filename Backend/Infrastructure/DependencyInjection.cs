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
            options.UseNpgsql(connectionString));

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

        return services;
    }
}
