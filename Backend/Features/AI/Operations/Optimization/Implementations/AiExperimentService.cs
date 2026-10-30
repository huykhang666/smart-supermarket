using System;
using System.Collections.Concurrent;
using System.Linq;
using SmartSupermarket.Backend.Features.AI.Optimization.Interfaces;
using SmartSupermarket.Backend.Features.AI.Optimization.Models;

namespace SmartSupermarket.Backend.Features.AI.Optimization.Implementations;

public class AiExperimentService : IAiExperimentService
{
    private readonly ConcurrentBag<RequestTelemetryDto> _telemetryLogs = new();

    public AiExperimentVariantDto AssignVariant(string? userOrSessionId = null)
    {
        // Deterministic hash assignment or default to Treatment_A
        int hash = Math.Abs((userOrSessionId ?? Guid.NewGuid().ToString()).GetHashCode());
        bool isControl = (hash % 100) < 20; // 20% control, 80% optimized treatment

        if (isControl)
        {
            return new AiExperimentVariantDto
            {
                ExperimentName = "Phase4_Optimization_Rollout",
                VariantKey = "Control_v3",
                PromptVersion = "v3.0.0-rag",
                RoutingStrategy = "StandardLegacyRouting",
                IsControlGroup = true,
                TrafficAllocationPercent = 20.0
            };
        }

        return new AiExperimentVariantDto
        {
            ExperimentName = "Phase4_Optimization_Rollout",
            VariantKey = "Variant_A_Optimized",
            PromptVersion = "v4.0.0-optimized",
            RoutingStrategy = "TieredSmartRouting",
            IsControlGroup = false,
            TrafficAllocationPercent = 80.0
        };
    }

    public void RecordExperimentOutcome(RequestTelemetryDto telemetry)
    {
        if (telemetry == null) return;
        _telemetryLogs.Add(telemetry);
    }

    public (int ControlCount, int TreatmentCount, double AvgTreatmentLatencyMs, double AvgControlLatencyMs) GetExperimentStats()
    {
        var logs = _telemetryLogs.ToList();
        var control = logs.Where(l => l.ExperimentVariant == "Control_v3").ToList();
        var treatment = logs.Where(l => l.ExperimentVariant != "Control_v3").ToList();

        double avgControl = control.Count > 0 ? control.Average(l => l.LatencyMs) : 0.0;
        double avgTreatment = treatment.Count > 0 ? treatment.Average(l => l.LatencyMs) : 0.0;

        return (control.Count, treatment.Count, Math.Round(avgTreatment, 1), Math.Round(avgControl, 1));
    }
}
