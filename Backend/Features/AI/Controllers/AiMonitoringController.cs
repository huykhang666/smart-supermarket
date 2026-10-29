using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Controllers;

[ApiController]
[Route("api/v1/ai/monitoring")]
[Route("api/ai/monitoring")]
public class AiMonitoringController : ControllerBase
{
    private readonly IAiMonitoringService _monitoringService;
    private readonly ILogger<AiMonitoringController> _logger;

    public AiMonitoringController(
        IAiMonitoringService monitoringService,
        ILogger<AiMonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy toàn bộ dữ liệu thống kê tổng hợp cho AI Monitoring Dashboard (Metrics, Latency, Tool Duration, Tokens, Cost).
    /// </summary>
    [HttpGet("dashboard")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AiOverviewMetricsDto>), 200)]
    public async Task<ActionResult<ApiResult<AiOverviewMetricsDto>>> GetMetrics(
        CancellationToken cancellationToken = default)
    {
        var overview = await _monitoringService.GetOverviewMetricsAsync(cancellationToken);
        return Ok(ApiResult<AiOverviewMetricsDto>.Success(overview));
    }

    /// <summary>
    /// Đặt lại các bộ đếm thống kê giám sát AI (phục vụ kiểm thử benchmark).
    /// </summary>
    [HttpPost("reset")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public ActionResult<ApiResult> ResetMetrics()
    {
        _monitoringService.ResetMetrics();
        return Ok(ApiResult.Success("Đã làm mới toàn bộ bộ đếm AI Monitoring."));
    }
}
