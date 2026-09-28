using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartSupermarket.Backend.Infrastructure.Payments;

namespace SmartSupermarket.Backend.Features.Payments.Services;

public class ZaloPayService : IZaloPayService
{
    private readonly ZaloPayOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZaloPayService> _logger;

    public ZaloPayService(IOptions<ZaloPayOptions> options, HttpClient httpClient, ILogger<ZaloPayService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool isSuccess, string? orderUrl, string? qrCode, string? error)> CreateOrderAsync(string appTransId, decimal amount, string description)
    {
        try
        {
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long amountLong = (long)amount;

            var param = new Dictionary<string, string>
            {
                { "app_id", _options.AppId },
                { "app_user", "SmartSuperMarket_POS" },
                { "app_time", appTime.ToString() },
                { "amount", amountLong.ToString() },
                { "app_trans_id", appTransId },
                { "bank_code", "zalopayapp" },
                { "description", description },
                { "callback_url", _options.CallbackUrl },
                { "embed_data", "{}" },
                { "item", "[]" }
            };

            // HMAC data = app_id + "|" + app_trans_id + "|" + app_user + "|" + amount + "|" + app_time + "|" + embed_data + "|" + item
            string rawData = $"{param["app_id"]}|{param["app_trans_id"]}|{param["app_user"]}|{param["amount"]}|{param["app_time"]}|{param["embed_data"]}|{param["item"]}";
            param.Add("mac", ComputeHmacSha256(rawData, _options.Key1));

            var content = new FormUrlEncodedContent(param);
            var response = await _httpClient.PostAsync(_options.Endpoint, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            int returnCode = root.GetProperty("return_code").GetInt32();

            if (returnCode == 1)
            {
                string orderUrl = root.GetProperty("order_url").GetString() ?? "";
                string qrCode = root.TryGetProperty("qr_code", out var qrProp) ? qrProp.GetString() ?? orderUrl : orderUrl;
                return (true, orderUrl, qrCode, null);
            }
            else
            {
                string returnMsg = root.TryGetProperty("return_message", out var msgProp) ? msgProp.GetString() ?? "Lỗi tạo đơn ZaloPay" : "Lỗi tạo đơn ZaloPay";
                return (false, null, null, returnMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ZaloPay CreateOrder API");
            return (false, null, null, ex.Message);
        }
    }

    public bool VerifyCallback(string data, string mac)
    {
        string expectedMac = ComputeHmacSha256(data, _options.Key2);
        return string.Equals(expectedMac, mac, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<(int returnCode, string returnMessage, string? zpTransId)> QueryOrderStatusAsync(string appTransId)
    {
        try
        {
            var param = new Dictionary<string, string>
            {
                { "app_id", _options.AppId },
                { "app_trans_id", appTransId }
            };

            // HMAC data = app_id + "|" + app_trans_id + "|" + key1
            string rawData = $"{param["app_id"]}|{param["app_trans_id"]}|{_options.Key1}";
            param.Add("mac", ComputeHmacSha256(rawData, _options.Key1));

            var content = new FormUrlEncodedContent(param);
            var response = await _httpClient.PostAsync(_options.QueryEndpoint, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            int returnCode = root.GetProperty("return_code").GetInt32();
            string returnMsg = root.TryGetProperty("return_message", out var msgProp) ? msgProp.GetString() ?? "" : "";
            string? zpTransId = root.TryGetProperty("zp_trans_id", out var zpProp) ? zpProp.ToString() : null;

            return (returnCode, returnMsg, zpTransId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ZaloPay QueryOrderStatus API");
            return (-1, ex.Message, null);
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
