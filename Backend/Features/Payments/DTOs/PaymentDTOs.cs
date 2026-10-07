using System.Text.Json.Serialization;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Payments.DTOs;

public class CreatePaymentTransactionRequest
{
    public int OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.QRCode;
    public decimal Amount { get; set; }
}

public class PaymentTransactionResponse
{
    public int PaymentTransactionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PaymentStatusResponse
{
    public int PaymentTransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

public class ZaloPayCallbackPayload
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }
}

public class ZaloPayCallbackData
{
    [JsonPropertyName("app_id")]
    public int AppId { get; set; }

    [JsonPropertyName("app_trans_id")]
    public string AppTransId { get; set; } = string.Empty;

    [JsonPropertyName("app_time")]
    public long AppTime { get; set; }

    [JsonPropertyName("app_user")]
    public string AppUser { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("embed_data")]
    public string EmbedData { get; set; } = string.Empty;

    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("zp_trans_id")]
    public long ZpTransId { get; set; }

    [JsonPropertyName("server_time")]
    public long ServerTime { get; set; }

    [JsonPropertyName("channel")]
    public int Channel { get; set; }

    [JsonPropertyName("merchant_user_id")]
    public string? MerchantUserId { get; set; }
}

public class ZaloPayCallbackResponse
{
    [JsonPropertyName("return_code")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("return_message")]
    public string ReturnMessage { get; set; } = string.Empty;
}

public class SyncGatewayStatusResponse
{
    public int PaymentTransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int GatewayResponseCode { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PaymentTransactionFilterParams
{
    public int? OrderId { get; set; }
    public string? Gateway { get; set; }
    public string? Status { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
