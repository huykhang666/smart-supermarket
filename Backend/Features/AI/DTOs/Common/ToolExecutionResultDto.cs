namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class ToolExecutionResultDto
{
    public bool IsSuccess { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }

    public static ToolExecutionResultDto Success(string toolName, object? data, long elapsedMs = 0) =>
        new()
        {
            IsSuccess = true,
            ToolName = toolName,
            Data = data,
            ExecutionTimeMs = elapsedMs
        };

    public static ToolExecutionResultDto Failed(string toolName, string error, long elapsedMs = 0) =>
        new()
        {
            IsSuccess = false,
            ToolName = toolName,
            ErrorMessage = error,
            ExecutionTimeMs = elapsedMs
        };
}

public class ToolExecutionResultDto<T>
{
    public bool IsSuccess { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }

    public static ToolExecutionResultDto<T> Success(string toolName, T? data, long elapsedMs = 0) =>
        new()
        {
            IsSuccess = true,
            ToolName = toolName,
            Data = data,
            ExecutionTimeMs = elapsedMs
        };

    public static ToolExecutionResultDto<T> Failed(string toolName, string error, long elapsedMs = 0) =>
        new()
        {
            IsSuccess = false,
            ToolName = toolName,
            ErrorMessage = error,
            ExecutionTimeMs = elapsedMs
        };
}
