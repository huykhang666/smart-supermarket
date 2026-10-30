using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartSupermarket.Backend.Infrastructure.External.AI;

public class GeminiService : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "Gemini";

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _configuration = configuration;
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? configuration["Gemini:ApiKey"] ?? string.Empty;
        var modelConfig = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? configuration["Gemini:Model"];
        _model = string.IsNullOrWhiteSpace(modelConfig) ? "gemini-2.5-flash" : modelConfig;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateAsync(prompt, null, false, cancellationToken);
    }

    public async Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_apiKey) && _apiKey.StartsWith("AIzaSy"))
        {
            try
            {
                var geminiResult = await CallGeminiAsync(prompt, systemInstruction, requireJson, cancellationToken);
                if (!string.IsNullOrWhiteSpace(geminiResult) && !geminiResult.StartsWith("[Lỗi"))
                {
                    return geminiResult;
                }
            }
            catch
            {
            }
        }

        var groqKey = _configuration["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            try
            {
                var groqResult = await CallGroqAsync(prompt, systemInstruction, requireJson, groqKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(groqResult) && !groqResult.StartsWith("{\"error\":"))
                {
                    return groqResult;
                }
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "[Gemini API chưa được cấu hình. Vui lòng thêm Gemini:ApiKey vào appsettings.json]";
        }

        return await CallGeminiAsync(prompt, systemInstruction, requireJson, cancellationToken);
    }

    private async Task<string> CallGeminiAsync(
        string prompt,
        string? systemInstruction,
        bool requireJson,
        CancellationToken cancellationToken)
    {
        string fullPrompt = string.IsNullOrWhiteSpace(systemInstruction)
            ? prompt
            : $"[CHỈ DẪN HỆ THỐNG]:\n{systemInstruction}\n\n[YÊU CẦU NGƯỜI DÙNG]:\n{prompt}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = fullPrompt } }
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

        try
        {
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
                return $"[Lỗi Gemini API: {response.StatusCode}] {responseBody}";
            }

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "[Không có phản hồi từ Gemini]";
        }
        catch (Exception ex)
        {
            return $"[Lỗi kết nối Gemini API]: {ex.Message}";
        }
    }

    private async Task<string> CallGroqAsync(
        string prompt,
        string? systemInstruction,
        bool requireJson,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var messages = new System.Collections.Generic.List<object>();
        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            messages.Add(new { role = "system", content = systemInstruction });
        }
        messages.Add(new { role = "user", content = prompt });

        var requestPayload = new System.Collections.Generic.Dictionary<string, object>
        {
            ["model"] = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile",
            ["messages"] = messages,
            ["temperature"] = 0.5
        };

        if (requireJson)
        {
            requestPayload["response_format"] = new { type = "json_object" };
        }

        var requestJson = JsonSerializer.Serialize(requestPayload);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

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

        return content ?? string.Empty;
    }
}
