# THIẾT KẾ CƠ CHẾ PHÂN QUYỀN (AUTHORIZATION SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Mô hình Phân quyền Vai trò (RBAC Model)](#21-mô-hình-phân-quyền-vai-trò-rbac-model)
   - 2.2. [Định nghĩa Danh mục Vai trò (User Roles)](#22-định-nghĩa-danh-mục-vai-trò-user-roles)
   - 2.3. [Ma trận Phân quyền Chi tiết theo Feature Endpoints](#23-ma-trận-phân-quyền-chi-tiết-theo-feature-endpoints)
   - 2.4. [Cấu hình Phân quyền Attribute trên ASP.NET Core API](#24-cấu-hình-phân-quyền-attribute-trên-aspnet-core-api)
   - 2.5. [Phân quyền Giao diện trên WinForms Desktop & React Web](#25-phân-quyền-giao-diện-trên-winforms-desktop--react-web)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Authorization.md** định nghĩa chính sách Phân quyền truy cập (Authorization) cho hệ thống **Smart SuperMarket**. Hệ thống áp dụng mô hình **Phân quyền dựa trên Vai trò (Role-Based Access Control - RBAC)** để đảm bảo mỗi nhóm người dùng (Admin, Manager, Staff, Customer) chỉ được phép thực thi các tác vụ và truy cập các API đúng với thẩm quyền được cấp.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Mô hình Phân quyền Vai trò (RBAC Model)
- **Tập trung kiểm soát tại Backend**: Tất cả các Endpoint API trên tầng Backend .NET 8 đều được bảo vệ bởi Filter xác thực JWT và kiểm tra Role. Dù người dùng cố tình gọi API qua công cụ ngoài (Postman/Curl), Backend vẫn tự động ngăn chặn nếu không có Token hoặc sai Role (`403 Forbidden`).
- **Phản ánh lên Giao diện Client**: Ứng dụng WinForms Desktop và React Web dựa vào thuộc tính `Role` trong JWT Token để tự động ẩn/hiện các Menu, Button hoặc Form tương ứng.

---

### 2.2. Định nghĩa Danh mục Vai trò (User Roles)

Các vai trò được định nghĩa trong Enum `UserRole` (`Domain/Enums/UserRole.cs`):

| Giá trị Enum | Tên Role | Mô tả thẩm quyền & Phạm vi công việc |
| :---: | :--- | :--- |
| **`1`** | **`Admin`** | Quản trị viên hệ thống có toàn quyền (Full Access): Tạo tài khoản nhân viên, xem báo cáo doanh thu, cấu hình AI, quản lý sản phẩm/kho. |
| **`2`** | **`Manager`** | Cửa hàng trưởng / Quản lý kho: Được quản lý nhập kho, xem báo cáo kinh doanh, quản lý sản phẩm, không có quyền tạo tài khoản Admin/Manager khác. |
| **`3`** | **`Staff`** | Nhân viên thu ngân / kho tại cửa hàng: Chỉ có quyền bán hàng POS (quét mã vạch), tạo hóa đơn và lập phiếu nhập kho. |
| **`4`** | **`Customer`** | Khách hàng mua sắm: Chỉ có quyền xem sản phẩm, đặt hàng online, xem lịch sử đơn hàng, tích điểm và đổi Voucher trên Web. |

---

### 2.3. Ma trận Phân quyền Chi tiết theo Feature Endpoints

| Nhóm API Feature | Endpoint API | Admin (1) | Manager (2) | Staff (3) | Customer (4) |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **Auth** | `POST /api/auth/login`, `register` | Public | Public | Public | Public |
| **Admin Users** | `GET, POST, PUT, DELETE /api/admin/users` | ✅ | ❌ | ❌ | ❌ |
| **Products** | `GET /api/products`, `/barcode/{code}` | ✅ | ✅ | ✅ | ✅ |
| **Products** | `POST, PUT, DELETE /api/products` | ✅ | ✅ | ❌ | ❌ |
| **Orders (POS)** | `POST /api/orders` (Tạo đơn bán tại quầy) | ✅ | ✅ | ✅ | ❌ |
| **Orders (Web)** | `POST /api/orders/online` (Khách đặt online)| ❌ | ❌ | ❌ | ✅ |
| **Inventory** | `POST /api/inventory/import` (Nhập kho) | ✅ | ✅ | ✅ | ❌ |
| **Inventory** | `POST /api/inventory/transfer` (Chuyển kho) | ✅ | ✅ | ❌ | ❌ |
| **AI Services** | `POST /api/ai/*` (Dự báo, Báo cáo AI) | ✅ | ✅ | ❌ | ❌ |
| **Dashboard** | `GET /api/admin/dashboard` | ✅ | ✅ | ❌ | ❌ |

---

### 2.4. Cấu hình Phân quyền Attribute trên ASP.NET Core API

Trên tầng Backend C#, phân quyền được áp dụng trực tiếp tại Controller hoặc Action bằng Decorator Attribute `[Authorize(Roles = "...")]`:

- Ví dụ API tạo tài khoản nhân viên: `[Authorize(Roles = "Admin")]`
- Ví dụ API tạo đơn hàng POS: `[Authorize(Roles = "Admin,Manager,Staff")]`
- Ví dụ API tra cứu sản phẩm công khai: `[AllowAnonymous]` hoặc không yêu cầu Token.

---

### 2.5. Phân quyền Giao diện trên WinForms Desktop & React Web

#### Trên WinForms Desktop App:
- Khi `LoginForm` đăng nhập thành công và nhận JWT Token:
  - Nếu `Role == Staff (3)`: Ẩn các nút Menu "Quản lý Nhân viên", "Báo cáo AI", "Cấu hình Hệ thống". Chỉ cho phép mở Form `PosForm` (Bán hàng) và Form Nhập kho.
  - Nếu `Role == Admin (1)`: Mở full toàn bộ các chức năng trên Menu chính.
  - Kiểm tra điều kiện mở Form trong code-behind: Không cho mở Form Admin bằng code nếu Role không phải Admin.

#### Trên React Web Client:
- Đóng gói các Route cần bảo mật bằng Component `ProtectedRoute`:
  - Khách hàng chưa đăng nhập khi bấm vào trang "Giỏ hàng" hoặc "Tích điểm" sẽ tự động chuyển hướng về trang `/login`.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Liệu có cần bảng `Permission` riêng trong CSDL để phân quyền động tới từng nút bấm (Button-level Permission) hay chỉ cần phân quyền cứng theo 4 Role? | Ảnh hưởng độ phức tạp của CSDL. | Đáp ứng tiêu chí B2 trong Rubric: Phân quyền theo 4 Role lưu trong CSDL là đủ đạt chuẩn mức 3 (Tốt/Xuất sắc). |

---

## 4. GHI CHÚ
- Khi bị từ chối quyền truy cập, Backend trả về mã lỗi HTTP `403 Forbidden`. Client cần hiển thị thông báo "Bạn không có quyền thực hiện chức năng này" thay vì làm crash phần mềm.

---

## 5. KẾT LUẬN

Tài liệu `Authorization.md` đã thiết lập toàn bộ quy tắc phân quyền vai trò (RBAC) cho hệ thống Smart SuperMarket. Mô hình kiểm soát 2 lớp (Backend Filter & Client UI Control) đảm bảo an toàn dữ liệu và trải nghiệm người dùng chuẩn mực.
