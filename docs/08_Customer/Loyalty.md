# QUẢN LÝ ĐIỂM THƯỞNG (LOYALTY)

## 1. MỤC ĐÍCH

Loyalty quản lý điểm thưởng của khách hàng thành viên dựa trên giá trị đơn hàng đã thanh toán thành công.

Điểm thưởng được lưu tại:

```text
Customer.LoyaltyPoints
```

Mọi thay đổi điểm phải được ghi nhận tại:

```text
PointHistory
```

## 2. QUY TẮC TÍCH ĐIỂM

Theo `07_Order Business Rules`:

```text
1 điểm = 10.000 VNĐ
```

Điểm được tính trên:

```text
FinalAmount
```

Công thức:

```text
PointsEarned = FLOOR(FinalAmount / 10000)
```

Ví dụ:

| FinalAmount | Điểm |
| ----------: | ---: |
|       9.000 |    0 |
|      10.000 |    1 |
|      50.000 |    5 |
|      99.999 |    9 |
|     100.000 |   10 |
|     250.000 |   25 |

## 3. ĐIỀU KIỆN TÍCH ĐIỂM

Customer chỉ được tích điểm khi:

```text
Order.CustomerId != NULL
AND Order.Status = 1
```

Order phải được thanh toán thành công.

Khách vãng lai:

```text
CustomerId = NULL
```

không được tích điểm.

## 4. QUY TRÌNH CỘNG ĐIỂM

```text
Checkout
   |
   v
Tính FinalAmount
   |
   v
Kiểm tra CustomerId
   |
   +---- NULL ----> Không cộng điểm
   |
   +---- Có ------> Tính PointsEarned
                         |
                         v
                  Customer.LoyaltyPoints += PointsEarned
                         |
                         v
                  Insert PointHistory
```

## 5. POINT HISTORY

Mỗi thay đổi điểm phải tạo một bản ghi lịch sử.

Các loại nghiệp vụ:

```text
Type = 1
Tích lũy điểm

Type = 3
Thu hồi / Điều chỉnh điểm
```

### Tích điểm

```text
PointChange = +PointsEarned
Type = 1
OrderId = OrderId
```

### Thu hồi điểm

Khi Order bị hủy:

```text
PointChange = -PointsEarned
Type = 3
OrderId = OrderId
```

## 6. HỦY ĐƠN VÀ THU HỒI ĐIỂM

Khi Order chuyển:

```text
Status: 1 -> 2
```

hệ thống phải thu hồi số điểm đã phát sinh từ Order đó.

Ví dụ:

```text
Order FinalAmount = 250.000

PointsEarned = 25
```

Sau khi hủy:

```text
Customer.LoyaltyPoints -= 25
```

và:

```text
PointHistory:
PointChange = -25
Type = 3
```

## 7. NGUYÊN TẮC

Không được cập nhật:

```text
Customer.LoyaltyPoints
```

mà không tạo:

```text
PointHistory
```

Điều này đảm bảo có thể audit toàn bộ lịch sử điểm của khách hàng.
