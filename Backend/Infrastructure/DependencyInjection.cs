using Microsoft.Extensions.DependencyInjection;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Security Services
        services.AddScoped<JwtService>();
        services.AddScoped<PasswordHasher>();

        // TODO: Register DbContext, Repositories, AI Clients

        return services;
    }
}
