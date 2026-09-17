# THIẾT KẾ THỰC THỂ CSDL PHÂN HỆ ĐƠN HÀNG (ORDER ENTITY DESIGN - V2.0)

Theo chuẩn hóa Cơ sở dữ liệu phiên bản 2.0 (Bảng 10, 11, 12, 13 trong ERD).

---

## 1. BẢNG `Order` (Bảng số 10)
Quản lý thông tin chung của từng hóa đơn bán hàng.

| Tên Cột | Kiểu C# | Kiểu CSDL SQL | Ràng buộc | Diễn giải |
| :--- | :--- | :--- | :--- | :--- |
| `OrderId` | `int` | `INT IDENTITY(1,1)` | PK, NOT NULL | Khóa chính mã đơn hàng |
| `EmployeeId` | `int` | `INT` | FK -> User(UserId), NOT NULL | Nhân viên / Thu ngân lập đơn |
| `CustomerId` | `int?` | `INT` | FK -> Customer(CustomerId), NULL | Khách hàng thành viên (nếu có) |
| `BranchId` | `int` | `INT` | FK -> Branch(BranchId), NOT NULL | Chi nhánh phát sinh đơn lẻ |
| `OrderDate` | `DateTime` | `DATETIME` | DEFAULT GETDATE() | Thời điểm tạo hóa đơn |
| `TotalAmount` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Tổng tiền hàng trước giảm giá |
| `DiscountAmount`| `decimal` | `DECIMAL(18,2)` | DEFAULT 0, CHECK >= 0 | Tổng số tiền được giảm |
| `VoucherId` | `int?` | `INT` | FK -> Voucher(VoucherId), NULL | Voucher cá nhân áp dụng (nếu có) |
| `FinalAmount` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Tiền thực thu (`TotalAmount - DiscountAmount`) |
| `Status` | `byte` | `TINYINT` | DEFAULT 1, CHECK IN (1, 2) | 1 = Hoàn tất, 2 = Đã hủy |
| `CreatedAt` | `DateTime` | `DATETIME` | DEFAULT GETDATE() | Ngày giờ tạo bản ghi |

---

## 2. BẢNG `OrderDetail` (Bảng số 12)
Lưu chi tiết từng sản phẩm xuất bán trong hóa đơn.

| Tên Cột | Kiểu C# | Kiểu CSDL SQL | Ràng buộc | Diễn giải |
| :--- | :--- | :--- | :--- | :--- |
| `OrderDetailId`| `int` | `INT IDENTITY(1,1)` | PK, NOT NULL | Khóa chính dòng chi tiết |
| `OrderId` | `int` | `INT` | FK -> Order(OrderId) ON DELETE CASCADE | Thuộc về hóa đơn nào |
| `ProductId` | `int` | `INT` | FK -> Product(ProductId), NOT NULL | Mã sản phẩm |
| `Quantity` | `int` | `INT` | NOT NULL, CHECK > 0 | Số lượng sản phẩm bán ra |
| `UnitPrice` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Đơn giá niêm yết tại thời điểm bán |
| `SubTotal` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Thành tiền (`Quantity * UnitPrice`) |

---

## 3. BẢNG `OrderPromotion` (Bảng số 11 - Quan hệ N-N)
Ghi nhận các chương trình khuyến mãi tự động áp dụng trên hóa đơn.

| Tên Cột | Kiểu C# | Kiểu CSDL SQL | Ràng buộc | Diễn giải |
| :--- | :--- | :--- | :--- | :--- |
| `OrderPromotionId` | `int` | `INT IDENTITY(1,1)` | PK, NOT NULL | Khóa chính dòng khuyến mãi đơn |
| `OrderId` | `int` | `INT` | FK -> Order(OrderId) ON DELETE CASCADE | Thuộc hóa đơn nào |
| `PromotionId` | `int` | `INT` | FK -> Promotion(PromotionId), NOT NULL | Mã chương trình khuyến mãi |
| `DiscountAmount` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Số tiền giảm từ promotion này |

---

## 4. BẢNG `Payment` (Bảng số 13)
Ghi nhận các giao dịch thanh toán cho hóa đơn.

| Tên Cột | Kiểu C# | Kiểu CSDL SQL | Ràng buộc | Diễn giải |
| :--- | :--- | :--- | :--- | :--- |
| `PaymentId` | `int` | `INT IDENTITY(1,1)` | PK, NOT NULL | Khóa chính bản ghi thanh toán |
| `OrderId` | `int` | `INT` | FK -> Order(OrderId) ON DELETE CASCADE | Thuộc hóa đơn nào |
| `PaymentMethod` | `byte` | `TINYINT` | NOT NULL, CHECK IN (1, 2, 3) | 1 = Tiền mặt, 2 = QR Code, 3 = Thẻ |
| `AmountPaid` | `decimal` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 | Số tiền thanh toán qua hình thức này |
| `PaymentDate` | `DateTime` | `DATETIME` | DEFAULT GETDATE() | Thời gian thực hiện thanh toán |
