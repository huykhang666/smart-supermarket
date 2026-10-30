using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.Optimization.Implementations;
using SmartSupermarket.Backend.Features.AI.Optimization.Interfaces;
using SmartSupermarket.Backend.Features.AI.Optimization.Models;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Implementations;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;
using SmartSupermarket.Backend.Features.AI.Routing.Implementations;
using SmartSupermarket.Backend.Features.AI.Routing.Interfaces;
using SmartSupermarket.Backend.Features.AI.Routing.Models;
using SmartSupermarket.Backend.Features.AI.Services;
using SmartSupermarket.Backend.Features.AI.Services.Implementations;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Implementations;
using SmartSupermarket.Backend.Features.AI.Tools;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class OptimizationAndProductionTests
{
    // ==========================================
    // 1. PROMPT VERSIONING TESTS
    // ==========================================
    [Fact]
    public void PromptVersionManager_InitializesWithV4OptimizedActive()
    {
        var manager = new PromptVersionManager();

        Assert.Equal("v4.0.0-optimized", manager.ActiveVersion);

        var activeSystem = manager.GetActivePrompt(PromptType.SystemIdentity);
        Assert.NotNull(activeSystem);
        Assert.Equal("v4.0.0-optimized", activeSystem.Version);
        Assert.True(activeSystem.IsActive);

        // V4 Optimized prompt must demonstrate token reduction compared to V3 RAG prompt
        var v3Prompt = manager.GetPromptByVersion(PromptType.SystemIdentity, "v3.0.0-rag");
        Assert.NotNull(v3Prompt);
        Assert.True(activeSystem.EstimatedTokens < v3Prompt.EstimatedTokens,
            $"Expected v4 tokens ({activeSystem.EstimatedTokens}) < v3 tokens ({v3Prompt.EstimatedTokens})");
    }

    [Fact]
    public void PromptVersionManager_CanRegisterAndSwitchActiveVersion()
    {
        var manager = new PromptVersionManager();

        var newPrompt = new PromptVersionDto
        {
            Version = "v4.1.0-streamlined",
            Type = PromptType.RetailConcierge,
            Content = "You are a concise retail concierge.",
            Changelog = "Streamlined token count by another 10%",
            EstimatedTokens = 120,
            IsActive = false
        };

        manager.RegisterVersion(newPrompt);
        manager.SetActiveVersion("v4.1.0-streamlined");

        Assert.Equal("v4.1.0-streamlined", manager.ActiveVersion);
        var activePrompt = manager.GetActivePrompt(PromptType.RetailConcierge);
        Assert.Equal("v4.1.0-streamlined", activePrompt.Version);
        Assert.Equal("You are a concise retail concierge.", activePrompt.Content);
    }

    [Fact]
    public void PromptVersionManager_CanRollbackToPreviousVersion()
    {
        var manager = new PromptVersionManager();
        Assert.Equal("v4.0.0-optimized", manager.ActiveVersion);

        var rollbackResult = manager.RollbackToVersion("v3.0.0-rag");

        Assert.True(rollbackResult.Success);
        Assert.Equal("v4.0.0-optimized", rollbackResult.PreviousVersion);
        Assert.Equal("v3.0.0-rag", rollbackResult.RestoredVersion);
        Assert.Equal("v3.0.0-rag", manager.ActiveVersion);

        var currentSystem = manager.GetActivePrompt(PromptType.SystemIdentity);
        Assert.Equal("v3.0.0-rag", currentSystem.Version);
    }

    [Fact]
    public void PromptVersionManager_RollbackToInvalidVersion_FailsGracefully()
    {
        var manager = new PromptVersionManager();
        var result = manager.RollbackToVersion("v99.9.9-nonexistent");

        Assert.False(result.Success);
        Assert.True(result.Message.Contains("không tồn tại", StringComparison.OrdinalIgnoreCase) ||
                    result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("v4.0.0-optimized", manager.ActiveVersion);
    }

    // ==========================================
    // 2. MODEL ROUTER TESTS
    // ==========================================
    [Fact]
    public async Task ModelRouter_RoutesExactFaqMatch_ToDeterministicOfflineTier()
    {
        var router = new ModelRouter();

        var decision = await router.RouteRequestAsync("Chính sách đổi trả rau củ thế nào?", hasExactFaqMatch: true);

        Assert.Equal(RoutingModelTier.DeterministicOffline, decision.Tier);
        Assert.Equal("local-deterministic-rag", decision.SelectedModel);
        Assert.Equal(0.0m, decision.EstimatedCostUsd);
        Assert.True(decision.EstimatedLatencyMs <= 10);
    }

    [Fact]
    public async Task ModelRouter_RoutesOutOfDomain_ToDeterministicGuardrailTier()
    {
        var router = new ModelRouter();

        var decision = await router.RouteRequestAsync("Thời tiết hôm nay thế nào?", hasExactFaqMatch: false);

        Assert.Equal(RoutingModelTier.DeterministicOffline, decision.Tier);
        Assert.Equal("local-deterministic-guardrail", decision.SelectedModel);
        Assert.Equal(0.0m, decision.EstimatedCostUsd);
    }

    [Fact]
    public async Task ModelRouter_RoutesCartActions_ToDeterministicOfflineTier()
    {
        var router = new ModelRouter();

        var decision = await router.RouteRequestAsync(
            "thêm vào giỏ",
            new UserIntentDto { Type = IntentType.CartAction, RawPrompt = "thêm vào giỏ" });

        Assert.Equal(RoutingModelTier.DeterministicOffline, decision.Tier);
        Assert.Equal("local-deterministic-cart", decision.SelectedModel);
        Assert.Equal(0.0m, decision.EstimatedCostUsd);
    }

    [Fact]
    public async Task ModelRouter_RoutesGreeting_ToFastLightweightTier()
    {
        var router = new ModelRouter();

        var decision = await router.RouteRequestAsync("Xin chào shop");

        Assert.Equal(RoutingModelTier.FastLightweight, decision.Tier);
        Assert.Equal("groq-llama3-8b", decision.SelectedModel);
        Assert.True(decision.EstimatedCostUsd < 0.0001m);
    }

    [Fact]
    public async Task ModelRouter_RoutesCategoryComparison_ToStrongReasoningTier()
    {
        var router = new ModelRouter();

        var decision = await router.RouteRequestAsync(
            "So sánh nước giặt Omo và Ariel loại nào tiết kiệm hơn?",
            new UserIntentDto { Type = IntentType.CategoryComparison, RawPrompt = "so sánh" });

        Assert.Equal(RoutingModelTier.StrongReasoning, decision.Tier);
        Assert.Equal("gemini-2.5-flash", decision.SelectedModel);
        Assert.True(decision.EstimatedCostUsd > 0.0001m);
    }

    // ==========================================
    // 3. A/B EXPERIMENTATION TESTS
    // ==========================================
    [Fact]
    public void AiExperimentService_AssignsVariantsDeterministically()
    {
        var service = new AiExperimentService();

        var v1 = service.AssignVariant("user_session_123");
        var v2 = service.AssignVariant("user_session_123");

        Assert.Equal(v1.VariantKey, v2.VariantKey);
        Assert.Equal(v1.PromptVersion, v2.PromptVersion);
    }

    [Fact]
    public void AiExperimentService_TracksTelemetryAndComputesRunningStats()
    {
        var service = new AiExperimentService();

        service.RecordExperimentOutcome(new RequestTelemetryDto
        {
            ExperimentVariant = "Control_v3",
            LatencyMs = 1200,
            CostUsd = 0.0005m
        });
        service.RecordExperimentOutcome(new RequestTelemetryDto
        {
            ExperimentVariant = "Control_v3",
            LatencyMs = 1000,
            CostUsd = 0.0005m
        });
        service.RecordExperimentOutcome(new RequestTelemetryDto
        {
            ExperimentVariant = "Variant_A_Optimized",
            LatencyMs = 600,
            CostUsd = 0.0002m
        });
        service.RecordExperimentOutcome(new RequestTelemetryDto
        {
            ExperimentVariant = "Variant_A_Optimized",
            LatencyMs = 500,
            CostUsd = 0.0002m
        });

        var stats = service.GetExperimentStats();

        Assert.Equal(2, stats.ControlCount);
        Assert.Equal(2, stats.TreatmentCount);
        Assert.Equal(1100.0, stats.AvgControlLatencyMs);
        Assert.Equal(550.0, stats.AvgTreatmentLatencyMs);
        Assert.True(stats.AvgTreatmentLatencyMs < stats.AvgControlLatencyMs, "Treatment must be faster than Control");
    }

    // ==========================================
    // 4. END-TO-END CONCIERGE INTEGRATION
    // ==========================================
    [Fact]
    public async Task PlanMealAndShopAsync_EnrichesResponseWithOptimizationMetadata()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        // Seed product
        var cat = new Category { CategoryId = 1, CategoryName = "Thịt tươi" };
        dbContext.Categories.Add(cat);
        var prod = new Product
        {
            ProductId = 1,
            ProductName = "Thịt ba chỉ MeatDeli",
            CategoryId = 1,
            Price = 65000,
            Barcode = "8935001001",
            Unit = "Khay 400g"
        };
        dbContext.Products.Add(prod);
        dbContext.InventoryBatches.Add(new InventoryBatch
        {
            BatchId = 1,
            ProductId = 1,
            Quantity = 100,
            ExpiryDate = DateTime.UtcNow.AddDays(5)
        });
        await dbContext.SaveChangesAsync();

        var mockLlm = new Mock<ILlmClient>();
        mockLlm.Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), false, default))
            .ReturnsAsync("Gợi ý thực đơn ba chỉ chiên thơm ngon cho gia đình.");

        var promptManager = new PromptVersionManager();
        var modelRouter = new ModelRouter();
        var experimentService = new AiExperimentService();

        var retailTool = new RetailTool(dbContext);
        var retailService = new RetailShoppingIntelligenceService(dbContext, retailTool, mockLlm.Object);
        var knowledgeService = new KnowledgeRetrievalService();

        var concierge = new AiShoppingConciergeService(
            dbContext,
            mockLlm.Object,
            retailService,
            knowledgeService,
            promptManager,
            modelRouter,
            experimentService);

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Gợi ý mâm cơm 4 người 100k thịt ba chỉ",
            TargetBudget = 100000,
            NumberOfPeople = 4
        };

        var response = await concierge.PlanMealAndShopAsync(request);

        // Verify Phase 4 enriched properties
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.ModelRouted));
        Assert.False(string.IsNullOrWhiteSpace(response.PromptVersion));
        Assert.False(string.IsNullOrWhiteSpace(response.ExperimentVariant));
        Assert.True(response.ExecutionLatencyMs >= 0);
        Assert.True(response.EstimatedCostUsd >= 0.0m);
    }

    [Fact]
    public async Task PlanMealAndShopAsync_FaqQuery_SetsCacheHitTrueAndDeterministicModel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var mockLlm = new Mock<ILlmClient>();
        var concierge = new AiShoppingConciergeService(dbContext, mockLlm.Object);

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Chính sách đổi trả hàng tươi sống thế nào?"
        };

        var response = await concierge.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.True(response.IsCacheHit);
        Assert.Equal("local-deterministic-rag", response.ModelRouted);
        Assert.Equal(0.0m, response.EstimatedCostUsd);
    }

    [Fact]
    public async Task PlanMealAndShopAsync_OutOfDomainQuery_RoutesToDeterministicGuardrail()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var mockLlm = new Mock<ILlmClient>();
        var concierge = new AiShoppingConciergeService(dbContext, mockLlm.Object);

        var request = new MealPlanChatRequest
        {
            UserPrompt = "Thời tiết ngày mai tại Hà Nội thế nào?"
        };

        var response = await concierge.PlanMealAndShopAsync(request);

        Assert.NotNull(response);
        Assert.False(response.HasMealSuggestion);
        Assert.Equal("local-deterministic-guardrail", response.ModelRouted);
        Assert.Equal(0.0m, response.EstimatedCostUsd);
    }
}
