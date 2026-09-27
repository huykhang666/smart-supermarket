# TỔNG QUAN PHÂN HỆ KHÁCH HÀNG (09_CUSTOMER MODULE)

## 1. GIỚI THIỆU

Phân hệ Khách hàng (Customer Module) quản lý thông tin khách hàng thành viên và các nghiệp vụ liên quan đến khách hàng trong hệ thống Smart SuperMarket.

Phân hệ được tích hợp trực tiếp với phân hệ Đơn hàng (07_Order), Voucher, Loyalty và POS.

Các chức năng chính:

* Quản lý hồ sơ khách hàng.
* Tra cứu khách hàng tại quầy POS.
* Quản lý điểm thưởng thành viên.
* Ghi nhận lịch sử biến động điểm.
* Quản lý voucher cá nhân của khách hàng.
* Tra cứu lịch sử mua hàng.
* Liên kết khách hàng với đơn hàng.
* Hỗ trợ thu hồi điểm và khôi phục voucher khi đơn hàng bị hủy.

## 2. PHẠM VI

Phân hệ Customer bao gồm:

1. Customer Profile
2. Loyalty Points
3. Voucher cá nhân
4. Order History
5. Customer Business Rules
6. Customer RESTful API

Phân hệ không trực tiếp xử lý:

* Quản lý sản phẩm.
* Quản lý tồn kho.
* Quản lý chương trình Promotion.
* Xử lý thanh toán.
* Tạo hoặc hủy đơn hàng.

Các nghiệp vụ trên thuộc các phân hệ tương ứng.

## 3. CÁC TÁC TỬ

### 3.1. Nhân viên thu ngân (Staff / Cashier)

Có thể:

* Tìm kiếm khách hàng bằng số điện thoại.
* Gắn khách hàng vào đơn hàng POS.
* Tạo khách hàng mới nếu hệ thống cho phép.
* Kiểm tra điểm thưởng.
* Nhập mã voucher của khách hàng.
* Xem thông tin khách hàng phục vụ giao dịch.

### 3.2. Admin / Manager

Có thể:

* Xem danh sách khách hàng.
* Xem chi tiết khách hàng.
* Cập nhật thông tin khách hàng.
* Tra cứu lịch sử mua hàng.
* Kiểm tra lịch sử điểm.
* Quản lý trạng thái khách hàng.

### 3.3. Customer

Có thể:

* Xem thông tin cá nhân.
* Xem điểm thưởng.
* Xem lịch sử giao dịch.
* Sử dụng voucher hợp lệ của mình trong giao dịch POS.

## 4. QUAN HỆ VỚI PHÂN HỆ ORDER

Customer được liên kết với Order thông qua:

```text
Order.CustomerId -> Customer.CustomerId
```

`CustomerId` trong bảng `Order` cho phép NULL.

Điều này cho phép hệ thống hỗ trợ cả:

* Khách hàng thành viên.
* Khách vãng lai.

Khi `CustomerId = NULL`, đơn hàng không phát sinh tích điểm thành viên.

Khi `CustomerId` có giá trị, hệ thống thực hiện nghiệp vụ Loyalty theo `07_Order Business Rules`.

## 5. TÍCH HỢP VỚI LOYALTY

Theo quy tắc của `07_Order`:

```text
1 điểm / 10.000 VNĐ
```

Điểm được tính dựa trên:

```text
FinalAmount
```

Công thức:

```text
PointsEarned = FLOOR(FinalAmount / 10.000)
```

Điểm được cộng vào:

```text
Customer.LoyaltyPoints
```

và ghi lịch sử tại:

```text
PointHistory
```

## 6. TÍCH HỢP VỚI VOUCHER

Voucher cá nhân được liên kết với khách hàng.

Khi checkout:

```text
Voucher.IsUsed = 0
AND Voucher.ExpiryDate >= Today
```

thì voucher có thể được sử dụng nếu các điều kiện nghiệp vụ khác cũng hợp lệ.

Sau khi Order được tạo thành công:

```text
Voucher.IsUsed = 1
```

Nếu Order bị hủy:

```text
Voucher.IsUsed = 0
```

## 7. DANH MỤC TÀI LIỆU

| File                 | Nội dung                    |
| -------------------- | --------------------------- |
| `README.md`          | Tổng quan phân hệ           |
| `CustomerProfile.md` | Hồ sơ khách hàng            |
| `Loyalty.md`         | Điểm thưởng và lịch sử điểm |
| `Voucher.md`         | Voucher cá nhân             |
| `OrderHistory.md`    | Lịch sử mua hàng            |
| `BusinessRules.md`   | Quy tắc nghiệp vụ           |
| `API.md`             | RESTful API                 |

## 8. NGUYÊN TẮC THIẾT KẾ

Phân hệ Customer phải đảm bảo:

* Không làm thay đổi dữ liệu Order đã hoàn tất.
* Không tự ý cộng điểm ngoài quy trình Order.
* Không cho sử dụng voucher đã dùng.
* Không cho sử dụng voucher hết hạn.
* Mọi biến động điểm phải có lịch sử.
* Khách vãng lai không được cộng Loyalty Points.
* Các nghiệp vụ ảnh hưởng đến Order, Inventory và Loyalty phải được thực hiện trong Transaction phù hợp.
