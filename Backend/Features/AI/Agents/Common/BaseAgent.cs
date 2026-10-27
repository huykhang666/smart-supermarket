using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

namespace SmartSupermarket.Backend.Features.AI.Agents.Common;

public abstract class BaseAgent : IAgent
{
    private static readonly ActivitySource ActivitySource = new("SmartSupermarket.AI.Agents");

    protected readonly ILLMGateway _llmGateway;
    protected readonly IToolDispatcher _toolDispatcher;
    protected readonly IPromptBuilder _promptBuilder;
    protected readonly ILogger _logger;

    public abstract string AgentName { get; }
    public abstract string Role { get; }

    protected BaseAgent(
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        IPromptBuilder promptBuilder,
        ILogger logger)
    {
        _llmGateway = llmGateway;
        _toolDispatcher = toolDispatcher;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<AgentResultDto> ExecuteAsync(
        AgentContextDto context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = ActivitySource.StartActivity($"{AgentName}.Execute");
        var traceId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.role", Role);
        activity?.SetTag("session.id", context.SessionId);
        activity?.SetTag("user.intent", context.Intent);

        _logger.LogInformation("[{Agent}] Bắt đầu thực thi nhiệm vụ '{Role}' (Session: {SessionId}, TraceId: {TraceId})",
            AgentName, Role, context.SessionId, traceId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await ProcessInternalAsync(context, cancellationToken);
            stopwatch.Stop();

            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.TraceId = traceId;

            _logger.LogInformation("[{Agent}] Hoàn thành thành công trong {ElapsedMs}ms",
                AgentName, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Agent}] Bị hủy bỏ theo yêu cầu qua CancellationToken sau {ElapsedMs}ms",
                AgentName, stopwatch.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Error, "Task was cancelled");
            return AgentResultDto.Failed(AgentName, "Tác vụ của tác nhân bị hủy.", stopwatch.ElapsedMilliseconds, traceId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{Agent}] Lỗi khi thực thi sau {ElapsedMs}ms",
                AgentName, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return AgentResultDto.Failed(AgentName, ex.Message, stopwatch.ElapsedMilliseconds, traceId);
        }
    }

    protected abstract Task<AgentResultDto> ProcessInternalAsync(
        AgentContextDto context,
        CancellationToken cancellationToken);

    protected async Task<ToolExecutionResultDto> CallToolAsync(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("[{Agent}] Yêu cầu ToolDispatcher gọi tool: {ToolMethod}", AgentName, toolMethod);
        return await _toolDispatcher.DispatchAsync(toolMethod, parameters, cancellationToken);
    }

    protected async Task<ToolExecutionResultDto<T>> CallToolAsync<T>(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("[{Agent}] Yêu cầu ToolDispatcher gọi tool: {ToolMethod} kiểu {Type}", AgentName, toolMethod, typeof(T).Name);
        return await _toolDispatcher.DispatchAsync<T>(toolMethod, parameters, cancellationToken);
    }

    protected async Task<string> GenerateLlmAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _llmGateway.GenerateTextAsync(prompt, systemInstruction, cancellationToken);
    }

    protected async Task<T?> GenerateLlmStructuredAsync<T>(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _llmGateway.GenerateStructuredAsync<T>(prompt, systemInstruction, cancellationToken);
    }

    protected T? GetMemory<T>(AgentContextDto context, string key)
    {
        if (context.SharedData != null && context.SharedData.TryGetValue(key, out var val) && val is T typedVal)
        {
            return typedVal;
        }

        return default;
    }

    protected void SetMemory(AgentContextDto context, string key, object value)
    {
        context.SharedData ??= new Dictionary<string, object>();
        context.SharedData[key] = value;
    }
}
