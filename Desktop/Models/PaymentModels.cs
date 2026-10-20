using System;

namespace Desktop.Models;

/// <summary>
/// 1. Enum Phương thức thanh toán
/// </summary>
public enum PaymentMethod : byte
{
    Cash = 1,        // Tiền mặt
    VietQR = 2,      // Mã QR Ngân hàng (VietQR - Miễn phí)
    CreditCard = 3   // Thẻ ATM / VISA (Quẹt máy POS)
}

/// <summary>
/// 2. Enum Trạng thái giao dịch
/// </summary>
public enum PaymentStatus
{
    Pending = 0,     // Đang chờ thanh toán
    Success = 1,     // Thanh toán thành công
    Failed = 2,      // Giao dịch thất bại
    Cancelled = 3,   // Khách hủy giao dịch
    Refunded = 4     // Đã hoàn tiền
}

/// <summary>
/// Thông tin cấu hình tài khoản ngân hàng thụ hưởng VietQR
/// </summary>
public static class BankConfig
{
    public const string BankCode = "MB";
    public const string AccountNumber = "0842573858";
    public const string AccountName = "PHAM TRUNG KIEN";
}

/// <summary>
/// Model thực thể giao dịch thanh toán (ánh xạ trực tiếp với bảng PaymentTransaction trong CSDL)
/// </summary>
public class PaymentTransactionModel
{
    public int TransactionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VietQR;
    public decimal Amount { get; set; }
    public decimal? GivenAmount { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? TransferContent { get; set; }
    public string? BankCode { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? CompletedDate { get; set; }
    public string CashierId { get; set; } = string.Empty;
}

/// <summary>
/// DTO gửi yêu cầu thanh toán
/// </summary>
public class PaymentRequestDto
{
    public string InvoiceId { get; set; } = string.Empty;       // Mã hóa đơn (Ví dụ: SSM10293)
    public decimal Amount { get; set; }                         // Số tiền cần thanh toán
    public PaymentMethod Method { get; set; }                   // Phương thức thanh toán
    public string CashierId { get; set; } = string.Empty;       // Mã thu ngân
    public string? CustomerPhone { get; set; }                 // SĐT khách hàng tích điểm
    public decimal? GivenAmount { get; set; }                   // Tiền khách đưa (nếu là tiền mặt)
}

/// <summary>
/// DTO phản hồi kết quả thanh toán
/// </summary>
public class PaymentResponseDto
{
    public bool IsSuccess { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string? QrCodeUrl { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? ChangeAmount { get; set; }
}
