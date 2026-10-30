using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Features.AI.Optimization.Interfaces;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Interfaces;
using SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai/monitoring")]
[Route("api/ai/monitoring")]
[Authorize(Roles = "Admin,Manager")]
public class AiMonitoringController : ControllerBase
{
    private readonly IAiMonitoringService _monitoringService;
    private readonly IPromptVersionManager _promptVersionManager;
    private readonly IAiExperimentService _experimentService;
    private readonly ILogger<AiMonitoringController> _logger;

    public AiMonitoringController(
        IAiMonitoringService monitoringService,
        ILogger<AiMonitoringController> logger)
        : this(monitoringService,
               new PromptVersioning.Implementations.PromptVersionManager(),
               new Optimization.Implementations.AiExperimentService(),
               logger)
    {
    }

    public AiMonitoringController(
        IAiMonitoringService monitoringService,
        IPromptVersionManager promptVersionManager,
        IAiExperimentService experimentService,
        ILogger<AiMonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _promptVersionManager = promptVersionManager;
        _experimentService = experimentService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy toàn bộ dữ liệu thống kê tổng hợp cho AI Monitoring Dashboard (Metrics, Latency, Tool Duration, Tokens, Cost).
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResult<AiMonitoringDashboardDto>), 200)]
    public async Task<ActionResult<ApiResult<AiMonitoringDashboardDto>>> GetDashboard(
        CancellationToken cancellationToken = default)
    {
        var dashboard = await _monitoringService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResult<AiMonitoringDashboardDto>.Success(dashboard));
    }

    /// <summary>
    /// Lấy chỉ số tổng quan nhanh (Total Requests, Tokens, Chi phí USD/VND, Tỉ lệ Cache Hit).
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResult<AiOverviewMetricsDto>), 200)]
    public async Task<ActionResult<ApiResult<AiOverviewMetricsDto>>> GetMetrics(
        CancellationToken cancellationToken = default)
    {
        var overview = await _monitoringService.GetOverviewMetricsAsync(cancellationToken);
        return Ok(ApiResult<AiOverviewMetricsDto>.Success(overview));
    }

    /// <summary>
    /// Đặt lại các bộ đếm thống kê giám sát AI (yêu cầu quyền Admin).
    /// </summary>
    [HttpPost("reset")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public ActionResult<ApiResult> ResetMetrics()
    {
        _monitoringService.ResetMetrics();
        return Ok(ApiResult.Success("Đã làm mới toàn bộ bộ đếm AI Monitoring."));
    }

    /// <summary>
    /// Lấy danh sách các phiên bản prompt đang lưu trữ và token tiêu thụ tương ứng.
    /// </summary>
    [HttpGet("prompt-versions")]
    public ActionResult<ApiResult<object>> GetPromptVersions()
    {
        var history = _promptVersionManager.GetVersionHistory(PromptType.SystemIdentity);
        var activePrompt = _promptVersionManager.GetActivePrompt(PromptType.SystemIdentity);

        return Ok(ApiResult<object>.Success(new
        {
            ActiveVersion = _promptVersionManager.ActiveVersion,
            CurrentPrompt = activePrompt,
            History = history
        }, "Lấy thông tin phiên bản Prompt thành công."));
    }

    /// <summary>
    /// Chuyển đổi phiên bản prompt đang áp dụng (hot-swap, yêu cầu quyền Admin).
    /// </summary>
    [HttpPost("prompt-versions/active")]
    [Authorize(Roles = "Admin")]
    public ActionResult<ApiResult<object>> SetActivePromptVersion([FromBody] SetPromptVersionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Version))
        {
            return BadRequest(ApiResult.Failure("Phiên bản target không được để trống."));
        }

        _promptVersionManager.SetActiveVersion(request.Version);
        _logger.LogInformation("[Production Operations] Admin đã chuyển đổi Prompt Active sang: {Version}", request.Version);

        return Ok(ApiResult<object>.Success(new
        {
            ActiveVersion = _promptVersionManager.ActiveVersion
        }, $"Đã kích hoạt phiên bản Prompt '{request.Version}' thành công."));
    }

    /// <summary>
    /// Rollback tức thì phiên bản Prompt về phiên bản trước đó (yêu cầu quyền Admin).
    /// </summary>
    [HttpPost("prompt-versions/rollback")]
    [Authorize(Roles = "Admin")]
    public ActionResult<ApiResult<PromptRollbackResultDto>> RollbackPromptVersion([FromBody] RollbackPromptVersionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetVersion))
        {
            return BadRequest(ApiResult.Failure("TargetVersion không được để trống."));
        }

        var result = _promptVersionManager.RollbackToVersion(request.TargetVersion);
        if (!result.Success)
        {
            return BadRequest(ApiResult<PromptRollbackResultDto>.Failure(result.Message));
        }

        _logger.LogWarning("[Production Operations] ROLLBACK PROMPT THÀNH CÔNG: Từ {OldVer} về {RestoredVer}",
            result.PreviousVersion, result.RestoredVersion);

        return Ok(ApiResult<PromptRollbackResultDto>.Success(result, result.Message));
    }

    /// <summary>
    /// Lấy thống kê phân bổ A/B testing và độ trễ thực tế của nhóm Control vs Treatment.
    /// </summary>
    [HttpGet("experiments")]
    public ActionResult<ApiResult<object>> GetExperiments()
    {
        var stats = _experimentService.GetExperimentStats();
        return Ok(ApiResult<object>.Success(new
        {
            ControlGroup = new
            {
                Name = "Control_v3",
                Count = stats.ControlCount,
                AvgLatencyMs = stats.AvgControlLatencyMs,
                PromptVersion = "v3.0.0-rag"
            },
            TreatmentGroup = new
            {
                Name = "Variant_A_Optimized",
                Count = stats.TreatmentCount,
                AvgLatencyMs = stats.AvgTreatmentLatencyMs,
                PromptVersion = "v4.0.0-optimized"
            },
            LatencyReductionMs = stats.AvgControlLatencyMs - stats.AvgTreatmentLatencyMs
        }, "Lấy thống kê A/B Experiment thành công."));
    }
}

public class SetPromptVersionRequest
{
    public string Version { get; set; } = string.Empty;
}

public class RollbackPromptVersionRequest
{
    public string TargetVersion { get; set; } = string.Empty;
}
