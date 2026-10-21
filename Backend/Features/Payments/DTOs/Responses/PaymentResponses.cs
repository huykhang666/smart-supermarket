using System;
using System.Text.Json.Serialization;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Payments.DTOs.Responses;

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
