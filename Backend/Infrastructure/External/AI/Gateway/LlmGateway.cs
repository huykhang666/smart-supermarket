using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class LlmGateway : ILLMGateway
{
    private readonly GeminiGateway _geminiGateway;
    private readonly GroqGateway _groqGateway;
    private readonly LocalFallbackGateway _localFallbackGateway;
    private readonly ILogger<LlmGateway> _logger;

    public string ActiveProvider { get; private set; } = "Unknown";

    public LlmGateway(
        GeminiGateway geminiGateway,
        GroqGateway groqGateway,
        LocalFallbackGateway localFallbackGateway,
        ILogger<LlmGateway> logger)
    {
        _geminiGateway = geminiGateway;
        _groqGateway = groqGateway;
        _localFallbackGateway = localFallbackGateway;
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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(json, options);
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
        if (_geminiGateway.IsConfigured)
        {
            try
            {
                var geminiResult = await _geminiGateway.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
                if (!string.IsNullOrWhiteSpace(geminiResult))
                {
                    ActiveProvider = "Gemini-2.5-Flash";
                    return geminiResult;
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
                    ActiveProvider = "Groq-Llama-3.3-70B";
                    return groqResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LlmGateway] GroqGateway failed. Falling back to LocalFallbackGateway.");
            }
        }

        _logger.LogInformation("[LlmGateway] Routing to LocalFallbackGateway.");
        ActiveProvider = "Local-Rule-Engine";
        return await _localFallbackGateway.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
    }
}
