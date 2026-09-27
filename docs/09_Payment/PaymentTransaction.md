# PaymentTransaction

## 1. Tổng quan

`PaymentTransaction` lưu thông tin của một giao dịch thanh toán với Payment Gateway.

Entity này được sử dụng đặc biệt cho các phương thức thanh toán bất đồng bộ như ZaloPay.

Không sử dụng `PaymentTransaction` để thay thế `Payment`.

Phân biệt:

```text
Payment
    = khoản tiền đã được ghi nhận cho Order

PaymentTransaction
    = quá trình giao dịch với Payment Gateway
```

---

## 2. Entity

### PaymentTransaction

| Field                | Type          | Required | Mô tả                   |
| -------------------- | ------------- | -------- | ----------------------- |
| PaymentTransactionId | INT IDENTITY  | YES      | Khóa chính              |
| TransactionCode      | VARCHAR(100)  | YES      | Mã giao dịch nội bộ     |
| OrderId              | INT           | YES      | ID Order                |
| PaymentMethod        | TINYINT       | YES      | Phương thức thanh toán  |
| Amount               | DECIMAL(18,2) | YES      | Số tiền                 |
| Gateway              | VARCHAR(50)   | YES      | Payment Gateway         |
| GatewayTransactionId | VARCHAR(150)  | NO       | ID giao dịch từ gateway |
| Status               | VARCHAR(30)   | YES      | Trạng thái              |
| PaymentUrl           | VARCHAR(500)  | NO       | URL thanh toán          |
| QrCode               | VARCHAR(MAX)  | NO       | Thông tin QR            |
| CreatedAt            | DATETIME      | YES      | Thời gian tạo           |
| PaidAt               | DATETIME      | NO       | Thời gian thanh toán    |
| ExpiredAt            | DATETIME      | NO       | Thời gian hết hạn       |

---

## 3. Gateway

Hiện tại:

```text
Gateway = ZALOPAY
```

Thiết kế có thể mở rộng:

```text
ZALOPAY
MOMO
VNPAY
BANK_QR
```

---

## 4. Status

### PENDING

Giao dịch được tạo nhưng chưa bắt đầu xử lý.

### PROCESSING

Giao dịch đang được xử lý.

### SUCCESS

Thanh toán thành công.

### FAILED

Thanh toán thất bại.

### EXPIRED

Giao dịch hết thời gian thanh toán.

### CANCELLED

Giao dịch bị hủy.

---

## 5. State Transition

```text
PENDING
   │
   ▼
PROCESSING
   │
   ├──────────► SUCCESS
   │
   ├──────────► FAILED
   │
   ├──────────► EXPIRED
   │
   └──────────► CANCELLED
```

Sau khi:

```text
SUCCESS
```

không được chuyển ngược về:

```text
PENDING
PROCESSING
FAILED
```

---

## 6. Relationship

```text
Order
  │
  ├──< Payment
  │
  └──< PaymentTransaction
```

Một Order có thể có nhiều PaymentTransaction trong trường hợp giao dịch thất bại và khách hàng thử lại.

Tuy nhiên chỉ giao dịch thành công hợp lệ mới được ghi nhận thành Payment.

---

## 7. Ví dụ

```text
OrderId = 1001
Amount = 250000
Gateway = ZALOPAY
Status = SUCCESS
```

Sau khi callback thành công:

```text
PaymentTransaction
        │
        ▼
Create Payment
        │
        ▼
Order payment completed
```

---

## 8. Duplicate Protection

`TransactionCode` phải là duy nhất.

Ví dụ:

```text
PAY-20260926-000001
```

Không cho phép tạo hai transaction có cùng:

```text
TransactionCode
```

Ngoài ra, `GatewayTransactionId` nếu được gateway cung cấp cũng phải được kiểm tra trùng trước khi cập nhật giao dịch.
