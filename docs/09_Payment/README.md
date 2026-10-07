# Payment Module

## 1. Tổng quan

Payment Module chịu trách nhiệm xử lý các giao dịch thanh toán trong hệ thống Smart SuperMarket.

Module hỗ trợ thanh toán thông qua:

* Tiền mặt.
* QR Code.
* Thẻ ngân hàng.
* Cổng thanh toán trực tuyến ZaloPay thông qua QR Code.

Trong phiên bản hiện tại, hệ thống lựa chọn **ZaloPay QR** làm phương thức thanh toán trực tuyến chính.

Payment Module kết nối với `Order Module` để đảm bảo đơn hàng chỉ được hoàn tất khi thanh toán hợp lệ.

---

## 2. Mục tiêu

Payment Module có các mục tiêu:

* Tạo giao dịch thanh toán.
* Theo dõi trạng thái thanh toán.
* Tích hợp ZaloPay.
* Sinh thông tin QR Code/payment URL.
* Nhận callback từ ZaloPay.
* Xác thực callback.
* Cập nhật trạng thái giao dịch.
* Liên kết giao dịch thanh toán với Order.
* Ngăn chặn thanh toán trùng.
* Hỗ trợ kiểm tra và tra cứu giao dịch.

---

## 3. Actor

### Staff

* Chọn phương thức thanh toán.
* Tạo giao dịch thanh toán.
* Hiển thị QR Code cho khách hàng.
* Kiểm tra trạng thái thanh toán.

### Customer

* Quét QR Code.
* Thanh toán thông qua ZaloPay.
* Nhận kết quả thanh toán.

### Admin / Manager

* Xem giao dịch thanh toán.
* Kiểm tra trạng thái giao dịch.
* Hỗ trợ xử lý giao dịch lỗi.

### ZaloPay

* Tiếp nhận yêu cầu thanh toán.
* Xử lý giao dịch.
* Gửi callback về hệ thống.

---

## 4. Quan hệ với Order Module

Order Module hiện có bảng:

`Payment`

với:

| Field         | Mô tả                  |
| ------------- | ---------------------- |
| PaymentId     | ID thanh toán          |
| OrderId       | ID đơn hàng            |
| PaymentMethod | Phương thức thanh toán |
| AmountPaid    | Số tiền                |
| PaymentDate   | Thời gian thanh toán   |

Payment Module sử dụng bảng này để lưu kết quả thanh toán cuối cùng.

Ngoài ra, Payment Module có bảng:

`PaymentTransaction`

để lưu thông tin kỹ thuật của giao dịch ZaloPay.

---

## 5. Payment Method

Hệ thống hiện quy định:

| Code | Phương thức |
| ---: | ----------- |
|    1 | Cash        |
|    2 | QR Code     |
|    3 | Card        |

ZaloPay QR được ánh xạ vào:

```text
PaymentMethod = 2
```

---

## 6. Trạng thái PaymentTransaction

```text
PENDING
    ↓
PROCESSING
    ↓
SUCCESS

PENDING / PROCESSING
    ↓
FAILED

PENDING / PROCESSING
    ↓
EXPIRED

PENDING / PROCESSING
    ↓
CANCELLED
```

---

## 7. Tài liệu

* `Payment.md`: Thiết kế Payment.
* `ZaloPay.md`: Thiết kế tích hợp ZaloPay (hỗ trợ ZaloPay Sandbox & Polling/Query).
* `PaymentTransaction.md`: Thiết kế giao dịch thanh toán.
* `BusinessRules.md`: Quy tắc nghiệp vụ (BR-01 đến BR-14).
* `PaymentFlow.md`: Luồng xử lý thanh toán (bao gồm Callback & Active Query Fallback Flow).
* `API.md`: API của Payment Module (kèm Endpoint đồng bộ trạng thái Gateway).

