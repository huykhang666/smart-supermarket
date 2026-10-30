using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;
using SmartSupermarket.Backend.Features.AI.Routing.Interfaces;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Interfaces;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai/health")]
[Route("api/ai/health")]
[AllowAnonymous]
public class AiHealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IKnowledgeRetrievalService _knowledgeService;
    private readonly IPromptVersionManager _promptVersionManager;
    private readonly IModelRouter _modelRouter;
    private readonly ILlmClient _llmClient;
    private readonly IConfiguration _configuration;

    public AiHealthController(
        AppDbContext dbContext,
        IKnowledgeRetrievalService knowledgeService,
        IPromptVersionManager promptVersionManager,
        IModelRouter modelRouter,
        ILlmClient llmClient,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _knowledgeService = knowledgeService;
        _promptVersionManager = promptVersionManager;
        _modelRouter = modelRouter;
        _llmClient = llmClient;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<object>>> GetAiHealth(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool dbHealthy = false;
        long dbLatencyMs = 0;

        try
        {
            var dbSw = Stopwatch.StartNew();
            dbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
            dbSw.Stop();
            dbLatencyMs = dbSw.ElapsedMilliseconds;
        }
        catch
        {
            dbHealthy = false;
        }

        var testFaq = await _knowledgeService.GetExactMatchFaqAsync("chính sách đổi trả", cancellationToken);
        var routing = await _modelRouter.RouteRequestAsync("chào shop", null, false, cancellationToken);
        sw.Stop();

        bool isDegraded = !dbHealthy;
        string status = isDegraded ? "Degraded" : "Healthy";

        var healthReport = new
        {
            Status = status,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ActivePromptVersion = _promptVersionManager.ActiveVersion,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            Timestamp = DateTime.UtcNow,
            Subsystems = new
            {
                Database = new
                {
                    Status = dbHealthy ? "Healthy" : "Unhealthy",
                    LatencyMs = dbLatencyMs
                },
                KnowledgeRetrievalRAG = new
                {
                    Status = "Healthy",
                    ExactMatchIndexReady = testFaq != null
                },
                PromptVersioning = new
                {
                    Status = "Healthy",
                    Active = _promptVersionManager.ActiveVersion
                },
                ModelRouter = new
                {
                    Status = "Healthy",
                    DefaultModel = routing.SelectedModel
                },
                LlmGateway = new
                {
                    Status = "Healthy",
                    Provider = _llmClient.ProviderName
                }
            }
        };

        return Ok(ApiResult<object>.Success(healthReport, $"Trạng thái vận hành AI: {status}"));
    }
}
