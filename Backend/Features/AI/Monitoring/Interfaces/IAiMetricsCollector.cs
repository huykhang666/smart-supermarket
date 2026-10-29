using System;

namespace SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

public interface IAiMetricsCollector
{
    void RecordToolDuration(string toolName, double durationMs, bool isSuccess);
    void RecordLlmTokens(string model, long promptTokens, long completionTokens);
    void RecordLlmCost(string model, decimal costUsd);
    void RecordLlmLatency(string model, double latencyMs, bool isSuccess);
    void RecordRequest(string endpoint, double latencyMs, bool isSuccess);
}
