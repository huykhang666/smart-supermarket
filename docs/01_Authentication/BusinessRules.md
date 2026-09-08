# QUY TẮC NGHIỆP VỤ XÁC THỰC & NGƯỜI DÙNG (BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Quy tắc Nghiệp vụ Quản lý Tài khoản Người dùng](#21-quy-tắc-nghiệp-vụ-quản-lý-tài-khoản-người-dùng)
   - 2.2. [Quy tắc Khởi tạo Tài khoản Nhân viên (Admin-only Staff Creation)](#22-quy-tắc-khởi-tạo-tài-khoản-nhân-viên-admin-only-staff-creation)
   - 2.3. [Quy tắc Đăng ký Khách hàng (Customer Registration & Phone OTP)](#23-quy-tắc-đăng-ký-khách-hàng-customer-registration--phone-otp)
   - 2.4. [Quy tắc Mã hóa & Bảo mật Mật khẩu](#24-quy-tắc-mã-hóa--bảo-mật-mật-khẩu)
   - 2.5. [Quy tắc Khóa tài khoản & Xóa mềm (Account Lockout & Soft Delete)](#25-quy-tắc-khóa-tài-khoản--xóa-mềm-account-lockout--soft-delete)
   - 2.6. [Quy tắc Tích điểm & Xếp hạng Thành viên Customer](#26-quy-tắc-tích-điểm--xếp-hạng-thành-viên-customer)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules - BR) liên quan đến quản lý tài khoản, đăng ký, đăng nhập, phân quyền, mã hóa và bảo vệ dữ liệu người dùng trong hệ thống **Smart SuperMarket**. 

Tất cả các lập trình viên Backend, WinForms Desktop và React Web phải tuân thủ chính xác các quy tắc này khi viết mã nguồn và xây dựng luồng xử lý.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Quy tắc Nghiệp vụ Quản lý Tài khoản Người dùng

- **BR-AUTH-01 (Định danh duy nhất)**: `Username` và `Email` là duy nhất trong toàn hệ thống. Không cho phép 2 tài khoản trùng Username hoặc Email.
- **BR-AUTH-02 (Số điện thoại duy nhất)**: `PhoneNumber` của Khách hàng (`Customer`) là duy nhất, được sử dụng để tra cứu điểm tích lũy tại quầy POS WinForms.
- **BR-AUTH-03 (Phân tách vai trò)**: Mỗi tài khoản người dùng bắt buộc phải được gán đúng 1 vai trò (`Role` trong `UserRole`: `1=Admin`, `2=Manager`, `3=Staff`, `4=Customer`).

---

### 2.2. Quy tắc Khởi tạo Tài khoản Nhân viên (Admin-only Staff Creation)

- **BR-STAFF-01 (Cấm tự đăng ký)**: Tài khoản Nhân viên (`Staff`/`Manager`) **TUYỆT ĐỐI KHÔNG ĐƯỢC TỰ ĐĂNG KÝ** tự do trên Web.
- **BR-STAFF-02 (Quyền khởi tạo thuộc về Admin)**: Chỉ có tài khoản có `Role = Admin (1)` mới có quyền gọi API `POST /api/admin/users` để tạo tài khoản nhân viên mới trên ứng dụng WinForms Desktop.
- **BR-STAFF-03 (Thông tin bắt buộc của Nhân viên)**: Khi Admin cấp tài khoản cho Nhân viên, các thông tin sau là bắt buộc: `Username`, `Password`, `FullName`, `Email`, `PhoneNumber`, `DateOfBirth`, `Role` (Staff/Manager) và `BranchId` (Chi nhánh làm việc).

---

### 2.3. Quy tắc Đăng ký Khách hàng (Customer Registration & Phone OTP)

- **BR-CUST-01 (Đăng ký tự do trên Web)**: Khách hàng được tự do đăng ký tài khoản trên Web Client React TS bằng Số điện thoại và Mật khẩu.
- **BR-CUST-02 (Xác thực Số điện thoại OTP)**: Quá trình đăng ký yêu cầu nhập đúng mã OTP gửi về số điện thoại (Ở phiên bản v1 thử nghiệm, sử dụng mã OTP giả lập cố định `666666`).
- **BR-CUST-03 (Tự động khởi tạo Hồ sơ Customer 3NF)**: Khi đăng ký tài khoản Khách hàng thành công, hệ thống tự động sinh 1 bản ghi `[User]` (`Role = 4`) và 1 bản ghi `Customer` liên kết 1-1 với `LoyaltyPoints = 0` và `MembershipTier = 1 (Bronze)`.

---

### 2.4. Quy tắc Mã hóa & Bảo mật Mật khẩu

- **BR-SEC-01 (Mã hóa BCrypt)**: Mật khẩu người dùng khi lưu vào CSDL phải được mã hóa bằng thuật toán `BCrypt.Net-Next` với Salt ngẫu nhiên (Work Factor = 11).
- **BR-SEC-02 (Độ mạnh mật khẩu)**: Mật khẩu khởi tạo phải có độ dài tối thiểu 6 ký tự.
- **BR-SEC-03 (Bảo mật DTO)**: Chuỗi `PasswordHash` chỉ tồn tại trong CSDL và Entity Backend, tuyệt đối **KHÔNG BAO GIỜ ĐƯỢC TRẢ VỀ** trong các đối tượng DTOs/JSON cho Client.

---

### 2.5. Quy tắc Khóa tài khoản & Xóa mềm (Account Lockout & Soft Delete)

- **BR-LOCK-01 (Khóa tài khoản tự động)**: Nếu người dùng đăng nhập sai mật khẩu liên tiếp **5 lần**, hệ thống tự động chuyển trạng thái tài khoản sang `Status = UserStatus.Locked (2)` để chống tấn công dò mật khẩu.
- **BR-LOCK-02 (Từ chối xác thực tài khoản bị khóa)**: Tài khoản có `Status == Locked` khi gọi API Đăng nhập sẽ bị từ chối với thông báo "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin".
- **BR-DEL-01 (Soft Delete - Xóa mềm)**: Khi Admin bấm xóa một nhân viên trên WinForms Desktop, hệ thống **KHÔNG XÓA VĨNH VIỄN** hàng trong CSDL mà chỉ cập nhật `Status = Locked (2)`. Điều này bắt buộc nhằm bảo toàn toàn vẹn dữ liệu cho lịch sử hóa đơn `Order` do nhân viên đó đã lập.

---

### 2.6. Quy tắc Tích điểm & Xếp hạng Thành viên Customer

- **BR-TIER-01 (Quy tắc Tích điểm)**: Khách hàng mua hàng tại quầy POS hoặc Web được tích 1 điểm (`LoyaltyPoint`) cho mỗi **10.000 VNĐ** thanh toán thực tế.
- **BR-TIER-02 (Quy tắc Xếp hạng Thành viên)**: Hạng thành viên (`MembershipTier`) tự động nâng cấp dựa trên tổng điểm tích lũy:
  - `1 = Bronze` (Đồng): 0 – 499 điểm.
  - `2 = Silver` (Bạc): 500 – 1.999 điểm.
  - `3 = Gold` (Vàng): 2.000 – 4.999 điểm.
  - `4 = Diamond` (Kim cương): $\ge 5.000$ điểm.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Khi tài khoản bị khóa do nhập sai 5 lần, Admin có thể bấm nút "Un-lock" mở lại tài khoản trên WinForms hay không? | Ảnh hưởng giao diện Quản lý Nhân viên WinForms. | Có. Thiết kế nút "Mở khóa tài khoản" trên Form Quản lý Nhân viên của Admin. |

---

## 4. GHI CHÚ
- Các quy tắc nghiệp vụ trên phải được kiểm tra tự động (Unit Test / Validation) tại tầng Service Backend trước khi thực thi lệnh lưu xuống CSDL.

---

## 5. KẾT LUẬN

Tài liệu `BusinessRules.md` đã làm rõ toàn bộ các quy tắc nghiệp vụ quan trọng đối với tài khoản và phân quyền. Việc tuân thủ nghiêm ngặt các quy tắc BR-AUTH, BR-STAFF, BR-SEC và BR-DEL sẽ giúp hệ thống vận hành an toàn và đúng thực tế kinh doanh.
