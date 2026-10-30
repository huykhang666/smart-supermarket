using Microsoft.OpenApi.Models;
using Serilog;
using SmartSupermarket.Backend.Infrastructure;
using SmartSupermarket.Backend.Infrastructure.Logging;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using SmartSupermarket.Backend.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with structured logging & daily file rotation
builder.ConfigureSerilog();

// Add Observability (CorrelationId, OpenTelemetry tracing)
builder.Services.AddObservability();

// Add ASP.NET Core Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<SmartSupermarket.Backend.Features.AI.Health.AiHealthCheck>("ai_subsystem");

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Swagger with JWT Bearer Token support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart SuperMarket API",
        Version = "v1",
        Description = "REST API Specification cho Hệ thống Quản lý Siêu thị Thông minh Smart SuperMarket"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán chuỗi JWT Token của bạn vào đây (không cần gõ chữ Bearer)"
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
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
app.UseCorrelationId();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

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

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapControllers();

try
{
    Log.Information("Khởi động máy chủ Smart SuperMarket Backend...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Máy chủ Smart SuperMarket Backend bị dừng đột ngột!");
}
finally
{
    Log.CloseAndFlush();
}
