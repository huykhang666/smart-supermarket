# PHÂN HỆ 01: XÁC THỰC, PHÂN QUYỀN & QUẢN LÝ NGƯỜI DÙNG (AUTHENTICATION & USER MANAGEMENT)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Mục tiêu phân hệ 01_Authentication](#21-mục-tiêu-phân-hệ-01_authentication)
   - 2.2. [Danh mục tài liệu trong phân hệ](#22-danh-mục-tài-liệu-trong-phân-hệ)
   - 2.3. [Ma trận phân quyền & Phạm vi truy cập](#23-ma-trận-phân-quyền--phạm-vi-truy-cập)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Phân hệ **01_Authentication** chịu trách nhiệm toàn bộ các bài toán về Đăng ký, Đăng nhập, Cấp phát Token xác thực (JWT), Mã hóa mật khẩu (BCrypt), Phân quyền người dùng (Role-Based Access Control - RBAC) và Quản lý hồ sơ người dùng trong hệ thống **Smart SuperMarket**.

Tài liệu này đóng vai trò là hướng dẫn tổng quan cho phân hệ 01, dựa trên thiết kế chuẩn hóa CSDL tại file `Database_Design_ERD.pdf` và các quy định kiến trúc cốt lõi tại `docs/00_Project/`.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Mục tiêu phân hệ 01_Authentication
- **Bảo mật tuyệt đối**: Bảo vệ thông tin tài khoản, mật khẩu được mã hóa an toàn với thuật toán BCrypt + Salt.
- **Xác thực tập trung (Centralized Auth)**: Cấp phát JWT Bearer Token không lưu trạng thái (Stateless JWT) dùng chung cho cả WinForms Desktop (Admin, Staff) và Web Client (Customer).
- **Chuẩn hóa 3NF CSDL**: Tuân thủ chuẩn thiết kế trong `Database_Design_ERD.pdf` (Tách riêng bảng `[User]` và `Customer` theo quan hệ 1-1 để tránh dữ liệu NULL ở các tài khoản Admin/Staff).
- **Phân quyền chặt chẽ (RBAC)**: Đảm bảo chỉ Admin mới có quyền khởi tạo tài khoản Nhân viên (`Staff`/`Manager`), Staff chỉ truy cập chức năng bán hàng POS và quản lý nhập kho.

---

### 2.2. Danh mục tài liệu trong phân hệ

Phân hệ `01_Authentication` bao gồm 9 tài liệu kỹ thuật chi tiết:

| STT | Tài liệu | File đính kèm | Nội dung chính |
| :---: | :--- | :--- | :--- |
| 1 | **Trang chỉ mục phân hệ** | `README.md` | Tổng quan phân hệ, mục tiêu và ma trận phân quyền. |
| 2 | **Cấu trúc CSDL User & Customer** | `UserEntity.md` | Chi tiết bảng `[User]` và `Customer` theo bản vẽ `Database_Design_ERD.pdf`. |
| 3 | **Cơ chế Đăng nhập & Mã hóa** | `Authentication.md` | Quy trình Đăng nhập, cấp JWT Token, cấu hình Claims và mã hóa BCrypt. |
| 4 | **Phân quyền Role-Based (RBAC)** | `Authorization.md` | Định nghĩa các Role (`1=Admin`, `2=Manager`, `3=Staff`, `4=Customer`) và chính sách phân quyền API. |
| 5 | **Quy tắc Nghiệp vụ Tài khoản** | `BusinessRules.md` | Tập hợp các quy tắc tạo tài khoản, khóa tài khoản, đăng ký OTP và Soft Delete. |
| 6 | **Luồng trải nghiệm Người dùng** | `UserFlow.md` | Mô tả luồng thao tác trên WinForms Desktop và Web Client. |
| 7 | **Sơ đồ Tuần tự Xác thực** | `SequenceDiagram.md` | Sơ đồ Mermaid sequence cho luồng Login, Register và Validate JWT Token. |
| 8 | **Danh sách API Authentication** | `UserAPI.md` | Chi tiết các REST API endpoints (`/api/auth/...`, `/api/admin/users/...`). |
| 9 | **Danh sách Task Triển khai** | `Tasks.md` | Phân công công việc từng bước cho lập trình viên Backend và WinForms Dev. |

---

### 2.3. Ma trận phân quyền & Phạm vi truy cập

| Chức năng / Phân hệ | Admin (1) | Manager (2) | Staff (3) | Customer (4) |
| :--- | :---: | :---: | :---: | :---: |
| Đăng nhập JWT hệ thống | ✅ | ✅ | ✅ | ✅ |
| Đăng ký tài khoản Web (SĐT + OTP) | ❌ | ❌ | ❌ | ✅ |
| Admin khởi tạo tài khoản Nhân viên mới | ✅ | ❌ | ❌ | ❌ |
| Khóa / Mở khóa tài khoản người dùng | ✅ | ❌ | ❌ | ❌ |
| Bán hàng POS quét mã vạch WinForms | ✅ | ✅ | ✅ | ❌ |
| Nhập kho & Quản lý HSD | ✅ | ✅ | ✅ | ❌ |
| Xem Dashboard Thống kê & Báo cáo AI | ✅ | ✅ | ❌ | ❌ |
| Đặt hàng Web & Đổi Voucher điểm thưởng | ❌ | ❌ | ❌ | ✅ |

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Thời hạn hết hạn của JWT Access Token nên đặt là bao lâu cho ứng dụng WinForms POS (tránh việc thu ngân đang bán hàng bị logout đột ngột)? | Ảnh hưởng trải nghiệm người dùng WinForms POS. | Đề xuất: Access Token có hạn 8-12 tiếng cho phiên làm việc ca thu ngân; hoặc triển khai Refresh Token cơ chế gia hạn tự động. |

---

## 4. GHI CHÚ
- Khi tham chiếu thiết kế CSDL, luôn sử dụng file `Database_Design_ERD.pdf` (Updated Version 2.0) làm chuẩn.
- Bảng `[User]` sử dụng từ khóa đóng ngoặc vuông `[User]` trong SQL Server để tránh trùng với từ khóa hệ thống.

---

## 5. KẾT LUẬN

Tài liệu `README.md` định hình toàn bộ phạm vi công việc cho phân hệ `01_Authentication`. Việc tuân thủ tài liệu này giúp đội ngũ phát triển xây dựng hệ thống bảo mật chặt chẽ, đáp ứng đúng chuẩn 3NF CSDL và yêu cầu phân quyền của hệ thống Smart SuperMarket.
