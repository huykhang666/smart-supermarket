using System;

namespace SmartSupermarket.Backend.Features.AI.Optimization.Models;

public class AiExperimentVariantDto
{
    public string ExperimentName { get; set; } = "PromptAndModelOptimization";
    public string VariantKey { get; set; } = "Variant_A_Optimized";
    public string PromptVersion { get; set; } = "v4.0.0-optimized";
    public string RoutingStrategy { get; set; } = "TieredSmartRouting";
    public bool IsControlGroup { get; set; } = false;
    public double TrafficAllocationPercent { get; set; } = 50.0;
}

public class RequestTelemetryDto
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string PromptVersion { get; set; } = "v4.0.0-optimized";
    public string ModelUsed { get; set; } = "gemini-2.5-flash";
    public string RoutingTier { get; set; } = "StrongReasoning";
    public long LatencyMs { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal CostUsd { get; set; }
    public bool CacheHit { get; set; }
    public string? ExperimentVariant { get; set; }
}
