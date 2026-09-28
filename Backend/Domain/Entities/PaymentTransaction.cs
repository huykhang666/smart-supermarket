using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Domain.Entities;

public class PaymentTransaction
{
    public int PaymentTransactionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.QRCode;
    public decimal Amount { get; set; }
    public string Gateway { get; set; } = "ZALOPAY";
    public string? GatewayTransactionId { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    public Order? Order { get; set; }
}
