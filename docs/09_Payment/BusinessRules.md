# Payment Business Rules

## 1. Tổng quan

Payment Module phải đảm bảo mọi giao dịch thanh toán được xác thực trước khi ghi nhận vào hệ thống.

---

# 2. Quy tắc thanh toán

## BR-01: Order phải tồn tại

Payment chỉ được tạo cho Order tồn tại.

Nếu:

```text
OrderId không tồn tại
```

trả về:

```text
400 Bad Request
```

---

## BR-02: Số tiền phải hợp lệ

```text
Amount > 0
```

Không chấp nhận:

```text
Amount <= 0
```

---

## BR-03: Không thanh toán vượt quá số tiền cần thiết

Đối với giao dịch ZaloPay:

```text
PaymentTransaction.Amount
=
Order.FinalAmount
```

Backend phải kiểm tra số tiền nhận được từ gateway.

Nếu số tiền không khớp:

```text
Payment không được ghi nhận
```

---

## BR-04: PaymentMethod

Chỉ chấp nhận:

```text
1 = Cash
2 = QR Code
3 = Card
```

ZaloPay:

```text
PaymentMethod = 2
```

---

## BR-05: Gateway

Đối với ZaloPay:

```text
Gateway = ZALOPAY
```

---

## BR-06: TransactionCode duy nhất

Mỗi giao dịch phải có:

```text
TransactionCode
```

duy nhất.

---

## BR-07: Callback phải được xác thực

Không được cập nhật Payment chỉ dựa vào dữ liệu callback chưa được xác thực.

Backend phải kiểm tra chữ ký/xác thực theo cơ chế của ZaloPay trước khi cập nhật giao dịch.

---

## BR-08: Callback phải có tính idempotent

Nếu cùng một callback được gửi nhiều lần:

```text
Callback 1 → SUCCESS
Callback 2 → SUCCESS
Callback 3 → SUCCESS
```

hệ thống chỉ được ghi nhận:

```text
1 Payment
```

---

## BR-09: Chỉ SUCCESS mới tạo Payment

```text
PENDING       → Không tạo Payment
PROCESSING    → Không tạo Payment
FAILED        → Không tạo Payment
EXPIRED       → Không tạo Payment
CANCELLED     → Không tạo Payment
SUCCESS       → Tạo Payment
```

---

## BR-10: Kiểm tra Order đã thanh toán

Nếu Order đã được thanh toán đầy đủ:

```text
SUM(Payment.AmountPaid) >= Order.FinalAmount
```

không được tạo thêm giao dịch thanh toán cho cùng Order.

---

## BR-11: Transaction phải được lưu trước khi gọi gateway

Khi tạo giao dịch:

```text
Create PaymentTransaction
        ↓
Call ZaloPay
        ↓
Update PaymentTransaction
```

Nếu request đến ZaloPay thất bại:

```text
Status = FAILED
```

---

## BR-12: Transaction Timeout

Nếu giao dịch vượt quá thời gian cho phép:

```text
Status = EXPIRED
```

Không được ghi nhận Payment.

---

## BR-13: Đối soát chủ động & Truy vấn trạng thái (Active Query Status)

Khi giao dịch ở trạng thái `PROCESSING` hoặc `PENDING` quá thời gian dự kiến (hoặc khi POS Polling mà chưa có Callback):

1. Backend được phép gọi API Query của Gateway để kiểm tra kết quả trực tiếp.
2. Nếu Gateway phản hồi thanh toán đã thành công (`SUCCESS`), Backend tự động kích hoạt tiến trình ghi nhận `Payment` và cập nhật `Order` giống như khi nhận Callback chuẩn.

---

## BR-14: Phân tách môi trường Sandbox và Production

1. Mọi khóa bảo mật (`AppId`, `Key1`, `Key2`) môi trường Sandbox tuyệt đối không dùng chung với Production.
2. Dữ liệu thử nghiệm từ Sandbox không được phép ghi nhận vào báo cáo doanh thu chính thức của siêu thị.


---

# 3. Luồng xử lý thành công

```text
Create Transaction
        ↓
PENDING
        ↓
Send to ZaloPay
        ↓
PROCESSING
        ↓
Customer pays
        ↓
Callback
        ↓
Verify callback
        ↓
Verify amount
        ↓
SUCCESS
        ↓
Create Payment
        ↓
Update Order
```

---

# 4. Luồng thất bại

```text
Create Transaction
        ↓
PENDING
        ↓
ZaloPay
        ↓
FAILED
```

Không tạo Payment.

---

# 5. Luồng callback trùng

```text
Callback
   ↓
Find Transaction
   ↓
Status = SUCCESS?
   ├── YES → Return success
   └── NO  → Process callback
```

---

# 6. Transaction Database

Các thao tác quan trọng phải được thực hiện trong database transaction khi cần:

```text
Update PaymentTransaction
        +
Create Payment
        +
Update Order
```

Nếu một thao tác thất bại:

```text
Rollback
```

để tránh trạng thái không nhất quán.
