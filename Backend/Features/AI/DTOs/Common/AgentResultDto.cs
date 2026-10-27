namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class AgentResultDto
{
    public bool IsSuccess { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string TraceId { get; set; } = string.Empty;

    public static AgentResultDto Success(string agentName, string output, object? data = null, long elapsedMs = 0, string traceId = "") =>
        new()
        {
            IsSuccess = true,
            AgentName = agentName,
            Output = output,
            Data = data,
            ExecutionTimeMs = elapsedMs,
            TraceId = traceId
        };

    public static AgentResultDto Failed(string agentName, string error, long elapsedMs = 0, string traceId = "") =>
        new()
        {
            IsSuccess = false,
            AgentName = agentName,
            ErrorMessage = error,
            ExecutionTimeMs = elapsedMs,
            TraceId = traceId
        };
}
