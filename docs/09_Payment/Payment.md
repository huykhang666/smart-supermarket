Markdown
# Payment Models & Enums - Smart SuperMarket

## 1. Enum Phương Thức Thanh Toán (PaymentMethod)
```csharp
namespace SmartSuperMarket.Payment
{
    public enum PaymentMethod
    {
        Cash = 1,        // Tiền mặt
        VietQR = 2,      // Mã QR Ngân hàng (VietQR - Miễn phí)
        CreditCard = 3   // Thẻ ATM / VISA (Quẹt máy POS)
    }
}
2. Enum Trạng Thái Giao Dịch (PaymentStatus)
C#
namespace SmartSuperMarket.Payment
{
    public enum PaymentStatus
    {
        Pending = 0,     // Đang chờ thanh toán
        Success = 1,     // Thanh toán thành công
        Failed = 2,      // Giao dịch thất bại
        Cancelled = 3,   // Khách hủy giao dịch
        Refunded = 4     // Đã hoàn tiền
    }
}
3. DTO Dữ Liệu Thanh Toán (PaymentDTO)
C#
namespace SmartSuperMarket.Payment
{
    public class PaymentRequestDto
    {
        public string InvoiceId { get; set; }       // Mã hóa đơn (Ví dụ: SSM10293)
        public decimal Amount { get; set; }         // Số tiền cần thanh toán
        public PaymentMethod Method { get; set; }   // Phương thức thanh toán
        public string CashierId { get; set; }       // Mã thu ngân
        public string CustomerPhone { get; set; }  // SĐT khách hàng tích điểm
    }

    public class PaymentResponseDto
    {
        public bool IsSuccess { get; set; }
        public string TransactionCode { get; set; }
        public string QrCodeUrl { get; set; }
        public string Message { get; set; }
    }
}