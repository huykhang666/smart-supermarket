# QUY TẮC NGHIỆP VỤ PHÂN HỆ KHÁCH HÀNG (09_CUSTOMER BUSINESS RULES)

## 1. QUY TẮC TẠO KHÁCH HÀNG

1. Customer phải có `FullName`.
2. Customer phải có `Phone`.
3. Số điện thoại không được trùng với Customer ACTIVE khác.
4. Email nếu có phải đúng định dạng.
5. Customer mới được tạo với trạng thái `ACTIVE`.
6. `LoyaltyPoints` ban đầu bằng `0`.

## 2. QUY TẮC KHÁCH VÃNG LAI

Order cho phép:

```text
CustomerId = NULL
```

Trường hợp này được xem là khách vãng lai.

Khách vãng lai:

* Không có hồ sơ Customer.
* Không tích điểm.
* Không phát sinh PointHistory.
* Không được sử dụng Voucher cá nhân yêu cầu Customer.

## 3. QUY TẮC GẮN CUSTOMER VỚI ORDER

Khi checkout tại POS:

```text
CustomerId
```

có thể được bỏ trống.

Nếu có CustomerId:

1. Kiểm tra Customer tồn tại.
2. Kiểm tra Customer có trạng thái được phép giao dịch.
3. Gắn Customer vào Order.
4. Thực hiện nghiệp vụ Loyalty nếu Order thành công.

## 4. QUY TẮC TÍCH ĐIỂM

Theo `07_Order`:

```text
1 điểm / 10.000 VNĐ
```

Công thức:

```text
PointsEarned = FLOOR(FinalAmount / 10000)
```

Điểm chỉ được cộng khi:

```text
Order.Status = 1
AND CustomerId != NULL
```

Ví dụ:

```text
FinalAmount = 250000

PointsEarned = FLOOR(250000 / 10000)
             = 25
```

## 5. QUY TẮC POINT HISTORY

Mọi thay đổi LoyaltyPoints phải có PointHistory.

### Tích điểm

```text
Type = 1
PointChange > 0
```

### Thu hồi điểm

```text
Type = 3
PointChange < 0
```

Không được thay đổi điểm mà không tạo lịch sử.

## 6. QUY TẮC HỦY ORDER

Khi Order:

```text
Status = 1
```

được Admin hoặc Manager hủy:

```text
Status = 2
```

Nếu Order có Customer:

```text
Customer.LoyaltyPoints -= PointsEarned
```

Đồng thời:

```text
PointHistory.Type = 3
PointHistory.PointChange = -PointsEarned
```

## 7. QUY TẮC VOUCHER

Voucher chỉ được sử dụng nếu:

```text
IsUsed = 0
AND ExpiryDate >= Today
```

Nếu voucher gắn với Customer:

```text
Voucher.CustomerId == Order.CustomerId
```

Sau khi Order thành công:

```text
IsUsed = 1
```

Nếu Order bị hủy:

```text
IsUsed = 0
```

## 8. QUY TẮC CHỐNG SỬ DỤNG VOUCHER TRÙNG

Việc kiểm tra và đánh dấu Voucher phải nằm trong Transaction.

Không cho phép một Voucher được sử dụng thành công bởi hai Order đồng thời.

## 9. QUY TẮC CUSTOMER STATUS

```text
ACTIVE
```

Khách hàng đang hoạt động.

```text
INACTIVE
```

Khách hàng không hoạt động nhưng dữ liệu lịch sử vẫn được giữ.

```text
BLOCKED
```

Khách hàng bị hạn chế theo chính sách hệ thống.

## 10. QUY TẮC XÓA CUSTOMER

Không xóa vật lý Customer đã có lịch sử Order.

Thay vào đó:

```text
Status = INACTIVE
```

Điều này bảo toàn quan hệ:

```text
Customer -> Order
Customer -> PointHistory
Customer -> Voucher
```

## 11. QUY TẮC PHÂN QUYỀN

### Staff

* Tìm kiếm Customer.
* Xem thông tin Customer.
* Gắn Customer vào Order.
* Tạo Customer tại POS nếu được cấp quyền.

### Admin / Manager

* Quản lý Customer.
* Cập nhật Customer.
* Xem Loyalty.
* Xem Order History.
* Xử lý các nghiệp vụ quản trị.

### Customer

* Xem hồ sơ của mình.
* Xem điểm của mình.
* Xem lịch sử mua hàng của mình.
* Sử dụng Voucher của mình.

## 12. QUY TẮC TRANSACTION

Các nghiệp vụ ảnh hưởng đồng thời đến:

```text
Order
Inventory
Voucher
Customer
PointHistory
StockHistory
```

phải được xử lý trong Transaction phù hợp.

Nếu bất kỳ bước quan trọng nào thất bại:

```text
ROLLBACK
```

để tránh trạng thái dữ liệu không nhất quán.

Ví dụ không được xảy ra:

```text
Order tạo thành công
Inventory đã trừ
nhưng Loyalty chưa cộng
```

hoặc:

```text
Voucher đã đánh dấu IsUsed = 1
nhưng Order tạo thất bại
```
