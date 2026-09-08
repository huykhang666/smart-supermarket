namespace SmartSupermarket.Backend.Infrastructure.External.AI;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
}

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiClient(HttpClient httpClient, GeminiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<string> GenerateReportAsync(string prompt)
    {
        // TODO: Call Google Gemini REST API
        return await Task.FromResult("AI Generated Response Placeholder");
    }
}
