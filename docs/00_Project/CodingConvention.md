# QUY CHUẨN LẬP TRÌNH (CODING CONVENTION) SMART SUPERMARKET

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Quy chuẩn Đặt tên mã nguồn C# (.NET)](#21-quy-chuẩn-đặt-tên-mã-nguồn-c-net)
   - 2.2. [Quy chuẩn Đặt tên mã nguồn TypeScript & React](#22-quy-chuẩn-đặt-tên-mã-nguồn-typescript--react)
   - 2.3. [Quy chuẩn Thiết kế RESTful API](#23-quy-chuẩn-thiết-kế-restful-api)
   - 2.4. [Chuẩn định dạng API Response (`ApiResult<T>`)](#24-chuẩn-định-dạng-api-response-apiresultt)
   - 2.5. [Quy định Xử lý Ngoại lệ & Validation](#25-quy-định-xử-lý-ngoại-lệ--validation)
   - 2.6. [Quy chuẩn Quản lý Mã nguồn & Git Commit](#26-quy-chuẩn-quản-lý-mã-nguồn--git-commit)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **CodingConvention.md** quy định các tiêu chuẩn lập trình, quy tắc đặt tên, định dạng dữ liệu API, cơ chế xử lý lỗi và quy trình quản lý phiên bản Git cho dự án **Smart SuperMarket**. 

Mục tiêu của tài liệu là đảm bảo mã nguồn của toàn bộ dự án đạt độ đồng nhất 100%, dễ đọc, dễ kiểm thử và tuân thủ các chuẩn mực công nghiệp hàng đầu cho cả C# (.NET 8/WinForms) và TypeScript (React).

---

## 2. NỘI DUNG CHÍNH

### 2.1. Quy chuẩn Đặt tên mã nguồn C# (.NET)

Dự án tuân thủ nghiêm ngặt **Microsoft C# Coding Conventions**:

| Thành phần C# | Quy chuẩn đặt tên | Ví dụ |
| :--- | :--- | :--- |
| **Namespace** | `PascalCase` dạng chuỗi | `SmartSupermarket.Backend.Features.Products` |
| **Class / Struct / Record** | `PascalCase` (Danh từ) | `Product`, `OrderService`, `JwtTokenService` |
| **Interface** | `PascalCase` bắt đầu chữ `I` | `IProductService`, `IDateTimeProvider` |
| **Method (Hàm)** | `PascalCase` (Động từ) | `GetByBarcodeAsync()`, `CreateOrderAsync()` |
| **Async Method** | `PascalCase` kết thúc bằng `Async` | `LoginAsync()`, `GenerateReportAsync()` |
| **Property** | `PascalCase` | `ProductName`, `LoyaltyPoints`, `Price` |
| **Field riêng tư (Private Field)** | `_camelCase` (Có dấu gạch dưới) | `_dbContext`, `_productService`, `_jwtOptions` |
| **Biến cục bộ (Local Variable)** | `camelCase` | `totalAmount`, `existingUser`, `barcodeInput` |
| **Hằng số (Constant)** | `PascalCase` | `DefaultMinStockLevel`, `MaxLoginAttempts` |
| **Enum Type & Member** | `PascalCase` | `UserRole.Admin`, `OrderStatus.Completed` |

---

### 2.2. Quy chuẩn Đặt tên mã nguồn TypeScript & React

| Thành phần TypeScript | Quy chuẩn đặt tên | Ví dụ |
| :--- | :--- | :--- |
| **React Component / Page** | `PascalCase` (.tsx) | `HomePage.tsx`, `Navbar.tsx`, `CartSummary.tsx` |
| **Custom Hook** | `camelCase` bắt đầu bằng `use` | `useAuth()`, `useCart()`, `useProducts()` |
| **Type / Interface** | `PascalCase` | `Product`, `CartItem`, `User` |
| **Function / Helper** | `camelCase` (.ts) | `formatCurrency()`, `calculateTotal()` |
| **State & Local Variable** | `camelCase` | `cartItems`, `isLoading`, `errorMessage` |
| **File Cấu hình** | `kebab-case` hoặc `camelCase` | `api.config.ts`, `tailwind.config.js` |

---

### 2.3. Quy chuẩn Thiết kế RESTful API

Các REST API được xây dựng trên tầng Backend .NET 8 phải tuân thủ các quy tắc sau:

1. **Cấu trúc URL Endpoint**:
   - Sử dụng chữ thường `kebab-case` hoặc danh từ số nhiều `plural`.
   - Không chứa động từ trong URL.
   - Ví dụ chuẩn:
     - `GET /api/products` (Lấy danh sách sản phẩm)
     - `GET /api/products/barcode/{barcode}` (Tra cứu theo mã vạch)
     - `POST /api/orders` (Tạo hóa đơn mới)
2. **Sử dụng đúng HTTP Verbs**:
   - `GET`: Đọc dữ liệu (Không làm thay đổi trạng thái hệ thống).
   - `POST`: Tạo mới tài nguyên (Tạo sản phẩm, Tạo đơn hàng, Đăng nhập).
   - `PUT`: Cập nhật toàn bộ thông tin tài nguyên.
   - `DELETE`: Xóa mềm hoặc xóa vĩnh viễn tài nguyên.
3. **Mã trạng thái HTTP (HTTP Status Codes)**:
   - `200 OK`: Thao tác xử lý thành công.
   - `201 Created`: Tạo mới tài nguyên thành công.
   - `400 Bad Request`: Dữ liệu đầu vào sai định dạng hoặc vi phạm validation.
   - `401 Unauthorized`: Chưa đăng nhập hoặc Token JWT không hợp lệ.
   - `403 Forbidden`: Đã đăng nhập nhưng không có quyền (Role) truy cập.
   - `404 Not Found`: Không tìm thấy tài nguyên yêu cầu.
   - `500 Internal Server Error`: Lỗi hệ thống chưa được xử lý.

---

### 2.4. Chuẩn định dạng API Response (`ApiResult<T>`)

Mọi Endpoint API đều phải trả về một cấu trúc JSON nhất quán bọc trong class `ApiResult<T>`:

```text
Cấu trúc JSON phẳng trả về cho Client:

{
  "isSuccess": true,
  "message": "Thao tác thực hiện thành công",
  "data": { ... },
  "errors": null
}
```

- **Khi thành công (`isSuccess = true`)**: `data` chứa payload dữ liệu trả về, `errors = null`.
- **Khi thất bại (`isSuccess = false`)**: `data = null`, `errors` chứa danh sách chi tiết các thông báo lỗi.

---

### 2.5. Quy định Xử lý Ngoại lệ & Validation

1. **Validation dữ liệu đầu vào**:
   - **Backend**: Validate DTOs bằng Data Annotations (`[Required]`, `[StringLength]`, `[Range]`) hoặc FluentValidation trước khi vào Service.
   - **WinForms Desktop**: Validate real-time tại các ô nhập liệu bằng `ErrorProvider` (Báo đỏ trực quan, không để ứng dụng bị crash).
2. **Xử lý Ngoại lệ (Exception Handling)**:
   - Không nuốt lỗi (Silent catch) bằng câu lệnh `catch (Exception) {}` rỗng.
   - Tầng Backend sử dụng **Global Exception Handling Middleware** để bắt các lỗi chưa được lường trước và trả về JSON `500 Internal Server Error` với thông báo thân thiện cho người dùng.
   - Luôn log chi tiết lỗi kèm StackTrace vào file log hệ thống.

---

### 2.6. Quy chuẩn Quản lý Mã nguồn & Git Commit

Dự án áp dụng quy chuẩn **Conventional Commits** cho tất cả các thông điệp Git commit:

`Cấu trúc Commit: <type>: <description>`

* **Các loại `<type>` được chấp nhận**:
  - `feat`: Thêm một tính năng mới (Ví dụ: `feat: add barcode scanner handler in POS form`).
  - `fix`: Sửa lỗi bug (Ví dụ: `fix: resolve total amount calculation error in checkout`).
  - `docs`: Cập nhật tài liệu (Ví dụ: `docs: update Architecture.md for JWT flow`).
  - `refactor`: Tái cấu trúc code nhưng không thay đổi chức năng (Ví dụ: `refactor: clean up User entity properties`).
  - `style`: Sửa định dạng UI/CSS, khoảng trắng (Ví dụ: `style: adjust WinForms POS layout buttons`).
  - `chore`: Cập nhật cấu hình build, gói NuGet/npm (Ví dụ: `chore: add QuestPDF package`).

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Hệ thống Logging trên Backend sẽ dùng thư viện mặc định `ILogger` hay tích hợp thêm `Serilog` để ghi log ra file theo ngày (Log Rotation)? | Ảnh hưởng cấu hình logging trong Backend. | Đề xuất: Sử dụng `ILogger` mặc định ở bản v1; bổ sung `Serilog` ghi log ra file khi hoàn thiện hạ tầng. |

---

## 4. GHI CHÚ
- Khi viết mã C#, luôn bật tính năng **Nullable Reference Types** (`<Nullable>enable</Nullable>`) để phát hiện sớm các nguy cơ gây lỗi `NullReferenceException`.
- Tất cả các thao tác I/O (Đọc/Ghi CSDL, Gọi API, Đọc/Ghi File PDF) bắt buộc phải sử dụng từ khóa `async` và `await`.

---

## 5. KẾT LUẬN

Tài liệu `CodingConvention.md` đã thiết lập toàn bộ quy chuẩn lập trình đồng bộ cho dự án Smart SuperMarket. Việc tuân thủ nghiêm ngặt các quy định về đặt tên, thiết kế RESTful API, bọc dữ liệu `ApiResult<T>`, xử lý lỗi và commit Git sẽ giúp nhóm 5 người làm việc ăn ý, đảm bảo chất lượng phần mềm cao nhất.
