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
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "Gemini";

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        var modelConfig = configuration["Gemini:Model"];
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
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "[Gemini API chưa được cấu hình. Vui lòng thêm Gemini:ApiKey vào appsettings.json]";
        }

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

            // Tự động thử fallback sang gemini-2.0-flash hoặc gemini-1.5-flash nếu model chưa phát hành chính thức
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
}
