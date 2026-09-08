using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Infrastructure.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? new JwtOptions();

        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        // 2. Add JWT Bearer Authentication Scheme
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 3. Add Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
            options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Manager.ToString()));
            options.AddPolicy("StaffOrAbove", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Manager.ToString(), UserRole.Staff.ToString()));
            options.AddPolicy("CustomerOnly", policy => policy.RequireRole(UserRole.Customer.ToString()));
        });

        return services;
    }
}
