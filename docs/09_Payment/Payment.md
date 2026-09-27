# Payment

## 1. Tổng quan

`Payment` đại diện cho khoản tiền thực tế được ghi nhận cho một Order.

Payment được tạo sau khi hệ thống xác nhận giao dịch thanh toán thành công.

Payment thuộc về một Order và có thể có nhiều Payment trong cùng một Order.

Điều này cho phép hệ thống hỗ trợ thanh toán bằng nhiều phương thức trong cùng một đơn hàng.

---

## 2. Entity

### Payment

| Field         | Type          | Required | Mô tả                  |
| ------------- | ------------- | -------- | ---------------------- |
| PaymentId     | INT IDENTITY  | YES      | Khóa chính             |
| OrderId       | INT           | YES      | ID đơn hàng            |
| PaymentMethod | TINYINT       | YES      | Phương thức thanh toán |
| AmountPaid    | DECIMAL(18,2) | YES      | Số tiền thanh toán     |
| PaymentDate   | DATETIME      | YES      | Thời gian thanh toán   |

---

## 3. PaymentMethod

```text
1 = Cash
2 = QR Code
3 = Card
```

Trong đó:

```text
PaymentMethod = 2
```

được sử dụng cho ZaloPay QR.

---

## 4. Relationship

```text
Order
  │
  └──< Payment
```

Một Order có thể có nhiều Payment.

Ví dụ:

```text
Order #1001
    ├── Cash       100,000
    └── QR Code    200,000
```

Tổng:

```text
300,000
```

Nếu:

```text
FinalAmount = 300,000
```

thì thanh toán hợp lệ.

---

## 5. Business Constraints

### PaymentMethod

Chỉ chấp nhận:

```text
1
2
3
```

Giá trị khác bị từ chối.

### AmountPaid

```text
AmountPaid >= 0
```

### Tổng thanh toán

```text
SUM(Payment.AmountPaid) >= Order.FinalAmount
```

Nếu nhỏ hơn:

```text
400 Bad Request
```

---

## 6. ZaloPay

Đối với ZaloPay:

```text
PaymentMethod = 2
```

Thông tin giao dịch chi tiết không lưu trực tiếp vào Payment.

Thông tin như:

* Transaction ID
* Gateway transaction ID
* App transaction ID
* QR URL
* Payment URL
* Gateway status
* Callback status

được lưu trong `PaymentTransaction`.

---

## 7. Quy tắc ghi Payment

Payment chỉ được ghi nhận khi:

```text
PaymentTransaction.Status = SUCCESS
```

hoặc đối với thanh toán tiền mặt/thẻ:

```text
Payment được xác nhận trực tiếp tại POS.
```

Không tạo Payment thành công khi giao dịch ZaloPay vẫn:

```text
PENDING
PROCESSING
FAILED
EXPIRED
CANCELLED
```
