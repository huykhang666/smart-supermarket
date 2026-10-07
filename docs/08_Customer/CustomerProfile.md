# HỒ SƠ KHÁCH HÀNG (CUSTOMER PROFILE)

## 1. MỤC ĐÍCH

Customer Profile quản lý thông tin cơ bản của khách hàng thành viên trong Smart SuperMarket.

Khách hàng được sử dụng để:

* Nhận diện khách hàng tại POS.
* Liên kết với Order.
* Tích lũy điểm Loyalty.
* Sử dụng Voucher cá nhân.
* Tra cứu lịch sử mua hàng.

## 2. THÔNG TIN KHÁCH HÀNG

Các thông tin chính:

| Trường          | Kiểu        | Mô tả         |
| --------------- | ----------- | ------------- |
| `CustomerId`    | `int`       | Khóa chính    |
| `FullName`      | `string`    | Họ và tên     |
| `Phone`         | `string`    | Số điện thoại |
| `Email`         | `string?`   | Email         |
| `DateOfBirth`   | `DateTime?` | Ngày sinh     |
| `Gender`        | `byte?`     | Giới tính     |
| `Address`       | `string?`   | Địa chỉ       |
| `LoyaltyPoints` | `int`       | Điểm hiện tại |
| `Status`        | `byte`      | Trạng thái    |
| `CreatedAt`     | `DateTime`  | Ngày tạo      |
| `UpdatedAt`     | `DateTime?` | Ngày cập nhật |

## 3. CUSTOMER ID

`CustomerId` là khóa định danh duy nhất của khách hàng.

Ví dụ:

```text
CustomerId = 1
CustomerId = 2
CustomerId = 3
```

CustomerId được sử dụng để liên kết:

```text
Customer
   |
   +---- Order
   |
   +---- Voucher
   |
   +---- PointHistory
```

## 4. SỐ ĐIỆN THOẠI

Số điện thoại là thông tin quan trọng để nhân viên POS tìm kiếm khách hàng.

Quy trình:

```text
Nhập số điện thoại
        |
        v
Tìm Customer
        |
   +----+----+
   |         |
Tìm thấy   Không tìm thấy
   |         |
   v         v
Hiển thị    Tạo mới
Customer    nếu được phép
```

Một số điện thoại không nên được đăng ký cho nhiều Customer đang hoạt động.

## 5. TRẠNG THÁI CUSTOMER

Đề xuất:

```text
1 = ACTIVE
2 = INACTIVE
3 = BLOCKED
```

### ACTIVE

Khách hàng có thể:

* Mua hàng.
* Tích điểm.
* Sử dụng voucher.

### INACTIVE

Khách hàng không còn hoạt động nhưng dữ liệu lịch sử vẫn được giữ.

### BLOCKED

Khách hàng bị hạn chế sử dụng các chức năng thành viên theo chính sách hệ thống.

## 6. KHÔNG XÓA VẬT LÝ CUSTOMER

Không nên xóa vật lý Customer đã phát sinh Order.

Lý do:

```text
Order.CustomerId
       |
       v
Customer.CustomerId
```

Nếu Customer bị xóa, lịch sử giao dịch có thể mất liên kết.

Do đó nên sử dụng trạng thái:

```text
INACTIVE
```

thay cho DELETE.

## 7. CUSTOMER VÀ KHÁCH VÃNG LAI

Order cho phép:

```text
CustomerId = NULL
```

Do đó:

```text
CustomerId = NULL
    -> Khách vãng lai
    -> Không tích điểm
    -> Không có Loyalty transaction
```

Ngược lại:

```text
CustomerId != NULL
    -> Khách thành viên
    -> Có thể tích điểm
    -> Có thể sử dụng Voucher
```

## 8. VALIDATION

Khi tạo hoặc cập nhật Customer:

* `FullName` không được rỗng.
* `Phone` không được rỗng.
* Phone phải đúng định dạng số điện thoại hệ thống quy định.
* Phone không được trùng Customer ACTIVE khác.
* Email nếu có phải đúng định dạng.
* Customer phải có trạng thái hợp lệ.
