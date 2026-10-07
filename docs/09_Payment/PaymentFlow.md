# Payment Flow

## 1. Tổng quan

Payment Flow mô tả quá trình thanh toán bằng ZaloPay QR từ lúc POS tạo giao dịch cho đến khi Order được xác nhận thanh toán.

---

# 2. Main Flow

```text
┌─────────┐
│   POS   │
└────┬────┘
     │
     │ Create Payment
     ▼
┌──────────────────┐
│ Payment Service  │
└────┬─────────────┘
     │
     │ Create Transaction
     ▼
┌──────────────────────┐
│ PaymentTransaction   │
│ Status = PENDING     │
└────┬─────────────────┘
     │
     │ Request Payment
     ▼
┌──────────────┐
│   ZaloPay    │
└────┬─────────┘
     │
     │ QR / Payment URL
     ▼
┌──────────────┐
│     POS      │
└────┬─────────┘
     │
     │ Display QR
     ▼
┌──────────────┐
│   Customer   │
└────┬─────────┘
     │
     │ Scan QR
     ▼
┌──────────────┐
│ ZaloPay App  │
└────┬─────────┘
     │
     │ Payment
     ▼
┌──────────────┐
│   ZaloPay    │
└────┬─────────┘
     │
     │ Callback
     ▼
┌──────────────────┐
│ Payment Service  │
└────┬─────────────┘
     │
     │ Verify
     ▼
┌──────────────────────┐
│ PaymentTransaction   │
│ Status = SUCCESS     │
└────┬─────────────────┘
     │
     │ Create Payment
     ▼
┌──────────────┐
│    Order     │
└──────────────┘
```

---

# 3. Create Payment

POS gửi:

```http
POST /api/v1/payments
```

Request:

```json
{
  "orderId": 1001,
  "paymentMethod": 2,
  "amount": 250000
}
```

Payment Service kiểm tra:

```text
Order tồn tại?
Amount hợp lệ?
Order đã thanh toán chưa?
PaymentMethod hợp lệ?
```

---

# 4. Create Transaction

Nếu hợp lệ:

```text
PaymentTransaction
```

được tạo:

```text
Status = PENDING
Gateway = ZALOPAY
```

---

# 5. Request ZaloPay

Payment Service gửi yêu cầu đến ZaloPay.

Nếu thành công:

```text
Status = PROCESSING
```

và nhận:

```text
PaymentUrl
QrCode
GatewayTransactionId
```

---

# 6. Display QR

Backend trả thông tin QR về POS.

POS hiển thị QR.

Customer sử dụng ZaloPay để quét QR.

---

# 7. Customer Payment

```text
Customer
    ↓
Scan QR
    ↓
ZaloPay App
    ↓
Confirm Payment
```

---

# 8. Callback

ZaloPay gửi callback về:

```text
POST /api/v1/payments/zalo-pay/callback
```

Payment Service:

```text
Receive callback
      ↓
Verify signature
      ↓
Find transaction
      ↓
Check transaction status
      ↓
Check amount
      ↓
Update status
```

---

# 9. Success

Nếu hợp lệ:

```text
PaymentTransaction.Status = SUCCESS
```

Sau đó:

```text
Create Payment
```

với:

```text
PaymentMethod = 2
AmountPaid = Transaction.Amount
```

---

# 10. Failed

Nếu thanh toán thất bại:

```text
PaymentTransaction.Status = FAILED
```

Không tạo Payment.

---

# 11. Expired

Nếu QR hết hạn:

```text
PaymentTransaction.Status = EXPIRED
```

Customer cần tạo giao dịch mới.

---

# 12. Duplicate Callback

Nếu callback đã được xử lý:

```text
Status = SUCCESS
```

backend không tạo Payment lần thứ hai.

---

# 13. Error Flow

```text
Create Transaction
       ↓
Call ZaloPay
       ↓
Request failed
       ↓
Status = FAILED
       ↓
Return error to POS
```

---

# 14. Order Integration

Khi thanh toán thành công:

```text
PaymentTransaction
        ↓
Payment
        ↓
Order
```

Order chỉ được xem là đã thanh toán đầy đủ khi:

```text
SUM(Payment.AmountPaid) >= Order.FinalAmount
```

---

# 15. Lưu ý kiến trúc

ZaloPay là phương thức thanh toán bất đồng bộ.

Do đó không nên thiết kế:

```text
POST /orders
    ↓
Call ZaloPay
    ↓
Wait callback
    ↓
Return response
```

thành một HTTP request duy nhất.

Nên tách:

```text
Create Order / Payment Intent
        ↓
Create ZaloPay Transaction
        ↓
Customer Payment
        ↓
Callback
        ↓
Finalize Payment
        ↓
Complete Order
```

Cách này giúp hệ thống xử lý được trường hợp khách hàng mất kết nối hoặc callback đến sau.

---

# 16. Fallback Polling & Active Query Flow

Mô tả luồng phòng thủ khi Callback từ ZaloPay bị trễ hoặc thất bại do lỗi mạng:

```text
┌─────────┐                ┌──────────────────┐               ┌──────────────┐
│   POS   │                │ Payment Service  │               │   ZaloPay    │
└────┬────┘                └────────┬─────────┘               └──────┬───────┘
     │                              │                                │
     │ GET /transactions/{id}/status│                                │
     ├─────────────────────────────►│                                │
     │ (Polling mỗi 3s)             │ Check Status                   │
     │                              │ Status == PROCESSING           │
     │                              │                                │
     │                              │ Active Query Gateway           │
     │                              ├───────────────────────────────►│
     │                              │ POST /v2/query                 │
     │                              │ (app_trans_id)                 │
     │                              │                                │
     │                              │ Gateway Response               │
     │                              │ return_code == 1 (SUCCESS)     │
     │                              │◄───────────────────────────────┤
     │                              │                                │
     │                              │ Update Transaction = SUCCESS   │
     │                              │ Create Payment                 │
     │                              │ Update Order Status            │
     │                              │                                │
     │ Status = SUCCESS             │                                │
     │◄─────────────────────────────┤                                │
     │                              │                                │
```

