# THIẾT KẾ CƠ CHẾ XÁC THỰC (AUTHENTICATION SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Cơ chế Xác thực JWT Bearer Token (Stateless Auth)](#21-cơ-chế-xác-thực-jwt-bearer-token-stateless-auth)
   - 2.2. [Cấu trúc Claims trong JWT Token](#22-cấu-trúc-claims-trong-jwt-token)
   - 2.3. [Quy trình Mã hóa Mật khẩu BCrypt + Salt](#23-quy-trình-mã-hóa-mật-khẩu-bcrypt--salt)
   - 2.4. [Quy trình Đăng nhập Hệ thống (`POST /api/auth/login`)](#24-quy-trình-đăng-nhập-hệ-thống-post-apiauthlogin)
   - 2.5. [Quy trình Đăng ký Tài khoản Khách hàng (`POST /api/auth/register`)](#25-quy-trình-đăng-ký-tài-khoản-khách-hàng-post-apiauthregister)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Authentication.md** mô tả thiết kế kỹ thuật cho cơ chế Xác thực (Authentication) của hệ thống **Smart SuperMarket**. Tài liệu định nghĩa phương thức đăng nhập cho 3 vai trò (Admin, Staff, Customer), quy trình cấp phát JWT Bearer Token không lưu trạng thái (Stateless), cấu trúc Claims và giải pháp mã hóa mật khẩu an toàn với BCrypt.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Cơ chế Xác thực JWT Bearer Token (Stateless Auth)
Hệ thống sử dụng tiêu chuẩn **JSON Web Token (JWT)** để xác thực tất cả các request đến Backend .NET 8 Web API:
- **Tính chất Stateless**: Server không lưu session trong RAM hay CSDL, giúp dễ dàng mở rộng và giảm tải server.
- **Dùng chung đa nền tảng**: Cả ứng dụng **WinForms Desktop (Staff/Admin)** và **React Web (Customer)** đều gửi JWT Token ở HTTP Header dạng `Authorization: Bearer <Token>`.
- **Cấu hình thời hạn**: Token có thời gian sống (`ExpiryMinutes`) được cấu hình trong `appsettings.json` qua `JwtOptions`.

---

### 2.2. Cấu trúc Claims trong JWT Token

Khi đăng nhập thành công, Backend sinh mã JWT chứa các thuộc tính Claims tiêu chuẩn:

| Name (Claim Type) | Giá trị Claim | Mục đích sử dụng |
| :--- | :--- | :--- |
| `ClaimTypes.NameIdentifier` | `UserId` (VD: `105`) | Định danh ID duy nhất của người dùng |
| `ClaimTypes.Name` | `Username` (VD: `staff_pos_01`) | Tên đăng nhập người dùng |
| `ClaimTypes.Email` | `Email` (VD: `staff@smartmarket.vn`) | Địa chỉ Email người dùng |
| `ClaimTypes.Role` | `Role` string (VD: `Admin`, `Staff`, `Customer`) | Kiểm tra phân quyền truy cập API |
| `BranchId` | `BranchId` (VD: `1`) | Định danh chi nhánh nhân viên đang làm việc |
| `exp` | Unix Timestamp | Thời điểm Token hết hạn |

---

### 2.3. Quy trình Mã hóa Mật khẩu BCrypt + Salt

Để đảm bảo an toàn tuyệt đối cho thông tin người dùng:
1. **Không bao giờ lưu mật khẩu thô (Plain-text)** trong CSDL.
2. Sử dụng thư viện `BCrypt.Net-Next` để mã hóa mật khẩu với **Salt ngẫu nhiên được tự động chèn vào chuỗi hash**:
   - Khi tạo mới tài khoản (Admin tạo Staff hoặc Customer đăng ký): `PasswordHash = BCrypt.HashPassword(password, workFactor: 11);`
   - Khi đăng nhập: `bool isValid = BCrypt.Verify(inputPassword, storedPasswordHash);`
3. Thuật toán BCrypt có cơ chế làm chậm thời gian tính toán (Work Factor = 11) giúp chống lại các cuộc tấn công quét mật khẩu Brute-force và Rainbow Table.

---

### 2.4. Quy trình Đăng nhập Hệ thống (`POST /api/auth/login`)

1. **Client gửi Request**: Nhập `Username` (hoặc Số điện thoại/Email) và `Password`.
2. **Backend tiếp nhận & Kiểm tra**:
   - Tìm kiếm `User` trong CSDL theo Username/Email/Phone. Nếu không thấy $\rightarrow$ Trả lỗi `401 Unauthorized`.
   - Kiểm tra trạng thái tài khoản: Nếu `Status == UserStatus.Locked (2)` $\rightarrow$ Trả lỗi `401 Unauthorized` ("Tài khoản đã bị khóa").
   - Kiểm tra Mật khẩu: So sánh bằng `PasswordHasher.VerifyPassword()`. Nếu sai $\rightarrow$ Trả lỗi `401 Unauthorized`.
3. **Cấp phát Token**:
   - Tạo các Claims và gọi `JwtService.GenerateToken()`.
   - Trả về đối tượng `AuthResponse` bọc trong `ApiResult<AuthResponse>` chứa: `Token`, `Username`, `FullName`, `Role`, `ExpiresIn`.

---

### 2.5. Quy trình Đăng ký Tài khoản Khách hàng (`POST /api/auth/register`)

1. **Client gửi Request**: Khách hàng nhập `PhoneNumber`, `FullName`, `Email`, `Password`.
2. **Backend Validate**:
   - Kiểm tra `PhoneNumber` và `Email` đã tồn tại trong CSDL chưa. Nếu trùng $\rightarrow$ Trả lỗi `400 Bad Request`.
3. **Tạo dữ liệu 2 bảng (Transaction 3NF)**:
   - Tạo bản ghi `[User]` với `Role = UserRole.Customer (4)`, `Status = Active (1)`, mã hóa mật khẩu với BCrypt.
   - Tạo bản ghi `Customer` tương ứng liên kết qua `UserId`, đặt `LoyaltyPoints = 0`, `MembershipTier = Bronze (1)`.
   - Thực thi trong 1 **Database Transaction** để đảm bảo cả 2 bản ghi cùng tạo thành công.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần bổ sung cơ chế Refresh Token (Lưu trong CSDL) để tự động gia hạn JWT Access Token mà không bắt thu ngân nhập lại mật khẩu khi làm việc ca dài không? | Ảnh hưởng đến trải nghiệm người dùng WinForms POS. | Đề xuất: Đặt thời gian sống của Access Token đủ dài cho 1 ca làm việc (8-12 tiếng) ở bản v1. Bổ sung Refresh Token ở bản v2. |

---

## 4. GHI CHÚ
- Chuỗi bí mật ký Token (`JwtOptions.SecretKey`) bắt buộc phải có độ dài tối thiểu 32 ký tự (256-bit) và lưu an toàn trong `appsettings.json`.
- Khi gọi các API yêu cầu đăng nhập, Client WinForms và Web phải bắt lỗi `401 Unauthorized` để tự động điều hướng người dùng về màn hình Đăng nhập (`LoginForm` hoặc `/login`).

---

## 5. KẾT LUẬN

Tài liệu `Authentication.md` đã định nghĩa chi tiết cơ chế xác thực JWT Bearer Token, thuật toán mã hóa mật khẩu BCrypt và quy trình đăng nhập/đăng ký cho toàn bộ hệ thống Smart SuperMarket.
