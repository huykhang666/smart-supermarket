using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class AiMonitoringDashboardDto
{
    public AiOverviewMetricsDto Overview { get; set; } = new();
    public List<AiToolMetricDto> ToolMetrics { get; set; } = new();
    public List<AiModelUsageMetricDto> ModelMetrics { get; set; } = new();
    public List<AiTraceItemDto> RecentTraces { get; set; } = new();
}

public class AiOverviewMetricsDto
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRatePercent { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public long TotalPromptTokens { get; set; }
    public long TotalCompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
    public decimal TotalCostVnd { get; set; }
    public decimal TotalSavedCostUsd { get; set; }
    public long CacheHitCount { get; set; }
    public double CacheHitRatioPercent { get; set; }
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}

public class AiToolMetricDto
{
    public string ToolName { get; set; } = string.Empty;
    public long InvocationCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double AverageDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double TotalDurationMs { get; set; }
}

public class AiModelUsageMetricDto
{
    public string ModelName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public double AverageLatencyMs { get; set; }
}

public class AiTraceItemDto
{
    public string TraceId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string ModelOrTool { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public decimal CostUsd { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool FromCache { get; set; }
}
