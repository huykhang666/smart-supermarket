# LUỒNG THAO TÁC NGƯỜI DÙNG (USER FLOW SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Luồng Đăng nhập & Điều hướng Giao diện trên WinForms Desktop](#21-luồng-đăng-nhập--điều-hướng-giao-diện-trên-winforms-desktop)
   - 2.2. [Luồng Admin Tạo mới & Quản lý Nhân viên trên WinForms Desktop](#22-luồng-admin-tạo-mới--quản-lý-nhân-viên-trên-winforms-desktop)
   - 2.3. [Luồng Khách hàng Đăng ký & Đăng nhập trên React Web App](#23-luồng-khách-hàng-đăng-ký--đăng-nhập-trên-react-web-app)
   - 2.4. [Luồng Xử lý Quên mật khẩu & Reset Mật khẩu](#24-luồng-xử-lý-quên-mật-khẩu--reset-mật-khẩu)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **UserFlow.md** mô tả các bước thao tác người dùng (User Flow) đối với phân hệ Xác thực và Quản lý tài khoản trên cả 2 nền tảng: **WinForms Desktop Application** (cho Admin & Staff) và **React Web Client** (cho Customer).

---

## 2. NỘI DUNG CHÍNH

### 2.1. Luồng Đăng nhập & Điều hướng Giao diện trên WinForms Desktop

```text
[Mở WinForms App] ──► [LoginForm] ──► Nhập Username & Password ──► Bấm [Đăng nhập]
                                                                        │
                 ┌──────────────────────────────────────────────────────┴──────────────────────────────────────────────────────┐
                 ▼ (Thất bại / Sai mật khẩu)                                                                                  ▼ (Thành công)
   [Hiển thị ErrorProvider báo đỏ / MsgBox lỗi]                                                             [Lưu JWT Token vào Memory App]
                 │                                                                                                             │
                 └──────────────────► Thử lại (Sai > 5 lần ──► Khóa tài khoản)                                                 ▼
                                                                                                        [Kiểm tra Role người dùng từ Token]
                                                                                                                       │
                                                                   ┌───────────────────────────────────────────────────┴───────────────────────────────────────────────────┐
                                                                   ▼ (Role == Staff / 3)                                                                                   ▼ (Role == Admin / 1)
                                                     [Mở MainForm - Ẩn bớt Menu Admin]                                                                          [Mở MainForm - Hiển thị Full Menu]
                                                     [Chỉ cho phép vào PosForm & Nhập kho]                                                                      [Cho phép vào Quản lý Nhân sự, Dashboard, AI]
```

---

### 2.2. Luồng Admin Tạo mới & Quản lý Nhân viên trên WinForms Desktop

1. Admin đăng nhập thành công vào WinForms Desktop App.
2. Tại Menu chính, chọn **Quản lý Hệ thống** $\rightarrow$ **Quản lý Nhân viên**.
3. Hệ thống mở Form `UserManagerForm`, gọi API `GET /api/admin/users` hiển thị danh sách nhân viên lên `DataGridView`.
4. **Thêm Nhân viên mới**:
   - Admin bấm nút **[Thêm nhân viên mới]**.
   - Nhập thông tin: Username, Mật khẩu khởi tạo, Họ tên, Email, SĐT, Ngày sinh, Chọn Chức vụ (`Staff` hoặc `Manager`), Chọn Chi nhánh làm việc.
   - Bấm **[Lưu]** $\rightarrow$ WinForms gọi API `POST /api/admin/users`.
   - Backend validate, mã hóa mật khẩu BCrypt, lưu CSDL $\rightarrow$ Trả kết quả thành công $\rightarrow$ Form tự reload danh sách.
5. **Khóa / Khóa mềm Nhân viên**:
   - Admin chọn nhân viên trên `DataGridView` $\rightarrow$ Bấm nút **[Khóa tài khoản]**.
   - WinForms mở Dialog xác nhận "Bạn có chắc chắn muốn khóa nhân viên này?".
   - Chọn `Yes` $\rightarrow$ Gọi API `DELETE /api/admin/users/{id}` (Backend chuyển `Status = Locked`).

---

### 2.3. Luồng Khách hàng Đăng ký & Đăng nhập trên React Web App

1. Khách hàng truy cập Web App $\rightarrow$ Bấm nút **[Đăng ký thành viên]**.
2. Nhập Số điện thoại, Họ tên, Email, Mật khẩu.
3. Bấm **[Nhận mã OTP]** $\rightarrow$ Nhập mã OTP xác thực (Ví dụ: `666666`).
4. Web gọi API `POST /api/auth/register` $\rightarrow$ Backend tạo tài khoản 2 bảng `User` & `Customer` $\rightarrow$ Trả về thông báo đăng ký thành công.
5. Khách hàng bấm **[Đăng nhập]** $\rightarrow$ Nhập Số điện thoại & Mật khẩu.
6. Web gọi API `POST /api/auth/login` $\rightarrow$ Nhận JWT Token $\rightarrow$ Lưu Token vào `localStorage` $\rightarrow$ Tự động chuyển hướng về Trang chủ, hiển thị số điểm tích lũy trên Header.

---

### 2.4. Luồng Xử lý Quên mật khẩu & Reset Mật khẩu

1. Tại giao diện Đăng nhập, bấm link **"Quên mật khẩu?"**.
2. Nhập Email/Số điện thoại đăng ký $\rightarrow$ Bấm **[Gửi yêu cầu]**.
3. Backend tạo mã Token Reset tạm thời và gửi qua Email SMTP cho người dùng.
4. Người dùng mở Email bấm vào link reset $\rightarrow$ Nhập mật khẩu mới $\rightarrow$ Backend mã hóa BCrypt và cập nhật CSDL.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Khi Admin tạo tài khoản cho Nhân viên mới, mật khẩu có được gửi tự động qua Email của nhân viên đó không? | Ảnh hưởng đến tích hợp Email SMTP trong `01_Authentication`. | Đề xuất: Admin đặt mật khẩu mặc định (ví dụ: `123456@Staff`), nhân viên đổi mật khẩu ở lần đăng nhập đầu tiên. |

---

## 4. GHI CHÚ
- Giao diện WinForms phải mượt mà, khi gọi API Đăng nhập hoặc Tạo nhân viên phải bật con trỏ `Cursor = Cursors.WaitCursor` hoặc ProgressBar để tránh đơ app.

---

## 5. KẾT LUẬN

Tài liệu `UserFlow.md` đã làm rõ các bước thao tác thực tế cho cả 3 vai trò trên cả 2 nền tảng Desktop và Web Client.
