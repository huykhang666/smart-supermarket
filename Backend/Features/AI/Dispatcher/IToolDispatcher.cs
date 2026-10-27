using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Dispatcher;

public interface IToolDispatcher
{
    void RegisterHandler(string toolMethod, Func<Dictionary<string, object>, CancellationToken, Task<object?>> handler);

    Task<ToolExecutionResultDto> DispatchAsync(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    Task<ToolExecutionResultDto<T>> DispatchAsync<T>(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    Task<List<ToolExecutionResultDto>> DispatchBatchParallelAsync(
        List<(string ToolMethod, Dictionary<string, object> Parameters)> requests,
        CancellationToken cancellationToken = default);
}
