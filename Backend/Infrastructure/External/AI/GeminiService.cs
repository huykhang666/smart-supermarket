using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartSupermarket.Backend.Infrastructure.External.AI;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    public GeminiService(IConfiguration configuration)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return "[AI chưa được cấu hình – vui lòng thêm Gemini:ApiKey vào appsettings.json]";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"{BaseUrl}?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return $"[Lỗi Gemini API: {response.StatusCode}] {responseBody}";

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "[Không có nội dung trả về từ AI]";
    }
}
