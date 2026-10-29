using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Monitoring.Implementations;

public class AiMonitoringService : IAiMonitoringService
{
    private const int MaxTraceHistory = 200;
    private const decimal UsdToVndRate = 25400m;

    private readonly IAiMetricsCollector _metricsCollector;
    private readonly ILogger<AiMonitoringService> _logger;

    private readonly ConcurrentQueue<AiTraceItemDto> _recentTraces = new();
    private readonly ConcurrentDictionary<string, ToolAccumulator> _toolStats = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ModelAccumulator> _modelStats = new(StringComparer.OrdinalIgnoreCase);

    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _totalPromptTokens;
    private long _totalCompletionTokens;
    private long _cacheHits;
    private decimal _totalCostUsd;
    private decimal _totalSavedCostUsd;
    private readonly ConcurrentBag<double> _recentLatencies = new();

    public AiMonitoringService(
        IAiMetricsCollector metricsCollector,
        ILogger<AiMonitoringService> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public void RecordToolExecution(
        string toolName,
        double durationMs,
        bool isSuccess,
        string? correlationId = null,
        string? error = null)
    {
        _metricsCollector.RecordToolDuration(toolName, durationMs, isSuccess);

        var acc = _toolStats.GetOrAdd(toolName, name => new ToolAccumulator(name));
        acc.Record(durationMs, isSuccess);

        AddTrace(new AiTraceItemDto
        {
            TraceId = Guid.NewGuid().ToString("N")[..8],
            CorrelationId = correlationId ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Operation = "ToolExecution",
            ModelOrTool = toolName,
            DurationMs = Math.Round(durationMs, 2),
            IsSuccess = isSuccess,
            ErrorMessage = error
        });
    }

    public void RecordLlmInvocation(
        string model,
        string provider,
        double latencyMs,
        long promptTokens,
        long completionTokens,
        decimal costUsd,
        bool isSuccess,
        bool fromCache = false,
        string? correlationId = null,
        string? error = null)
    {
        _metricsCollector.RecordLlmLatency(model, latencyMs, isSuccess);
        _metricsCollector.RecordLlmTokens(model, promptTokens, completionTokens);
        _metricsCollector.RecordLlmCost(model, costUsd);

        Interlocked.Add(ref _totalPromptTokens, promptTokens);
        Interlocked.Add(ref _totalCompletionTokens, completionTokens);

        if (fromCache)
        {
            Interlocked.Increment(ref _cacheHits);
            _totalSavedCostUsd += costUsd;
        }
        else
        {
            _totalCostUsd += costUsd;
        }

        var key = $"{provider}:{model}";
        var acc = _modelStats.GetOrAdd(key, _ => new ModelAccumulator(model, provider));
        acc.Record(promptTokens, completionTokens, costUsd, latencyMs);

        AddTrace(new AiTraceItemDto
        {
            TraceId = Guid.NewGuid().ToString("N")[..8],
            CorrelationId = correlationId ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Operation = fromCache ? "LlmCall (Cache Hit)" : "LlmCall",
            ModelOrTool = model,
            DurationMs = Math.Round(latencyMs, 2),
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            CostUsd = Math.Round(costUsd, 6),
            IsSuccess = isSuccess,
            ErrorMessage = error,
            FromCache = fromCache
        });
    }

    public void RecordRequest(
        string agentName,
        double latencyMs,
        bool isSuccess,
        string? correlationId = null,
        string? error = null)
    {
        _metricsCollector.RecordRequest(agentName, latencyMs, isSuccess);

        Interlocked.Increment(ref _totalRequests);
        if (isSuccess)
        {
            Interlocked.Increment(ref _successfulRequests);
        }
        else
        {
            Interlocked.Increment(ref _failedRequests);
        }

        _recentLatencies.Add(latencyMs);

        AddTrace(new AiTraceItemDto
        {
            TraceId = Guid.NewGuid().ToString("N")[..8],
            CorrelationId = correlationId ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Operation = "AgentRequest",
            ModelOrTool = agentName,
            DurationMs = Math.Round(latencyMs, 2),
            IsSuccess = isSuccess,
            ErrorMessage = error
        });
    }

    public Task<AiMonitoringDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var overview = CalculateOverview();

        var toolMetrics = _toolStats.Values
            .Select(t => t.ToDto())
            .OrderByDescending(t => t.InvocationCount)
            .ToList();

        var modelMetrics = _modelStats.Values
            .Select(m => m.ToDto())
            .OrderByDescending(m => m.RequestCount)
            .ToList();

        var recentTraces = _recentTraces
            .ToArray()
            .OrderByDescending(t => t.Timestamp)
            .Take(50)
            .ToList();

        var dashboard = new AiMonitoringDashboardDto
        {
            Overview = overview,
            ToolMetrics = toolMetrics,
            ModelMetrics = modelMetrics,
            RecentTraces = recentTraces
        };

        return Task.FromResult(dashboard);
    }

    public Task<AiOverviewMetricsDto> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateOverview());
    }

    public void ResetMetrics()
    {
        _toolStats.Clear();
        _modelStats.Clear();
        while (_recentTraces.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _successfulRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _totalPromptTokens, 0);
        Interlocked.Exchange(ref _totalCompletionTokens, 0);
        Interlocked.Exchange(ref _cacheHits, 0);
        _totalCostUsd = 0;
        _totalSavedCostUsd = 0;
        _recentLatencies.Clear();
    }

    private AiOverviewMetricsDto CalculateOverview()
    {
        var totalReq = Interlocked.Read(ref _totalRequests);
        var successReq = Interlocked.Read(ref _successfulRequests);
        var failReq = Interlocked.Read(ref _failedRequests);
        var promptTok = Interlocked.Read(ref _totalPromptTokens);
        var compTok = Interlocked.Read(ref _totalCompletionTokens);
        var hits = Interlocked.Read(ref _cacheHits);

        var latencies = _recentLatencies.ToArray();
        double avgLatency = latencies.Length > 0 ? latencies.Average() : 0;
        double p95 = 0;
        if (latencies.Length > 0)
        {
            Array.Sort(latencies);
            int index = (int)Math.Ceiling(latencies.Length * 0.95) - 1;
            p95 = latencies[Math.Clamp(index, 0, latencies.Length - 1)];
        }

        var totalCost = _totalCostUsd;
        var totalSaved = _totalSavedCostUsd;
        var totalTokens = promptTok + compTok;
        var cacheRatio = totalReq > 0 ? (double)hits / totalReq * 100.0 : 0.0;
        var successRate = totalReq > 0 ? (double)successReq / totalReq * 100.0 : 100.0;

        return new AiOverviewMetricsDto
        {
            TotalRequests = totalReq,
            SuccessfulRequests = successReq,
            FailedRequests = failReq,
            SuccessRatePercent = Math.Round(successRate, 1),
            AverageLatencyMs = Math.Round(avgLatency, 1),
            P95LatencyMs = Math.Round(p95, 1),
            TotalPromptTokens = promptTok,
            TotalCompletionTokens = compTok,
            TotalTokens = totalTokens,
            TotalCostUsd = Math.Round(totalCost, 4),
            TotalCostVnd = Math.Round(totalCost * UsdToVndRate, 0),
            TotalSavedCostUsd = Math.Round(totalSaved, 4),
            CacheHitCount = hits,
            CacheHitRatioPercent = Math.Round(cacheRatio, 1),
            ServerTimeUtc = DateTime.UtcNow
        };
    }

    private void AddTrace(AiTraceItemDto item)
    {
        _recentTraces.Enqueue(item);
        while (_recentTraces.Count > MaxTraceHistory && _recentTraces.TryDequeue(out _)) { }
    }

    private sealed class ToolAccumulator
    {
        private readonly string _name;
        private long _count;
        private long _success;
        private long _failure;
        private double _totalDuration;
        private double _min = double.MaxValue;
        private double _max;
        private readonly object _lock = new();

        public ToolAccumulator(string name)
        {
            _name = name;
        }

        public void Record(double duration, bool isSuccess)
        {
            lock (_lock)
            {
                _count++;
                if (isSuccess) _success++; else _failure++;
                _totalDuration += duration;
                if (duration < _min) _min = duration;
                if (duration > _max) _max = duration;
            }
        }

        public AiToolMetricDto ToDto()
        {
            lock (_lock)
            {
                return new AiToolMetricDto
                {
                    ToolName = _name,
                    InvocationCount = _count,
                    SuccessCount = _success,
                    FailureCount = _failure,
                    AverageDurationMs = _count > 0 ? Math.Round(_totalDuration / _count, 2) : 0,
                    MinDurationMs = _min == double.MaxValue ? 0 : Math.Round(_min, 2),
                    MaxDurationMs = Math.Round(_max, 2),
                    TotalDurationMs = Math.Round(_totalDuration, 2)
                };
            }
        }
    }

    private sealed class ModelAccumulator
    {
        private readonly string _model;
        private readonly string _provider;
        private long _requests;
        private long _promptTokens;
        private long _completionTokens;
        private decimal _costUsd;
        private double _totalLatency;
        private readonly object _lock = new();

        public ModelAccumulator(string model, string provider)
        {
            _model = model;
            _provider = provider;
        }

        public void Record(long promptTokens, long completionTokens, decimal cost, double latency)
        {
            lock (_lock)
            {
                _requests++;
                _promptTokens += promptTokens;
                _completionTokens += completionTokens;
                _costUsd += cost;
                _totalLatency += latency;
            }
        }

        public AiModelUsageMetricDto ToDto()
        {
            lock (_lock)
            {
                return new AiModelUsageMetricDto
                {
                    ModelName = _model,
                    Provider = _provider,
                    RequestCount = _requests,
                    PromptTokens = _promptTokens,
                    CompletionTokens = _completionTokens,
                    TotalTokens = _promptTokens + _completionTokens,
                    EstimatedCostUsd = Math.Round(_costUsd, 6),
                    AverageLatencyMs = _requests > 0 ? Math.Round(_totalLatency / _requests, 2) : 0
                };
            }
        }
    }
}
