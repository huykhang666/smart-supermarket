# Payment API

## Base URL

```text
/api/v1/payments
```

---

# 1. Create Payment

## Endpoint

```http
POST /api/v1/payments
```

## Authorization

```text
Staff
Admin
```

---

## Request

```json
{
  "orderId": 1001,
  "paymentMethod": 2,
  "amount": 250000
}
```

### Fields

| Field         | Type    | Required | Mô tả       |
| ------------- | ------- | -------- | ----------- |
| orderId       | INT     | YES      | ID Order    |
| paymentMethod | INT     | YES      | Phương thức |
| amount        | DECIMAL | YES      | Số tiền     |

---

## Payment Method

```text
1 = Cash
2 = QR Code
3 = Card
```

ZaloPay sử dụng:

```text
paymentMethod = 2
```

---

## Success Response

```http
201 Created
```

```json
{
  "isSuccess": true,
  "message": "Payment transaction created successfully",
  "data": {
    "paymentTransactionId": 15,
    "transactionCode": "PAY-20260926-000001",
    "orderId": 1001,
    "paymentMethod": 2,
    "gateway": "ZALOPAY",
    "amount": 250000,
    "status": "PENDING",
    "paymentUrl": "https://...",
    "qrCode": "..."
  },
  "errors": null
}
```

---

# 2. Get Payment Transaction

## Endpoint

```http
GET /api/v1/payments/transactions/{id}
```

## Authorization

```text
Staff
Admin
```

---

## Success Response

```json
{
  "isSuccess": true,
  "message": "Payment transaction retrieved successfully",
  "data": {
    "paymentTransactionId": 15,
    "transactionCode": "PAY-20260926-000001",
    "orderId": 1001,
    "paymentMethod": 2,
    "gateway": "ZALOPAY",
    "amount": 250000,
    "status": "SUCCESS",
    "createdAt": "2026-09-26T20:00:00",
    "paidAt": "2026-09-26T20:02:15"
  },
  "errors": null
}
```

---

# 3. Get Payment Status

## Endpoint

```http
GET /api/v1/payments/transactions/{id}/status
```

## Authorization

```text
Staff
Admin
Customer
```

---

## Response

```json
{
  "isSuccess": true,
  "message": "Payment status retrieved successfully",
  "data": {
    "paymentTransactionId": 15,
    "status": "SUCCESS"
  },
  "errors": null
}
```

---

# 4. ZaloPay Callback

## Endpoint

```http
POST /api/v1/payments/zalo-pay/callback
```

Endpoint này được ZaloPay gọi đến backend.

Không yêu cầu Staff hoặc Customer authentication.

Callback phải được xác thực bằng cơ chế chữ ký của ZaloPay.

---

## Callback Processing

```text
Receive callback
      ↓
Verify callback
      ↓
Find PaymentTransaction
      ↓
Check duplicate
      ↓
Check amount
      ↓
Update transaction
      ↓
Create Payment
      ↓
Update Order
```

---

# 5. Cancel Payment Transaction

## Endpoint

```http
POST /api/v1/payments/transactions/{id}/cancel
```

## Authorization

```text
Staff
Admin
```

---

## Rule

Chỉ cho phép hủy:

```text
PENDING
PROCESSING
```

Không cho phép hủy:

```text
SUCCESS
```

---

## Response

```json
{
  "isSuccess": true,
  "message": "Payment transaction cancelled successfully",
  "data": true,
  "errors": null
}
```

---

# 6. List Payment Transactions

## Endpoint

```http
GET /api/v1/payments/transactions
```

## Authorization

```text
Admin
```

---

## Query Parameters

```text
orderId
gateway
status
paymentMethod
startDate
endDate
page
pageSize
```

Ví dụ:

```http
GET /api/v1/payments/transactions?status=SUCCESS&page=1&pageSize=20
```

---

## Response

```json
{
  "isSuccess": true,
  "message": "Payment transactions retrieved successfully",
  "data": {
    "items": [
      {
        "paymentTransactionId": 15,
        "transactionCode": "PAY-20260926-000001",
        "orderId": 1001,
        "gateway": "ZALOPAY",
        "paymentMethod": 2,
        "amount": 250000,
        "status": "SUCCESS",
        "createdAt": "2026-09-26T20:00:00",
        "paidAt": "2026-09-26T20:02:15"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  },
  "errors": null
}
```

---

# 7. Sync Gateway Transaction Status

## Endpoint

```http
POST /api/v1/payments/transactions/{id}/sync-gateway
```

## Authorization

```text
Staff
Admin
```

## Mô tả

Phương thức cho phép POS hoặc Admin chủ động kích hoạt yêu cầu Backend phát lệnh hỏi trạng thái trực tiếp sang ZaloPay API (`/v2/query`), phòng trường hợp bị rớt Callback.

## Response

```json
{
  "isSuccess": true,
  "message": "Gateway status synchronized successfully",
  "data": {
    "paymentTransactionId": 15,
    "status": "SUCCESS",
    "gatewayResponseCode": 1
  },
  "errors": null
}
```

---

# 8. Error Response


```json
{
  "isSuccess": false,
  "message": "Payment transaction failed",
  "data": null,
  "errors": [
    "Payment amount does not match order amount"
  ]
}
```

---

# 8. HTTP Status Codes

| Status | Ý nghĩa                          |
| -----: | -------------------------------- |
|    200 | Thành công                       |
|    201 | Tạo transaction thành công       |
|    400 | Dữ liệu không hợp lệ             |
|    401 | Chưa đăng nhập                   |
|    403 | Không có quyền                   |
|    404 | Không tìm thấy Order/Transaction |
|    409 | Transaction bị trùng             |
|    500 | Lỗi hệ thống                     |
|    502 | Gateway không phản hồi/lỗi       |
