using System.Net.Http.Json;
using SmartSupermarket.Desktop.Models;

namespace SmartSupermarket.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public ApiClient(string baseUrl = "https://localhost:7123/")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<UserModel?> LoginAsync(string username, string password)
    {
        // TODO: Call POST /api/auth/login
        return await Task.FromResult<UserModel?>(null);
    }

    public async Task<ProductModel?> GetProductByBarcodeAsync(string barcode)
    {
        // TODO: Call GET /api/products/barcode/{barcode}
        return await Task.FromResult<ProductModel?>(null);
    }
}
