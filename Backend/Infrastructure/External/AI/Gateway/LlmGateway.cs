using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.Common;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class LlmGateway : ILLMGateway
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = AiJsonDefaults.Options;

    private readonly GeminiGateway _geminiGateway;
    private readonly GroqGateway _groqGateway;
    private readonly LocalFallbackGateway _localFallbackGateway;
    private readonly IResponseCache _responseCache;
    private readonly IAiMonitoringService? _monitoringService;
    private readonly ILogger<LlmGateway> _logger;
    private volatile string _activeProvider = "Unknown";

    public string ActiveProvider => _activeProvider;

    public LlmGateway(
        GeminiGateway geminiGateway,
        GroqGateway groqGateway,
        LocalFallbackGateway localFallbackGateway,
        IResponseCache responseCache,
        ILogger<LlmGateway> logger,
        IAiMonitoringService? monitoringService = null)
    {
        _geminiGateway = geminiGateway;
        _groqGateway = groqGateway;
        _localFallbackGateway = localFallbackGateway;
        _responseCache = responseCache;
        _logger = logger;
        _monitoringService = monitoringService;
    }

    public async Task<string> GenerateTextAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(prompt, systemInstruction, false, cancellationToken);
    }

    public async Task<string> GenerateJsonAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(prompt, systemInstruction, true, cancellationToken);
    }

    public async Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        var json = await GenerateJsonAsync(prompt, systemInstruction, cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LlmGateway] Failed to deserialize JSON into type {TypeName}. Raw JSON: {Json}", typeof(T).Name, json);
            return default;
        }
    }

    private async Task<string> ExecuteWithFallbackAsync(
        string prompt,
        string? systemInstruction,
        bool requireJson,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var promptChars = prompt.Length + (systemInstruction?.Length ?? 0);
        var promptTokens = Math.Max(1, (long)Math.Ceiling(promptChars / 4.0));

        var cached = await _responseCache.GetResponseAsync(prompt, systemInstruction, requireJson, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            sw.Stop();
            _activeProvider = "Cache-Hit";
            var compTokens = Math.Max(1, (long)Math.Ceiling(cached.Length / 4.0));
            var savedCost = CalculateCost("gemini-2.5-flash", promptTokens, compTokens);
            _monitoringService?.RecordLlmInvocation(
                model: "cache-hit",
                provider: "Redis/Memory",
                latencyMs: sw.Elapsed.TotalMilliseconds,
                promptTokens: promptTokens,
                completionTokens: compTokens,
                costUsd: savedCost,
                isSuccess: true,
                fromCache: true);
            return cached;
        }

        string result;
        if (_geminiGateway.IsConfigured)
        {
            try
            {
                var geminiResult = await _geminiGateway.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
                if (!string.IsNullOrWhiteSpace(geminiResult))
                {
                    sw.Stop();
                    _activeProvider = "Gemini-2.5-Flash";
                    result = geminiResult;
                    var compTokens = Math.Max(1, (long)Math.Ceiling(result.Length / 4.0));
                    var cost = CalculateCost("gemini-2.5-flash", promptTokens, compTokens);

                    _monitoringService?.RecordLlmInvocation(
                        model: "gemini-2.5-flash",
                        provider: "Google",
                        latencyMs: sw.Elapsed.TotalMilliseconds,
                        promptTokens: promptTokens,
                        completionTokens: compTokens,
                        costUsd: cost,
                        isSuccess: true);

                    await _responseCache.SetResponseAsync(prompt, result, systemInstruction, requireJson, cancellationToken: cancellationToken);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LlmGateway] GeminiGateway failed. Falling back to GroqGateway.");
            }
        }

        if (_groqGateway.IsConfigured)
        {
            try
            {
                var groqResult = await _groqGateway.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
                if (!string.IsNullOrWhiteSpace(groqResult))
                {
                    sw.Stop();
                    _activeProvider = "Groq-Llama-3.3-70B";
                    result = groqResult;
                    var compTokens = Math.Max(1, (long)Math.Ceiling(result.Length / 4.0));
                    var cost = CalculateCost("llama-3.3-70b", promptTokens, compTokens);

                    _monitoringService?.RecordLlmInvocation(
                        model: "llama-3.3-70b-versatile",
                        provider: "Groq",
                        latencyMs: sw.Elapsed.TotalMilliseconds,
                        promptTokens: promptTokens,
                        completionTokens: compTokens,
                        costUsd: cost,
                        isSuccess: true);

                    await _responseCache.SetResponseAsync(prompt, result, systemInstruction, requireJson, cancellationToken: cancellationToken);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LlmGateway] GroqGateway failed. Falling back to LocalFallbackGateway.");
            }
        }

        _logger.LogInformation("[LlmGateway] Routing to LocalFallbackGateway.");
        _activeProvider = "Local-Rule-Engine";
        result = await _localFallbackGateway.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
        sw.Stop();

        if (!string.IsNullOrWhiteSpace(result))
        {
            var compTokens = Math.Max(1, (long)Math.Ceiling(result.Length / 4.0));
            _monitoringService?.RecordLlmInvocation(
                model: "local-rule-engine",
                provider: "Internal",
                latencyMs: sw.Elapsed.TotalMilliseconds,
                promptTokens: promptTokens,
                completionTokens: compTokens,
                costUsd: 0m,
                isSuccess: true);

            await _responseCache.SetResponseAsync(prompt, result, systemInstruction, requireJson, cancellationToken: cancellationToken);
        }

        return result;
    }

    private static decimal CalculateCost(string model, long promptTokens, long completionTokens)
    {
        return model.ToLowerInvariant() switch
        {
            var m when m.Contains("gemini") => (promptTokens * 0.0000001m) + (completionTokens * 0.0000004m),
            var m when m.Contains("llama") || m.Contains("groq") => (promptTokens * 0.00000059m) + (completionTokens * 0.00000079m),
            _ => 0m
        };
    }
}
