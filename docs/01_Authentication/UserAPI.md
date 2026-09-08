# THIẾT KẾ DANH SÁCH REST API AUTHENTICATION & USER MANAGEMENT (USER API SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh sách REST API Endpoints Phân hệ 01](#21-tổng-quan-danh-sách-rest-api-endpoints-phân-hệ-01)
   - 2.2. [Chi tiết API 1: Đăng nhập (`POST /api/auth/login`)](#22-chi-tiết-api-1-đăng-nhập-post-apiauthlogin)
   - 2.3. [Chi tiết API 2: Khách hàng Đăng ký (`POST /api/auth/register`)](#23-chi-tiết-api-2-khách-hàng-đăng-ký-post-apiauthregister)
   - 2.4. [Chi tiết API 3: Lấy thông tin User hiện tại (`GET /api/auth/me`)](#24-chi-tiết-api-3-lấy-thông-tin-user-hiện-tại-get-apiauthme)
   - 2.5. [Chi tiết API 4: Admin Lấy danh sách Nhân viên (`GET /api/admin/users`)](#25-chi-tiết-api-4-admin-lấy-danh-sách-nhân-viên-get-apiadminusers)
   - 2.6. [Chi tiết API 5: Admin Tạo mới Nhân viên (`POST /api/admin/users`)](#26-chi-tiết-api-5-admin-tạo-mới-nhân-viên-post-apiadminusers)
   - 2.7. [Chi tiết API 6: Admin Cập nhật Nhân viên (`PUT /api/admin/users/{id}`)](#27-chi-tiết-api-6-admin-cập-nhật-nhân-viên-put-apiadminusersid)
   - 2.8. [Chi tiết API 7: Admin Khóa mềm Nhân viên (`DELETE /api/admin/users/{id}`)](#28-chi-tiết-api-7-admin-khóa-mềm-nhân-viên-delete-apiadminusersid)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **UserAPI.md** định nghĩa chi tiết hợp đồng giao tiếp (API Contract) cho toàn bộ các Endpoints thuộc phân hệ **01_Authentication & User Management**. Tài liệu quy định URL, HTTP Method, cấu trúc Request Body, Query Parameters, Response Body (`ApiResult<T>`) và các mã lỗi HTTP tương ứng.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh sách REST API Endpoints Phân hệ 01

| HTTP Method | Endpoint URL | Vai trò được gọi | Mục đích sử dụng |
| :---: | :--- | :---: | :--- |
| `POST` | `/api/auth/login` | Public | Đăng nhập hệ thống & lấy JWT Token (Admin, Staff, Customer) |
| `POST` | `/api/auth/register` | Public | Khách hàng tự đăng ký tài khoản mới trên Web |
| `GET` | `/api/auth/me` | Authenticated | Lấy thông tin tài khoản người dùng đang đăng nhập từ Token |
| `GET` | `/api/admin/users` | Admin | Lấy danh sách tài khoản Nhân viên (Hỗ trợ phân trang, lọc) |
| `POST` | `/api/admin/users` | Admin | Admin tạo tài khoản Nhân viên mới trên WinForms Desktop |
| `PUT` | `/api/admin/users/{id}` | Admin | Admin cập nhật thông tin nhân viên (SĐT, Email, Role, BranchId) |
| `DELETE`| `/api/admin/users/{id}` | Admin | Admin khóa mềm tài khoản nhân viên (`Status = Locked`) |

---

### 2.2. Chi tiết API 1: Đăng nhập (`POST /api/auth/login`)

- **Xác thực**: Public (Không yêu cầu Token)
- **Request Body (JSON)**:
```json
{
  "username": "staff_pos_01",
  "password": "Password123!"
}
```
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Đăng nhập thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 105,
    "username": "staff_pos_01",
    "fullName": "Nguyễn Văn Thu Ngân",
    "email": "thungan01@smartmarket.vn",
    "role": "Staff",
    "branchId": 1,
    "expiresIn": 28800
  },
  "errors": null
}
```
- **Response thất bại (401 Unauthorized)**:
```json
{
  "isSuccess": false,
  "message": "Tên đăng nhập hoặc mật khẩu không chính xác",
  "data": null,
  "errors": ["Invalid credentials"]
}
```

---

### 2.3. Chi tiết API 2: Khách hàng Đăng ký (`POST /api/auth/register`)

- **Xác thực**: Public
- **Request Body (JSON)**:
```json
{
  "phoneNumber": "0987654321",
  "fullName": "Trần Thị Khách Hàng",
  "email": "khachhang@gmail.com",
  "password": "CustomerPassword123!",
  "otpCode": "666666"
}
```
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Đăng ký tài khoản thành công",
  "data": {
    "userId": 201,
    "phoneNumber": "0987654321",
    "fullName": "Trần Thị Khách Hàng"
  },
  "errors": null
}
```

---

### 2.4. Chi tiết API 3: Lấy thông tin User hiện tại (`GET /api/auth/me`)

- **Xác thực**: Header `Authorization: Bearer <JWT_TOKEN>`
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Thành công",
  "data": {
    "userId": 105,
    "username": "staff_pos_01",
    "fullName": "Nguyễn Văn Thu Ngân",
    "email": "thungan01@smartmarket.vn",
    "phoneNumber": "0912345678",
    "role": "Staff",
    "status": "Active",
    "branchId": 1
  },
  "errors": null
}
```

---

### 2.5. Chi tiết API 4: Admin Lấy danh sách Nhân viên (`GET /api/admin/users`)

- **Xác thực**: Header Bearer Token (Yêu cầu Role `Admin`)
- **Query Parameters**: `?role=Staff&branchId=1&page=1&pageSize=10`
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách nhân viên thành công",
  "data": [
    {
      "userId": 105,
      "username": "staff_pos_01",
      "fullName": "Nguyễn Văn Thu Ngân",
      "email": "thungan01@smartmarket.vn",
      "phoneNumber": "0912345678",
      "dateOfBirth": "1998-05-15",
      "role": "Staff",
      "status": "Active",
      "branchId": 1,
      "createdAt": "2026-09-01T08:00:00Z"
    }
  ],
  "errors": null
}
```

---

### 2.6. Chi tiết API 5: Admin Tạo mới Nhân viên (`POST /api/admin/users`)

- **Xác thực**: Header Bearer Token (Yêu cầu Role `Admin`)
- **Request Body (JSON)**:
```json
{
  "username": "staff_pos_02",
  "password": "InitialPassword123!",
  "fullName": "Lê Văn Kho",
  "email": "levankho@smartmarket.vn",
  "phoneNumber": "0933445566",
  "dateOfBirth": "2000-10-20",
  "role": "Staff",
  "branchId": 1
}
```
- **Response thành công (201 Created)**: Trả về đối tượng User vừa tạo.

---

### 2.7. Chi tiết API 6: Admin Cập nhật Nhân viên (`PUT /api/admin/users/{id}`)

- **Xác thực**: Header Bearer Token (Yêu cầu Role `Admin`)
- **Request Body (JSON)**: Cập nhật `fullName`, `email`, `phoneNumber`, `role`, `branchId`, `status`.

---

### 2.8. Chi tiết API 7: Admin Khóa mềm Nhân viên (`DELETE /api/admin/users/{id}`)

- **Xác thực**: Header Bearer Token (Yêu cầu Role `Admin`)
- **Mô tả**: Chuyển `UserStatus` từ `Active (1)` sang `Locked (2)`.
- **Response thành công (200 OK)**: "Đã khóa tài khoản nhân viên thành công".

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần API đổi mật khẩu cá nhân (`POST /api/auth/change-password`) cho nhân viên tự đổi mật khẩu trên WinForms không? | Bổ sung API bảo mật cá nhân. | Đề xuất: Thêm API `change-password` trong phiên bản triển khai Auth nâng cao. |

---

## 4. GHI CHÚ
- Tất cả các Endpoint API trong phân hệ 01 phải được kiểm thử qua Swagger UI hoặc Postman trước khi kết nối với WinForms Desktop.

---

## 5. KẾT LUẬN

Tài liệu `UserAPI.md` đã quy định rõ ràng hợp đồng API (URL, Method, Request/Response Payload, HTTP Status) cho phân hệ 01_Authentication. Đây là căn cứ chính xác để Backend Developer và WinForms/Web Developer lập trình giao tiếp.
