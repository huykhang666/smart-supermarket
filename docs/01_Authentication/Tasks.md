# DANH SÁCH TASK TRIỂN KHAI PHÂN HỆ AUTHENTICATION (TASKS SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Danh sách Task Tầng Backend (.NET 8 Web API)](#21-danh-sách-task-tầng-backend-net-8-web-api)
   - 2.2. [Danh sách Task Tầng Desktop Application (C# WinForms)](#22-danh-sách-task-tầng-desktop-application-c-winforms)
   - 2.3. [Danh sách Task Tầng Web Client (React + TypeScript)](#23-danh-sách-task-tầng-web-client-react--typescript)
   - 2.4. [Tiêu chí Kiểm thử & Nghiệm thu Phân hệ 01](#24-tiêu-chí-kiểm-thử--nghiệm-thu-phân-hệ-01)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Tasks.md** chi tiết hóa danh sách các tác vụ công việc (Tasks Checklist) cần triển khai cho phân hệ **01_Authentication & User Management**. Tài liệu được chia theo từng tầng phát triển (Backend, Desktop WinForms, Web Client) giúp các lập trình viên theo dõi tiến độ và thực hiện đúng phạm vi công việc được giao.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Danh sách Task Tầng Backend (.NET 8 Web API)

- [ ] **TASK-BE-011**: Khởi tạo Entity `User.cs` và `Customer.cs` trong `Domain/Entities/` theo đúng bản vẽ `Database_Design_ERD.pdf` (Version 2.0).
- [ ] **TASK-BE-012**: Khởi tạo Enums `UserRole.cs` và `UserStatus.cs` trong `Domain/Enums/`.
- [ ] **TASK-BE-013**: Cấu hình Fluent API cho 2 bảng `[User]` và `Customer` trong `Infrastructure/Persistence/Configurations/` và cập nhật `AppDbContext.cs`.
- [ ] **TASK-BE-014**: Cài đặt gói `BCrypt.Net-Next` và viết class `PasswordHasher.cs` trong `Infrastructure/Security/`.
- [ ] **TASK-BE-015**: Cài đặt gói `JwtBearer` và viết class `JwtService.cs` sinh Token chứa Claims (`UserId`, `Username`, `Role`, `BranchId`).
- [ ] **TASK-BE-016**: Cấu hình Đăng ký Dependency Injection trong `Infrastructure/DependencyInjection.cs`.
- [ ] **TASK-BE-017**: Xây dựng DTOs trong `Features/Auth/DTOs/` (`LoginRequest`, `RegisterCustomerRequest`, `AuthResponse`).
- [ ] **TASK-BE-018**: Viết Service `AuthService.cs` xử lý nghiệp vụ Đăng nhập, Đăng ký Customer và Cấp Token.
- [ ] **TASK-BE-019**: Viết Controller `AuthController.cs` cung cấp các API `/api/auth/login`, `/api/auth/register`, `/api/auth/me`.
- [ ] **TASK-BE-020**: Viết Controller `AdminUserController.cs` cung cấp các API quản lý nhân viên `/api/admin/users` (Yêu cầu `[Authorize(Roles = "Admin")]`).

---

### 2.2. Danh sách Task Tầng Desktop Application (C# WinForms)

- [ ] **TASK-DESK-011**: Thiết kế Form Đăng nhập `LoginForm.cs` (gồm ô nhập Username/Phone, Password, Nút Đăng nhập, ErrorProvider báo đỏ).
- [ ] **TASK-DESK-012**: Viết hàm gọi API Đăng nhập trong `Services/ApiClient.cs` bất đồng bộ (`async/await`).
- [ ] **TASK-DESK-013**: Lưu trữ JWT Token vào bộ nhớ ứng dụng sau khi đăng nhập thành công.
- [ ] **TASK-DESK-014**: Thiết kế Form Quản lý Nhân viên `UserManagerForm.cs` cho Admin (gồm `DataGridView` danh sách nhân viên, ô nhập thông tin, nút Thêm, Sửa, Khóa tài khoản).
- [ ] **TASK-DESK-015**: Thực hiện phân quyền UI trên `MainForm`: Tự động ẩn/hiện Menu Admin tùy thuộc vào `Role` của người dùng sau khi đăng nhập.

---

### 2.3. Danh sách Task Tầng Web Client (React + TypeScript)

- [ ] **TASK-WEB-011**: Khởi tạo Type Interfaces `User` và `AuthResponse` trong `src/shared/types/index.ts`.
- [ ] **TASK-WEB-012**: Xây dựng Form Đăng ký Khách hàng (SĐT + OTP giả lập + Mật khẩu).
- [ ] **TASK-WEB-013**: Xây dựng Form Đăng nhập Khách hàng trên Web.
- [ ] **TASK-WEB-014**: Lưu JWT Token vào `localStorage` và tự động gắn Token vào Header của `axiosInstance.ts`.
- [ ] **TASK-WEB-015**: Tạo Component `ProtectedRoute` bảo vệ các trang yêu cầu đăng nhập.

---

### 2.4. Tiêu chí Kiểm thử & Nghiệm thu Phân hệ 01

1. **Kiểm thử Đăng nhập**:
   - Đăng nhập đúng Username/Password $\rightarrow$ Nhận 200 OK + JWT Token $\rightarrow$ Chuyển Form thành công.
   - Nhập sai mật khẩu 5 lần $\rightarrow$ Tài khoản tự chuyển sang trạng thái `Locked` $\rightarrow$ Báo lỗi tài khoản bị khóa.
2. **Kiểm thử Phân quyền**:
   - Nhân viên `Staff` đăng nhập WinForms $\rightarrow$ Không nhìn thấy Menu "Quản lý Nhân viên".
   - Nhân viên `Staff` cố tình gọi API `/api/admin/users` qua Postman $\rightarrow$ Nhận lỗi `403 Forbidden`.
3. **Kiểm thử Tạo Nhân viên**:
   - Admin tạo Nhân viên mới trên WinForms $\rightarrow$ Mật khẩu được mã hóa BCrypt trong SQL Server $\rightarrow$ Nhân viên mới đăng nhập thành công.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần viết Unit Test cho `AuthService.cs` không? | Đảm bảo chất lượng code Backend. | Đề xuất: Viết Unit Test cho 2 hàm `LoginAsync` và `RegisterCustomerAsync` bằng xUnit / Moq nếu có thời gian. |

---

## 4. GHI CHÚ
- Lập trình viên phải đánh dấu `[x]` vào các checkbox Task khi hoàn thành và commit code tương ứng.

---

## 5. KẾT LUẬN

Tài liệu `Tasks.md` đã quy hoạch toàn bộ các công việc chi tiết cần triển khai cho phân hệ 01_Authentication trên cả 3 tầng Backend, Desktop và Web. Việc thực hiện lần lượt theo danh sách Task sẽ giúp đảm bảo tiến độ và chất lượng mã nguồn.
