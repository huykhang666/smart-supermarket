using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class LlmGateway : ILLMGateway
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GeminiGateway _geminiGateway;
    private readonly GroqGateway _groqGateway;
    private readonly LocalFallbackGateway _localFallbackGateway;
    private readonly IResponseCache _responseCache;
    private readonly ILogger<LlmGateway> _logger;
    private volatile string _activeProvider = "Unknown";

    public string ActiveProvider => _activeProvider;

    public LlmGateway(
        GeminiGateway geminiGateway,
        GroqGateway groqGateway,
        LocalFallbackGateway localFallbackGateway,
        IResponseCache responseCache,
        ILogger<LlmGateway> logger)
    {
        _geminiGateway = geminiGateway;
        _groqGateway = groqGateway;
        _localFallbackGateway = localFallbackGateway;
        _responseCache = responseCache;
        _logger = logger;
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
        var cached = await _responseCache.GetResponseAsync(prompt, systemInstruction, requireJson, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            _activeProvider = "Cache-Hit";
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
                    _activeProvider = "Gemini-2.5-Flash";
                    result = geminiResult;
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
                    _activeProvider = "Groq-Llama-3.3-70B";
                    result = groqResult;
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
        if (!string.IsNullOrWhiteSpace(result))
        {
            await _responseCache.SetResponseAsync(prompt, result, systemInstruction, requireJson, cancellationToken: cancellationToken);
        }
        return result;
    }
}
