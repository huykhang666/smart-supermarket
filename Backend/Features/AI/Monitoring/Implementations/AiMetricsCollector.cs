using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Monitoring.Implementations;

public class AiMetricsCollector : IAiMetricsCollector, IDisposable
{
    public const string MeterName = "SmartSupermarket.AI";
    private readonly Meter _meter;

    private readonly Histogram<double> _toolDurationHistogram;
    private readonly Counter<long> _llmPromptTokenCounter;
    private readonly Counter<long> _llmCompletionTokenCounter;
    private readonly Counter<double> _llmCostCounter;
    private readonly Histogram<double> _llmLatencyHistogram;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestLatencyHistogram;

    public AiMetricsCollector()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _toolDurationHistogram = _meter.CreateHistogram<double>(
            "ai.tool.duration",
            unit: "ms",
            description: "Thoi luong thuc thi cua cac AI Tools");

        _llmPromptTokenCounter = _meter.CreateCounter<long>(
            "ai.tokens.prompt",
            unit: "tokens",
            description: "Tong so Prompt Tokens tieu thu");

        _llmCompletionTokenCounter = _meter.CreateCounter<long>(
            "ai.tokens.completion",
            unit: "tokens",
            description: "Tong so Completion Tokens sinh ra");

        _llmCostCounter = _meter.CreateCounter<double>(
            "ai.cost.usd",
            unit: "USD",
            description: "Chi phi uoc tinh tinh bang USD");

        _llmLatencyHistogram = _meter.CreateHistogram<double>(
            "ai.llm.latency",
            unit: "ms",
            description: "Do tre goi LLM Gateway");

        _requestCounter = _meter.CreateCounter<long>(
            "ai.requests.total",
            unit: "requests",
            description: "Tong so luot yeu cau AI Agent");

        _requestLatencyHistogram = _meter.CreateHistogram<double>(
            "ai.request.latency",
            unit: "ms",
            description: "Do tre toan trinh xu ly cua AI");
    }

    public void RecordToolDuration(string toolName, double durationMs, bool isSuccess)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("tool.name", toolName),
            new("status", isSuccess ? "success" : "failed")
        };
        _toolDurationHistogram.Record(durationMs, tags);
    }

    public void RecordLlmTokens(string model, long promptTokens, long completionTokens)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("model", model)
        };
        _llmPromptTokenCounter.Add(promptTokens, tags);
        _llmCompletionTokenCounter.Add(completionTokens, tags);
    }

    public void RecordLlmCost(string model, decimal costUsd)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("model", model)
        };
        _llmCostCounter.Add((double)costUsd, tags);
    }

    public void RecordLlmLatency(string model, double latencyMs, bool isSuccess)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("model", model),
            new("status", isSuccess ? "success" : "failed")
        };
        _llmLatencyHistogram.Record(latencyMs, tags);
    }

    public void RecordRequest(string endpoint, double latencyMs, bool isSuccess)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint),
            new("status", isSuccess ? "success" : "failed")
        };
        _requestCounter.Add(1, tags);
        _requestLatencyHistogram.Record(latencyMs, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
