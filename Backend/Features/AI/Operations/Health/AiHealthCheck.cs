using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;
using SmartSupermarket.Backend.Features.AI.Routing.Interfaces;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Interfaces;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Health;

public class AiHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;
    private readonly IKnowledgeRetrievalService _knowledgeService;
    private readonly IPromptVersionManager _promptVersionManager;
    private readonly IModelRouter _modelRouter;

    public AiHealthCheck(
        AppDbContext dbContext,
        IKnowledgeRetrievalService knowledgeService,
        IPromptVersionManager promptVersionManager,
        IModelRouter modelRouter)
    {
        _dbContext = dbContext;
        _knowledgeService = knowledgeService;
        _promptVersionManager = promptVersionManager;
        _modelRouter = modelRouter;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var isDegraded = false;
        var description = "AI Subsystem is fully operational.";

        // 1. Database Check
        var sw = Stopwatch.StartNew();
        try
        {
            bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            sw.Stop();
            data["Database"] = new
            {
                Status = canConnect ? "Healthy" : "Degraded",
                LatencyMs = sw.ElapsedMilliseconds
            };
            if (!canConnect) isDegraded = true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            data["Database"] = new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                LatencyMs = sw.ElapsedMilliseconds
            };
            isDegraded = true;
        }

        // 2. Knowledge Retrieval RAG Store Check
        try
        {
            var testFaq = await _knowledgeService.GetExactMatchFaqAsync("chính sách đổi trả", cancellationToken);
            data["KnowledgeBase"] = new
            {
                Status = "Healthy",
                RAGGroundingAvailable = testFaq != null
            };
        }
        catch (Exception ex)
        {
            data["KnowledgeBase"] = new
            {
                Status = "Degraded",
                Error = ex.Message
            };
            isDegraded = true;
        }

        // 3. Prompt Version Manager Check
        try
        {
            string activeVer = _promptVersionManager.ActiveVersion;
            data["PromptVersioning"] = new
            {
                Status = "Healthy",
                ActiveVersion = activeVer
            };
        }
        catch (Exception ex)
        {
            data["PromptVersioning"] = new
            {
                Status = "Degraded",
                Error = ex.Message
            };
            isDegraded = true;
        }

        // 4. Model Router Check
        try
        {
            var routerDecision = await _modelRouter.RouteRequestAsync("xin chào", null, false, cancellationToken);
            data["ModelRouter"] = new
            {
                Status = "Healthy",
                DefaultLightweightModel = routerDecision.SelectedModel
            };
        }
        catch (Exception ex)
        {
            data["ModelRouter"] = new
            {
                Status = "Degraded",
                Error = ex.Message
            };
            isDegraded = true;
        }

        if (isDegraded)
        {
            return HealthCheckResult.Degraded("AI Subsystem is running in degraded mode.", data: data);
        }

        return HealthCheckResult.Healthy(description, data);
    }
}
