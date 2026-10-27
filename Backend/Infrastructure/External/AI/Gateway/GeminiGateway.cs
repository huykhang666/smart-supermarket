using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class GeminiGateway
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiGateway> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiGateway(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiGateway> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        var modelConfig = configuration["Gemini:Model"];
        _model = string.IsNullOrWhiteSpace(modelConfig) ? "gemini-2.5-flash" : modelConfig;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) && _apiKey.StartsWith("AIzaSy");

    public async Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Gemini API key is missing or invalid.");
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            systemInstruction = string.IsNullOrWhiteSpace(systemInstruction)
                ? null
                : new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048,
                response_mime_type = requireJson ? "application/json" : "text/plain"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var fallbackUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            response = await _httpClient.PostAsync(fallbackUrl, content, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                fallbackUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
                response = await _httpClient.PostAsync(fallbackUrl, content, cancellationToken);
            }
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[GeminiGateway] HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
