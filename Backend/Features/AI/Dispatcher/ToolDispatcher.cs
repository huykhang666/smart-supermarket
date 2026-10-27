using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Dispatcher;

public class ToolDispatcher : IToolDispatcher
{
    private readonly ILogger<ToolDispatcher> _logger;
    private readonly ConcurrentDictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object?>>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1500);

    public ToolDispatcher(ILogger<ToolDispatcher> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler(string toolMethod, Func<Dictionary<string, object>, CancellationToken, Task<object?>> handler)
    {
        _handlers[toolMethod] = handler;
    }

    public async Task<ToolExecutionResultDto> DispatchAsync(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[ToolDispatcher] Bắt đầu gọi Tool: {ToolMethod}", toolMethod);

        if (!_handlers.TryGetValue(toolMethod, out var handler))
        {
            stopwatch.Stop();
            _logger.LogWarning("[ToolDispatcher] Không tìm thấy handler đăng ký cho Tool: {ToolMethod}", toolMethod);
            return ToolExecutionResultDto.Failed(toolMethod, $"Handler cho Tool '{toolMethod}' chưa được đăng ký.", stopwatch.ElapsedMilliseconds);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        int maxRetries = 2;
        int delayMs = 150;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var data = await handler(parameters, timeoutCts.Token);
                stopwatch.Stop();
                _logger.LogInformation("[ToolDispatcher] Tool {ToolMethod} thành công trong {ElapsedMs}ms (Lần thử {Attempt})",
                    toolMethod, stopwatch.ElapsedMilliseconds, attempt);
                return ToolExecutionResultDto.Success(toolMethod, data, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogError("[ToolDispatcher] Tool {ToolMethod} bị timeout sau {TimeoutMs}ms", toolMethod, DefaultTimeout.TotalMilliseconds);
                return ToolExecutionResultDto.Failed(toolMethod, $"Tool '{toolMethod}' bị timeout sau 1500ms.", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries || cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "[ToolDispatcher] Tool {ToolMethod} thất bại sau {Attempt} lần thử", toolMethod, attempt);
                    return ToolExecutionResultDto.Failed(toolMethod, ex.Message, stopwatch.ElapsedMilliseconds);
                }

                _logger.LogWarning(ex, "[ToolDispatcher] Lần thử {Attempt} cho {ToolMethod} thất bại, thử lại sau {DelayMs}ms...", attempt, toolMethod, delayMs);
                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2;
            }
        }

        stopwatch.Stop();
        return ToolExecutionResultDto.Failed(toolMethod, "Không thể hoàn thành thực thi Tool.", stopwatch.ElapsedMilliseconds);
    }

    public async Task<ToolExecutionResultDto<T>> DispatchAsync<T>(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawResult = await DispatchAsync(toolMethod, parameters, cancellationToken);
        if (!rawResult.IsSuccess)
        {
            return ToolExecutionResultDto<T>.Failed(toolMethod, rawResult.ErrorMessage ?? "Lỗi không xác định.", rawResult.ExecutionTimeMs);
        }

        if (rawResult.Data is T typedData)
        {
            return ToolExecutionResultDto<T>.Success(toolMethod, typedData, rawResult.ExecutionTimeMs);
        }

        return ToolExecutionResultDto<T>.Success(toolMethod, default, rawResult.ExecutionTimeMs);
    }

    public async Task<List<ToolExecutionResultDto>> DispatchBatchParallelAsync(
        List<(string ToolMethod, Dictionary<string, object> Parameters)> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests == null || requests.Count == 0)
        {
            return new List<ToolExecutionResultDto>();
        }

        var tasks = requests.Select(r => DispatchAsync(r.ToolMethod, r.Parameters, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
