using SmartSupermarket.Backend.Features.AI.Optimization.Models;

namespace SmartSupermarket.Backend.Features.AI.Optimization.Interfaces;

public interface IAiExperimentService
{
    AiExperimentVariantDto AssignVariant(string? userOrSessionId = null);
    void RecordExperimentOutcome(RequestTelemetryDto telemetry);
    (int ControlCount, int TreatmentCount, double AvgTreatmentLatencyMs, double AvgControlLatencyMs) GetExperimentStats();
}
