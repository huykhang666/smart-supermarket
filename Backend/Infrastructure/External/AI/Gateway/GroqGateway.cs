using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class GroqGateway
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqGateway> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqGateway(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GroqGateway> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _configuration = configuration;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? configuration["Groq:ApiKey"] ?? string.Empty;
        var modelConfig = Environment.GetEnvironmentVariable("GROQ_MODEL") ?? configuration["Groq:Model"];
        _model = string.IsNullOrWhiteSpace(modelConfig) ? "llama-3.3-70b-versatile" : modelConfig;
    }

    public virtual bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public virtual async Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Groq API key is missing.");
        }

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            messages.Add(new { role = "system", content = systemInstruction });
        }
        messages.Add(new { role = "user", content = prompt });

        var requestPayload = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["messages"] = messages,
            ["temperature"] = 0.5
        };

        if (requireJson)
        {
            requestPayload["response_format"] = new { type = "json_object" };
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = JsonContent.Create(requestPayload)
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[GroqGateway] HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Groq API error: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
