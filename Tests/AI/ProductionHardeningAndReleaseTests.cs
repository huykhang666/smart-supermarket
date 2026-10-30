using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.Controllers;
using SmartSupermarket.Backend.Features.AI.Health;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Features.AI.Optimization.Implementations;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Implementations;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;
using SmartSupermarket.Backend.Features.AI.Routing.Implementations;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Implementations;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class ProductionHardeningAndReleaseTests
{
    // ==========================================
    // 1. SECRETS & ENVIRONMENT CONFIGURATION TESTS
    // ==========================================
    [Fact]
    public void GeminiService_PrioritizesEnvironmentVariableOverStaticConfig()
    {
        var testEnvKey = "AIzaSy_TEST_ENV_OVERRIDE_KEY";
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", testEnvKey);
        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Gemini:ApiKey"]).Returns("AIzaSy_STATIC_FILE_KEY");
            configMock.Setup(c => c["Gemini:Model"]).Returns("gemini-2.0-flash");

            var geminiService = new GeminiService(new HttpClient(), configMock.Object);

            // Access private _apiKey via reflection to verify it resolved from env var
            var apiKeyField = typeof(GeminiService).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(apiKeyField);
            var resolvedKey = apiKeyField.GetValue(geminiService) as string;

            Assert.Equal(testEnvKey, resolvedKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
        }
    }

    [Fact]
    public void GroqClient_PrioritizesEnvironmentVariableOverStaticConfig()
    {
        var testEnvKey = "gsk_TEST_ENV_OVERRIDE_KEY";
        Environment.SetEnvironmentVariable("GROQ_API_KEY", testEnvKey);
        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Groq:ApiKey"]).Returns("gsk_STATIC_FILE_KEY");
            configMock.Setup(c => c["Groq:Model"]).Returns("llama-3.3-70b-versatile");

            var groqClient = new GroqClient(new HttpClient(), configMock.Object);

            var apiKeyField = typeof(GroqClient).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(apiKeyField);
            var resolvedKey = apiKeyField.GetValue(groqClient) as string;

            Assert.Equal(testEnvKey, resolvedKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GROQ_API_KEY", null);
        }
    }

    // ==========================================
    // 2. AUTHORIZATION & ACCESS CONTROL TESTS
    // ==========================================
    [Fact]
    public void AiMonitoringController_IsProtectedByRoleBasedAuthorization()
    {
        var controllerType = typeof(AiMonitoringController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttr);
        Assert.Contains("Admin", authAttr.Roles);
        Assert.Contains("Manager", authAttr.Roles);
    }

    [Fact]
    public void AiMonitoringController_ResetMetrics_IsRestrictedStrictlyToAdmin()
    {
        var method = typeof(AiMonitoringController).GetMethod(nameof(AiMonitoringController.ResetMetrics));
        Assert.NotNull(method);

        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Admin", authAttr.Roles);
    }

    [Fact]
    public void AiMonitoringController_PromptManagement_IsRestrictedToAdmin()
    {
        var setActiveMethod = typeof(AiMonitoringController).GetMethod(nameof(AiMonitoringController.SetActivePromptVersion));
        Assert.NotNull(setActiveMethod);
        var setActiveAuth = setActiveMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(setActiveAuth);
        Assert.Equal("Admin", setActiveAuth.Roles);

        var rollbackMethod = typeof(AiMonitoringController).GetMethod(nameof(AiMonitoringController.RollbackPromptVersion));
        Assert.NotNull(rollbackMethod);
        var rollbackAuth = rollbackMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(rollbackAuth);
        Assert.Equal("Admin", rollbackAuth.Roles);
    }

    [Fact]
    public void AiRetailController_IsProtectedByRoleBasedAuthorization()
    {
        var controllerType = typeof(AiRetailController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttr);
        Assert.Contains("Admin", authAttr.Roles);
        Assert.Contains("Manager", authAttr.Roles);
    }

    [Fact]
    public void AiController_CustomerShoppingEndpoints_RemainPubliclyAccessible()
    {
        var mealPlanMethod = typeof(AiController).GetMethod(nameof(AiController.PlanMeal));
        Assert.NotNull(mealPlanMethod);
        Assert.NotNull(mealPlanMethod.GetCustomAttribute<AllowAnonymousAttribute>());

        var chatMethod = typeof(AiController).GetMethod(nameof(AiController.Chat));
        Assert.NotNull(chatMethod);
        Assert.NotNull(chatMethod.GetCustomAttribute<AllowAnonymousAttribute>());

        var injectCartMethod = typeof(AiController).GetMethod(nameof(AiController.InjectCart));
        Assert.NotNull(injectCartMethod);
        Assert.NotNull(injectCartMethod.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    // ==========================================
    // 3. HEALTH CHECK & READINESS TESTS
    // ==========================================
    [Fact]
    public async Task AiHealthCheck_ReturnsHealthyWhenSubsystemsAreFunctional()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var knowledgeService = new KnowledgeRetrievalService();
        var promptManager = new PromptVersionManager();
        var modelRouter = new ModelRouter();

        var healthCheck = new AiHealthCheck(dbContext, knowledgeService, promptManager, modelRouter);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(result.Data.ContainsKey("Database"));
        Assert.True(result.Data.ContainsKey("KnowledgeBase"));
        Assert.True(result.Data.ContainsKey("PromptVersioning"));
        Assert.True(result.Data.ContainsKey("ModelRouter"));
    }

    [Fact]
    public async Task AiHealthController_GetAiHealth_ReturnsStructuredDiagnosticReport()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var knowledgeService = new KnowledgeRetrievalService();
        var promptManager = new PromptVersionManager();
        var modelRouter = new ModelRouter();
        var mockLlm = new Mock<ILlmClient>();
        mockLlm.Setup(l => l.ProviderName).Returns("Gemini");

        var configMock = new Mock<IConfiguration>();

        var healthController = new AiHealthController(
            dbContext,
            knowledgeService,
            promptManager,
            modelRouter,
            mockLlm.Object,
            configMock.Object);

        var actionResult = await healthController.GetAiHealth();
        var okResult = actionResult.Result as OkObjectResult;

        Assert.NotNull(okResult);
        var apiResult = okResult.Value as ApiResult<object>;
        Assert.NotNull(apiResult);
        Assert.True(apiResult.IsSuccess);
    }

    // ==========================================
    // 4. OUTPUT HYGIENE & PRODUCTION ERROR SAFETY
    // ==========================================
    [Fact]
    public void AiController_GetSafeErrorMessage_SanitizesStackTracesInProduction()
    {
        // When ASPNETCORE_ENVIRONMENT is Production (or anything other than Development)
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        try
        {
            var exception = new InvalidOperationException("PostgreSQL Socket Connection Timeout at 10.0.0.1:5432");
            var method = typeof(AiController).GetMethod("GetSafeErrorMessage", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var safeMsg = method.Invoke(null, new object[] { exception, "Lỗi máy chủ nội bộ" }) as string;

            // In production, must NOT leak internal socket IP or connection string
            Assert.Equal("Lỗi máy chủ nội bộ", safeMsg);
            Assert.DoesNotContain("10.0.0.1", safeMsg);
            Assert.DoesNotContain("PostgreSQL", safeMsg);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    // ==========================================
    // 5. ROLLBACK & OPERATIONAL LIFECYCLE TESTS
    // ==========================================
    [Fact]
    public void AiMonitoringController_RollbackPromptVersion_ExecutesSafeRollback()
    {
        var monitoringMock = new Mock<IAiMonitoringService>();
        var promptManager = new PromptVersionManager();
        var experimentService = new AiExperimentService();
        var loggerMock = new Mock<ILogger<AiMonitoringController>>();

        var controller = new AiMonitoringController(
            monitoringMock.Object,
            promptManager,
            experimentService,
            loggerMock.Object);

        Assert.Equal("v4.0.0-optimized", promptManager.ActiveVersion);

        // Perform live rollback via controller
        var actionResult = controller.RollbackPromptVersion(new RollbackPromptVersionRequest
        {
            TargetVersion = "v3.0.0-rag"
        });

        var okResult = actionResult.Result as OkObjectResult;
        Assert.NotNull(okResult);

        Assert.Equal("v3.0.0-rag", promptManager.ActiveVersion);
        var activePrompt = promptManager.GetActivePrompt(PromptType.SystemIdentity);
        Assert.Equal("v3.0.0-rag", activePrompt.Version);
    }
}
