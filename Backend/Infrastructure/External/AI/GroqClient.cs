using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartSupermarket.Backend.Infrastructure.External.AI;

/// <summary>
/// Client kết nối Groq Cloud API (chạy chip LPU siêu tốc, chuẩn OpenAI-compatible).
/// Hỗ trợ các model hàng đầu: llama-3.3-70b-versatile, llama-3.1-8b-instant.
/// </summary>
public class GroqClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private const string DefaultEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    public string ProviderName => "Groq";

    public GroqClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? string.Empty;
        _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
    }

    public async Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "{\"error\": \"Groq:ApiKey chưa được cấu hình trong appsettings.json\"}";
        }

        // Khung cấu trúc gọi API Groq chuẩn OpenAI Chat Completion
        var messages = new System.Collections.Generic.List<object>();
        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            messages.Add(new { role = "system", content = systemInstruction });
        }
        messages.Add(new { role = "user", content = prompt });

        var requestPayload = new System.Collections.Generic.Dictionary<string, object>
        {
            ["model"] = _model,
            ["messages"] = messages,
            ["temperature"] = 0.3
        };

        if (requireJson)
        {
            requestPayload["response_format"] = new { type = "json_object" };
        }

        var requestJson = JsonSerializer.Serialize(requestPayload);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, DefaultEndpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"{{\"error\": \"Lỗi Groq API [{response.StatusCode}]: {responseBody}\"}}";
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "{}";
    }
}
