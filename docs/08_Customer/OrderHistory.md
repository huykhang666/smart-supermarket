# LỊCH SỬ MUA HÀNG KHÁCH HÀNG (ORDER HISTORY)

## 1. MỤC ĐÍCH

Order History cho phép tra cứu các đơn hàng đã phát sinh của một khách hàng.

Customer không sở hữu trực tiếp dữ liệu Order.

Quan hệ được thực hiện thông qua:

```text
Order.CustomerId
        |
        v
Customer.CustomerId
```

## 2. THÔNG TIN HIỂN THỊ

Danh sách lịch sử mua hàng nên bao gồm:

| Trường           | Mô tả               |
| ---------------- | ------------------- |
| `OrderId`        | Mã đơn hàng         |
| `OrderDate`      | Thời gian mua       |
| `BranchId`       | Chi nhánh           |
| `TotalAmount`    | Tổng tiền hàng      |
| `DiscountAmount` | Tổng giảm giá       |
| `FinalAmount`    | Tiền thực thu       |
| `VoucherId`      | Voucher đã sử dụng  |
| `Status`         | Trạng thái đơn hàng |

## 3. TRẠNG THÁI ORDER

Theo `07_Order`:

```text
1 = Hoàn tất
2 = Đã hủy
```

### Order hoàn tất

Được xem là giao dịch mua hàng thành công.

### Order đã hủy

Không được xem là giao dịch mua hàng còn hiệu lực.

Các ảnh hưởng liên quan:

```text
Inventory -> Hoàn kho
Voucher   -> Khôi phục IsUsed
Loyalty   -> Thu hồi điểm
```

## 4. TRA CỨU LỊCH SỬ

Có thể lọc theo:

```text
CustomerId
BranchId
StartDate
EndDate
Status
```

Ví dụ:

```text
GET /api/v1/customers/1/orders
```

có thể trả về toàn bộ lịch sử mua hàng của Customer có ID `1`.

## 5. XEM CHI TIẾT

Khi xem chi tiết một Order, hệ thống có thể hiển thị:

```text
Order
 ├── OrderDetail
 ├── OrderPromotion
 ├── Payment
 └── Voucher
```

Customer có thể xem thông tin giao dịch nhưng không được phép tự ý thay đổi Order.

## 6. QUYỀN TRUY CẬP

### Customer

Chỉ được xem Order thuộc chính mình.

### Staff

Có thể tra cứu Customer và lịch sử Order phục vụ nghiệp vụ POS theo quyền được cấp.

### Admin / Manager

Có thể tra cứu lịch sử Order phục vụ quản trị và xử lý nghiệp vụ.

## 7. KHÔNG XÓA LỊCH SỬ

Order History không được xóa chỉ vì Customer chuyển sang `INACTIVE`.

Lịch sử giao dịch phải được bảo toàn để phục vụ:

* Audit.
* Báo cáo.
* Đối soát.
* Chăm sóc khách hàng.
* Xử lý khiếu nại.
