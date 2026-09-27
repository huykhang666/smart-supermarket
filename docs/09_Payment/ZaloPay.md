# ZaloPay Integration

## 1. Tổng quan

ZaloPay được sử dụng làm cổng thanh toán QR cho hệ thống Smart SuperMarket.

Khách hàng sử dụng ứng dụng ZaloPay để quét QR Code và thực hiện thanh toán.

Luồng tổng quát:

```text
POS
 │
 ▼
Payment Service
 │
 ▼
ZaloPay
 │
 ▼
QR Code
 │
 ▼
Customer
 │
 ▼
ZaloPay App
 │
 ▼
ZaloPay
 │
 ▼
Callback
 │
 ▼
Payment Service
 │
 ▼
PaymentTransaction
 │
 ▼
Order
```

---

## 2. Vai trò của ZaloPay

ZaloPay chịu trách nhiệm:

* Nhận yêu cầu thanh toán.
* Tạo giao dịch.
* Cung cấp thông tin thanh toán.
* Xử lý thanh toán.
* Gửi kết quả giao dịch về hệ thống.

Payment Service chịu trách nhiệm:

* Tạo giao dịch.
* Gửi request đến ZaloPay.
* Kiểm tra response.
* Xác thực callback.
* Cập nhật PaymentTransaction.
* Ghi nhận Payment khi giao dịch thành công.

---

## 3. Thông tin giao dịch

Mỗi giao dịch cần có mã giao dịch nội bộ.

Ví dụ:

```text
PAY-20260926-000001
```

Ngoài ra giao dịch có thể lưu mã giao dịch của ZaloPay.

```text
GatewayTransactionId
```

Hai mã này được dùng để đối chiếu giao dịch.

---

## 4. QR Payment

Sau khi tạo giao dịch thành công:

```text
Payment Service
       │
       ▼
    ZaloPay
       │
       ▼
QR / Payment URL
```

Payment Service trả thông tin QR cho POS.

POS hiển thị QR cho khách hàng.

Khách hàng:

```text
Mở ZaloPay
    ↓
Quét QR
    ↓
Kiểm tra số tiền
    ↓
Xác nhận thanh toán
```

---

## 5. Callback

Sau khi giao dịch được xử lý, ZaloPay gửi callback về backend.

Backend thực hiện:

1. Nhận callback.
2. Xác thực callback.
3. Tìm `PaymentTransaction`.
4. Kiểm tra giao dịch.
5. Kiểm tra số tiền.
6. Kiểm tra giao dịch đã xử lý hay chưa.
7. Cập nhật trạng thái.
8. Nếu thành công thì tạo Payment.
9. Cập nhật Order.

---

## 6. Idempotency

Callback có thể được gửi lại.

Backend không được tạo Payment nhiều lần cho cùng một giao dịch.

Ví dụ:

```text
Callback #1
    ↓
SUCCESS
    ↓
Create Payment
```

Nếu nhận:

```text
Callback #2
    ↓
SUCCESS
```

hệ thống phải phát hiện giao dịch đã xử lý.

Không được tạo thêm Payment.

---

## 7. Security

Thông tin xác thực ZaloPay phải được lưu trong:

```text
Environment Variables
```

Không được hard-code:

```text
Key1
Key2
Secret Key
```

trong source code.

Ví dụ:

```text
ZALOPAY_APP_ID
ZALOPAY_KEY1
ZALOPAY_KEY2
ZALOPAY_ENDPOINT
```

---

## 8. Environment

Ví dụ:

```text
ZALOPAY_APP_ID=...
ZALOPAY_KEY1=...
ZALOPAY_KEY2=...
ZALOPAY_ENDPOINT=...
ZALOPAY_CALLBACK_URL=...
```

Giá trị thực tế được cấu hình theo môi trường:

```text
Development
Production
```

Không commit secret lên GitHub.
