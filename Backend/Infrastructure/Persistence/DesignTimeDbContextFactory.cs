using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SmartSupermarket.Backend.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI migrations targeting PostgreSQL.
/// This ensures 'dotnet ef migrations add' uses Npgsql provider instead of InMemory.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        
        // If running from solution root, navigate into Backend if needed
        var backendPath = Path.Combine(basePath, "Backend");
        if (Directory.Exists(backendPath) && File.Exists(Path.Combine(backendPath, "appsettings.json")))
        {
            basePath = backendPath;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=AISupermarketDB;Username=postgres;Password=YourPassword123!";

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);

        return new AppDbContext(builder.Options);
    }
}
