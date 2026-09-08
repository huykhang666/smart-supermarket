using Microsoft.OpenApi;
using SmartSupermarket.Backend.Infrastructure;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using SmartSupermarket.Backend.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT Bearer Token support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart SuperMarket API",
        Version = "v1",
        Description = "REST API Specification cho Hệ thống Quản lý Siêu thị Thông minh Smart SuperMarket"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Bearer Token theo định dạng: Bearer {your_jwt_token}"
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    options.AddSecurityRequirement((doc) => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

// Extension method registering Infrastructure layer (DbContext, Security, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Auto seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<PasswordHasher>();
        await DbInitializer.SeedDataAsync(dbContext, passwordHasher);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình Seed Data CSDL.");
    }
}

// Middleware Pipeline
app.UseStaticFiles();
app.UseRouting();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart SuperMarket API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// Auto Redirect root / to /swagger/index.html
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));

app.MapControllers();

app.Run();
