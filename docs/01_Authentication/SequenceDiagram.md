# SƠ ĐỒ TUẦN TỰ XÁC THỰC & PHÂN QUYỀN (SEQUENCE DIAGRAM SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Sơ đồ Tuần tự Luồng Đăng nhập Hệ thống (Login Sequence Diagram)](#21-sơ-đồ-tuần-tự-luồng-đăng-nhập-hệ-thống-login-sequence-diagram)
   - 2.2. [Sơ đồ Tuần tự Admin Tạo mới Tài khoản Nhân viên (Staff Creation Sequence Diagram)](#22-sơ-đồ-tuần-tự-admin-tạo-mới-tài-khoản-nhân-viên-staff-creation-sequence-diagram)
   - 2.3. [Sơ đồ Tuần tự Khách hàng Đăng ký Tài khoản Web (Customer Registration Sequence Diagram)](#23-sơ-đồ-tuần-tự-khách-hàng-đăng-ký-tài-khoản-web-customer-registration-sequence-diagram)
   - 2.4. [Sơ đồ Tuần tự Validate JWT Token tại Backend Middleware](#24-sơ-đồ-tuần-tự-validate-jwt-token-tại-backend-middleware)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **SequenceDiagram.md** mô tả bằng sơ đồ tuần tự (Sequence Diagram) các tương tác qua lại theo thời gian giữa Người dùng, Client App (WinForms Desktop / React Web), Backend Controllers, Services, Security Helpers và CSDL SQL Server đối với phân hệ 01_Authentication.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Sơ đồ Tuần tự Luồng Đăng nhập Hệ thống (Login Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor User as Người dùng (Staff / Admin / Customer)
    participant Client as Client App (WinForms / Web)
    participant AuthCtrl as AuthController
    participant AuthService as AuthService
    participant Hasher as PasswordHasher (BCrypt)
    participant JwtService as JwtService
    participant DB as SQL Server Database

    User->>Client: Nhập Username/Phone & Password, bấm Đăng nhập
    Client->>AuthCtrl: POST /api/auth/login (LoginRequest)
    AuthCtrl->>AuthService: LoginAsync(request)
    AuthService->>DB: GetByUsernameOrPhoneAsync(identifier)
    DB-->>AuthService: Trả về đối tượng User (kèm PasswordHash, Status, Role)
    
    alt User không tồn tại
        AuthService-->>AuthCtrl: Ném NotFoundException ("Tài khoản không tồn tại")
        AuthCtrl-->>Client: Trả về 401 Unauthorized (ApiResult.Failure)
        Client-->>User: Hiển thị báo lỗi trên UI
    else User bị khóa (Status == Locked)
        AuthService-->>AuthCtrl: Ném UnauthorizedException ("Tài khoản bị khóa")
        AuthCtrl-->>Client: Trả về 401 Unauthorized (ApiResult.Failure)
        Client-->>User: Hiển thị thông báo "Tài khoản của bạn đã bị khóa"
    else User hợp lệ
        AuthService->>Hasher: VerifyPassword(inputPassword, storedHash)
        alt Mật khẩu không đúng
            Hasher-->>AuthService: Return false
            AuthService-->>AuthCtrl: Ném UnauthorizedException ("Sai mật khẩu")
            AuthCtrl-->>Client: Trả về 401 Unauthorized
            Client-->>User: Hiển thị báo sai mật khẩu
        else Mật khẩu khớp 100%
            Hasher-->>AuthService: Return true
            AuthService->>JwtService: GenerateToken(UserId, Username, Role, BranchId)
            JwtService-->>AuthService: Trả chuỗi JWT Bearer Token
            AuthService-->>AuthCtrl: Trả đối tượng AuthResponse
            AuthCtrl-->>Client: Trả về 200 OK (ApiResult.Success)
            Client->>Client: Lưu JWT Token vào Storage / App Memory
            Client-->>User: Điều hướng vào Màn hình chính theo Role
        end
    end
```

---

### 2.2. Sơ đồ Tuần tự Admin Tạo mới Tài khoản Nhân viên (Staff Creation Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin Manager
    participant Form as WinForms UserManagerForm
    participant ApiClient as ApiClient (HttpClient)
    participant UserCtrl as AdminUserController
    participant UserService as UserService
    participant Hasher as PasswordHasher (BCrypt)
    participant DB as SQL Server Database

    Admin->>Form: Nhập thông tin Staff mới (Username, Password, Name, Role, BranchId)
    Admin->>Form: Bấm nút [Tạo tài khoản nhân viên]
    Form->>ApiClient: PostAsync("/api/admin/users", requestData)
    ApiClient->>UserCtrl: POST /api/admin/users (Header Authorization: Bearer <AdminJWT>)
    Note over UserCtrl: Middleware xác thực Token & kiểm tra Role == Admin
    UserCtrl->>UserService: CreateStaffAsync(request)
    UserService->>DB: CheckExistUsernameOrEmail(username, email)
    alt Trùng Username/Email
        DB-->>UserService: Trả về trùng lặp
        UserService-->>UserCtrl: Ném BadRequestException ("Email/Username đã tồn tại")
        UserCtrl-->>ApiClient: Trả về 400 Bad Request
        ApiClient-->>Form: Trả về lỗi
        Form-->>Admin: Báo đỏ ErrorProvider tại ô nhập liệu
    else Hợp lệ
        UserService->>Hasher: HashPassword(rawPassword)
        Hasher-->>UserService: Trả về chuỗi hashedPassword (BCrypt)
        UserService->>DB: AddUserAsync(newUser)
        DB-->>UserService: Lưu CSDL thành công
        UserService-->>UserCtrl: Trả về UserDto
        UserCtrl-->>ApiClient: Trả về 201 Created (ApiResult.Success)
        ApiClient-->>Form: Trả về kết quả thành công
        Form-->>Admin: Hiển thị thông báo thành công & Reload DataGridView
    end
```

---

### 2.3. Sơ đồ Tuần tự Khách hàng Đăng ký Tài khoản Web (Customer Registration Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor Cust as Khách hàng (Customer)
    participant Web as React Web Client
    participant AuthCtrl as AuthController
    participant AuthService as AuthService
    participant Hasher as PasswordHasher
    participant DB as SQL Server Database

    Cust->>Web: Nhập SĐT, Họ tên, Email, Mật khẩu & Mã OTP
    Cust->>Web: Bấm [Hoàn tất Đăng ký]
    Web->>AuthCtrl: POST /api/auth/register (RegisterCustomerRequest)
    AuthCtrl->>AuthService: RegisterCustomerAsync(request)
    AuthService->>DB: CheckExistPhoneOrEmail(phone, email)
    alt Số điện thoại hoặc Email đã được sử dụng
        DB-->>AuthService: Đã tồn tại
        AuthService-->>AuthCtrl: Ném BadRequestException
        AuthCtrl-->>Web: Trả về 400 Bad Request
        Web-->>Cust: Hiển thị thông báo "Số điện thoại đã được đăng ký"
    else Hợp lệ
        AuthService->>Hasher: HashPassword(password)
        Hasher-->>AuthService: Trả hashedPassword
        Note over AuthService, DB: Mở DB Transaction
        AuthService->>DB: Insert table [User] (Role=Customer, Status=Active)
        AuthService->>DB: Insert table Customer (LoyaltyPoints=0, Tier=Bronze)
        Note over AuthService, DB: Commit Transaction
        DB-->>AuthService: Lưu 2 bảng thành công
        AuthService-->>AuthCtrl: Trả kết quả thành công
        AuthCtrl-->>Web: Trả về 200 OK (ApiResult.Success)
        Web-->>Cust: Thông báo Đăng ký thành công & Chuyển sang Form Login
    end
```

---

### 2.4. Sơ đồ Tuần tự Validate JWT Token tại Backend Middleware

```mermaid
sequenceDiagram
    autonumber
    actor Client as WinForms POS / React Web
    participant JwtMiddleware as JwtMiddleware
    participant TokenHandler as JwtSecurityTokenHandler
    participant Controller as Feature API Controller

    Client->>JwtMiddleware: HTTP Request + Header [Authorization: Bearer <Token>]
    JwtMiddleware->>JwtMiddleware: Kiểm tra Header Authorization
    alt Không có Header Authorization
        JwtMiddleware-->>Client: Trả về 401 Unauthorized (Thiếu Token)
    else Có Header Bearer Token
        JwtMiddleware->>TokenHandler: ValidateToken(token, JwtOptions.SecretKey)
        alt Token bị hết hạn (Expired) hoặc Sai Chữ ký Signature
            TokenHandler-->>JwtMiddleware: Validation Failed
            JwtMiddleware-->>Client: Trả về 401 Unauthorized (Token hết hạn)
        else Token hợp lệ 100%
            TokenHandler-->>JwtMiddleware: Trả về ClaimsPrincipal (UserId, Role...)
            JwtMiddleware->>JwtMiddleware: Gán ClaimsContext vào HttpContext.User
            JwtMiddleware->>Controller: Chuyển tiếp Request vào Controller Action
            Controller-->>Client: Trả về dữ liệu kết quả
        end
    end
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có nên bổ sung Blacklist Token trong Redis/RAM Cache để lập tức hủy hiệu lực của JWT Token khi Admin bấm "Khóa tài khoản" nhân viên hay không? | Ảnh hưởng tốc độ phản ứng ngắt phiên làm việc. | Ở bản v1, khi Token bị ngắt ở lần gọi API tiếp theo nếu UserStatus bị đổi sang Locked thì API tự từ chối. |

---

## 4. GHI CHÚ
- Các sơ đồ tuần tự trên mô tả đúng luồng xử lý thực tế sẽ được lập trình trong C# Backend API và Client.

---

## 5. KẾT LUẬN

Tài liệu `SequenceDiagram.md` đã trực quan hóa chi tiết 4 sơ đồ tương tác tuần tự cốt lõi của phân hệ Authentication. Đây là tài liệu hướng dẫn kỹ thuật quan trọng giúp lập trình viên hình dung chính xác thứ tự gọi hàm và xử lý luồng dữ liệu.
