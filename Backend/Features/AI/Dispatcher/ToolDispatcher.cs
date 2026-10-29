using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Features.AI.Tools;

namespace SmartSupermarket.Backend.Features.AI.Dispatcher;

public class ToolDispatcher : IToolDispatcher
{
    private readonly ILogger<ToolDispatcher> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IAiMonitoringService? _monitoringService;
    private readonly ConcurrentDictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object?>>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1500);

    public ToolDispatcher(
        ILogger<ToolDispatcher> logger,
        IServiceScopeFactory? scopeFactory = null,
        IAiMonitoringService? monitoringService = null)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _monitoringService = monitoringService;
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

        Func<Dictionary<string, object>, CancellationToken, Task<object?>>? handler = null;
        if (!_handlers.TryGetValue(toolMethod, out handler))
        {
            if (_scopeFactory != null)
            {
                handler = (p, ct) => ExecuteBuiltInToolAsync(toolMethod, p, ct);
            }
            else
            {
                stopwatch.Stop();
                _logger.LogWarning("[ToolDispatcher] Không tìm thấy handler đăng ký cho Tool: {ToolMethod}", toolMethod);
                return ToolExecutionResultDto.Failed(toolMethod, $"Handler cho Tool '{toolMethod}' chưa được đăng ký.", stopwatch.ElapsedMilliseconds);
            }
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
                _monitoringService?.RecordToolExecution(toolMethod, stopwatch.ElapsedMilliseconds, true);
                return ToolExecutionResultDto.Success(toolMethod, data, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogError("[ToolDispatcher] Tool {ToolMethod} bị timeout sau {TimeoutMs}ms", toolMethod, DefaultTimeout.TotalMilliseconds);
                _monitoringService?.RecordToolExecution(toolMethod, stopwatch.ElapsedMilliseconds, false, error: "Timeout");
                return ToolExecutionResultDto.Failed(toolMethod, $"Tool '{toolMethod}' bị timeout sau 1500ms.", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries || cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "[ToolDispatcher] Tool {ToolMethod} thất bại sau {Attempt} lần thử", toolMethod, attempt);
                    _monitoringService?.RecordToolExecution(toolMethod, stopwatch.ElapsedMilliseconds, false, error: ex.Message);
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

        if (rawResult.Data != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(rawResult.Data, AiJsonDefaults.Options);
                var deserialized = JsonSerializer.Deserialize<T>(json, AiJsonDefaults.Options);
                return ToolExecutionResultDto<T>.Success(toolMethod, deserialized, rawResult.ExecutionTimeMs);
            }
            catch
            {
            }
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

    private async Task<object?> ExecuteBuiltInToolAsync(
        string toolMethod,
        Dictionary<string, object> parameters,
        CancellationToken ct)
    {
        if (_scopeFactory == null)
        {
            throw new InvalidOperationException("ServiceScopeFactory không khả dụng để thực thi built-in tool.");
        }

        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        return toolMethod.ToLowerInvariant() switch
        {
            "inventorytool.getstock" =>
                await sp.GetRequiredService<IInventoryTool>().GetStockAsync(GetParam<int>(parameters, "productId"), ct),

            "inventorytool.getnearexpiry" or "inventorytool.getnearexpirybatches" =>
                await sp.GetRequiredService<IInventoryTool>().GetNearExpiryBatchesAsync(GetParam(parameters, "daysThreshold", 3), ct),

            "inventorytool.getbatches" or "inventorytool.getbatchesbyproduct" =>
                await sp.GetRequiredService<IInventoryTool>().GetBatchesByProductAsync(GetParam<int>(parameters, "productId"), ct),

            "inventorytool.getalternativeproduct" =>
                await sp.GetRequiredService<IInventoryTool>().GetAlternativeProductAsync(
                    GetParam<int>(parameters, "outOfStockProductId", GetParam<int>(parameters, "productId")), ct),

            "producttool.search" or "producttool.searchproducts" =>
                await sp.GetRequiredService<IProductTool>().SearchAsync(
                    GetParam(parameters, "keyword", string.Empty),
                    GetParam<int?>(parameters, "categoryId"),
                    GetParam(parameters, "limit", 15), ct),

            "producttool.getbyid" or "producttool.getproductbyid" =>
                await sp.GetRequiredService<IProductTool>().GetByIdAsync(
                    GetParam<int>(parameters, "productId", GetParam<int>(parameters, "id")), ct),

            "producttool.getalternative" or "producttool.getalternativeproduct" =>
                await sp.GetRequiredService<IProductTool>().GetAlternativeAsync(
                    GetParam<int>(parameters, "originalProductId", GetParam<int>(parameters, "productId")),
                    GetParam<decimal>(parameters, "maxPrice"), ct),

            "promotiontool.getvouchers" or "promotiontool.getactivevouchers" =>
                await sp.GetRequiredService<IPromotionTool>().GetActiveVouchersAsync(ct),

            "promotiontool.checkflashsale" or "promotiontool.getflashsaleitems" =>
                await sp.GetRequiredService<IPromotionTool>().CheckFlashSaleAsync(ct),

            "promotiontool.calculatediscount" or "promotiontool.applydiscount" =>
                await sp.GetRequiredService<IPromotionTool>().CalculateDiscountAsync(
                    GetParam(parameters, "items", new List<MealIngredientItemDto>()),
                    GetParam<string?>(parameters, "voucherCode"), ct),

            "recipetool.getingredients" or "recipetool.getrecipeingredients" =>
                await sp.GetRequiredService<IRecipeTool>().GetRecipeIngredientsAsync(
                    GetParam(parameters, "dishName", string.Empty),
                    GetParam(parameters, "servings", 2), ct),

            "recipetool.getcookingtips" =>
                await sp.GetRequiredService<IRecipeTool>().GetCookingTipsAsync(
                    GetParam(parameters, "dishName", string.Empty), ct),

            "budgettool.evaluatebudget" =>
                await sp.GetRequiredService<IBudgetTool>().EvaluateBudgetAsync(
                    GetParam<decimal>(parameters, "targetBudget"),
                    GetParam(parameters, "items", new List<MealIngredientItemDto>()), ct),

            "budgettool.proposecheaperalternative" =>
                await sp.GetRequiredService<IBudgetTool>().ProposeCheaperAlternativeAsync(
                    GetParam<int>(parameters, "currentProductId"),
                    GetParam<decimal>(parameters, "targetPrice"), ct),

            "carttool.addtocart" =>
                await sp.GetRequiredService<ICartTool>().AddToCartAsync(
                    GetParam(parameters, "sessionId", Guid.NewGuid().ToString()),
                    GetParam(parameters, "items", new List<MealIngredientItemDto>()), ct),

            "carttool.estimatecart" or "carttool.estimatecarttotal" =>
                await sp.GetRequiredService<ICartTool>().EstimateCartTotalAsync(
                    GetParam(parameters, "items", new List<MealIngredientItemDto>()),
                    GetParam<string?>(parameters, "voucherCode"), ct),

            _ => throw new NotSupportedException($"Tool method '{toolMethod}' không được hỗ trợ.")
        };
    }

    private static T GetParam<T>(Dictionary<string, object> parameters, string key, T defaultValue = default!)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var val) || val == null)
        {
            return defaultValue;
        }

        if (val is T typedVal)
        {
            return typedVal;
        }

        if (val is JsonElement jsonElement)
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), AiJsonDefaults.Options);
                if (deserialized != null)
                {
                    return deserialized;
                }
            }
            catch
            {
            }
        }

        try
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(val, targetType);
        }
        catch
        {
            return defaultValue;
        }
    }
}
