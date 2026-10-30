namespace SmartSupermarket.Backend.Features.AI.Routing.Models;

public enum RoutingModelTier
{
    DeterministicOffline = 0,
    FastLightweight = 1,
    StrongReasoning = 2
}

public class ModelRoutingDecisionDto
{
    public string SelectedModel { get; set; } = string.Empty;
    public RoutingModelTier Tier { get; set; } = RoutingModelTier.FastLightweight;
    public string DecisionReason { get; set; } = string.Empty;
    public int EstimatedLatencyMs { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public bool RequiresExternalCall => Tier != RoutingModelTier.DeterministicOffline;
}
