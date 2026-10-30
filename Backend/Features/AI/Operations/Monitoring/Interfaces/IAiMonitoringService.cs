using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

public interface IAiMonitoringService
{
    void RecordToolExecution(
        string toolName,
        double durationMs,
        bool isSuccess,
        string? correlationId = null,
        string? error = null);

    void RecordLlmInvocation(
        string model,
        string provider,
        double latencyMs,
        long promptTokens,
        long completionTokens,
        decimal costUsd,
        bool isSuccess,
        bool fromCache = false,
        string? correlationId = null,
        string? error = null);

    void RecordRequest(
        string agentName,
        double latencyMs,
        bool isSuccess,
        string? correlationId = null,
        string? error = null);

    Task<AiMonitoringDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<AiOverviewMetricsDto> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);

    void ResetMetrics();
}
