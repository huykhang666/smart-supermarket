# QUY TẮC NGHIỆP VỤ QUẢN LÝ NHÀ CUNG CẤP & CHUỖI CUNG ỨNG (SUPPLIER BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh mục Quy tắc Nghiệp vụ](#21-tổng-quan-danh-mục-quy-tắc-nghiệp-vụ)
   - 2.2. [Chi tiết Quy tắc 1: Tính Duy nhất của Nhà cung cấp (BR-SUPP-01)](#22-chi-tiết-quy-tắc-1-tính-duy-nhất-của-nhà-cung-cấp-br-supp-01)
   - 2.3. [Chi tiết Quy tắc 2: Chuẩn hóa Thông tin Liên hệ (BR-SUPP-02)](#23-chi-tiết-quy-tắc-2-chuẩn-hóa-thông-tin-liên-hệ-br-supp-02)
   - 2.4. [Chi tiết Quy tắc 3: Ràng buộc Liên kết N-N ProductSupplier & Nhà cung cấp Mặc định (BR-SUPP-03)](#24-chi-tiết-quy-tắc-3-ràng-buộc-liên-kết-n-n-productsupplier--nhà-cung-cấp-mặc-định-br-supp-03)
   - 2.5. [Chi tiết Quy tắc 4: Kiểm soát Giá nhập & Điều khoản Cung ứng (BR-SUPP-04)](#25-chi-tiết-quy-tắc-4-kiểm-soát-giá-nhập--điều-khoản-cung-ứng-br-supp-04)
   - 2.6. [Chi tiết Quy tắc 5: Ràng buộc Quản lý & Hủy Liên kết Sản phẩm (BR-SUPP-05)](#26-chi-tiết-quy-tắc-5-ràng-buộc-quản-lý--hủy-liên-kết-sản-phẩm-br-supp-05)
   - 2.7. [Chi tiết Quy tắc 6: Quản lý Trạng thái & Xóa mềm (BR-SUPP-06)](#27-chi-tiết-quy-tắc-6-quản-lý-trạng-thái--xóa-mềm-br-supp-06)
   - 2.8. [Chi tiết Quy tắc 7: Phân quyền Thao tác theo Vai trò (BR-SUPP-07)](#28-chi-tiết-quy-tắc-7-phân-quyền-thao-tác-theo-vai-trò-br-supp-07)
   - 2.9. [Chi tiết Quy tắc 8: Quyền Truy cập Read-Only cho AI Agent (BR-SUPP-08)](#29-chi-tiết-quy-tắc-8-quyền-truy-cập-read-only-cho-ai-agent-br-supp-08)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** định nghĩa tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules) bắt buộc phải tuân thủ cho phân hệ **Quản lý Nhà cung cấp (04_Supplier)** trong hệ thống **Smart SuperMarket**. Các quy tắc này đóng vai trò là điều kiện tiên quyết cho việc xây dựng logic tầng Business Service (`SupplierService.cs`), tầng Validation DTOs và Phân quyền API.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh mục Quy tắc Nghiệp vụ

| Mã quy tắc | Tên quy tắc nghiệp vụ | Phạm vi áp dụng | Mức độ nghiêm ngặt |
| :---: | :--- | :--- | :---: |
| **BR-SUPP-01** | Tính Duy nhất của Nhà cung cấp | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-SUPP-02** | Chuẩn hóa Thông tin Liên hệ | DTO Validation / Service | **Bắt buộc (High)** |
| **BR-SUPP-03** | Ràng buộc N-N & Nhà cung cấp Mặc định | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-SUPP-04** | Kiểm soát Giá nhập & Điều khoản Cung ứng | Backend Service / Validation | **Bắt buộc (High)** |
| **BR-SUPP-05** | Hủy Liên kết & Bảo toàn Dữ liệu Nhập kho | Backend Service / DB FK | **Bắt buộc (High)** |
| **BR-SUPP-06** | Trạng thái Hợp tác & Xóa mềm | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-SUPP-07** | Phân quyền Quản lý theo Vai trò (RBAC) | Controller Authorize Filter | **Bắt buộc (Critical)** |
| **BR-SUPP-08** | Giới hạn Quyền Read-Only cho AI Agent | Service / Controller Auth | **Bắt buộc (Critical)** |

---

### 2.2. Chi tiết Quy tắc 1: Tính Duy nhất của Nhà cung cấp (BR-SUPP-01)

1. **Mô tả**: Tên nhà cung cấp (`SupplierName`) không được phép trùng lặp trên toàn hệ thống.
2. **Quy định chi tiết**:
   - Khi tạo mới: Kiểm tra tên nhà cung cấp (không phân biệt hoa/thường, không tính khoảng trắng thừa). Nếu đã tồn tại $\rightarrow$ Báo lỗi `400 Bad Request` (*"Tên nhà cung cấp này đã tồn tại trên hệ thống"*).
   - Khi cập nhật: Cho phép giữ nguyên tên cũ của chính nhà cung cấp đó, nhưng không được cập nhật trùng với tên của bất kỳ nhà cung cấp nào khác.
   - Nếu cung cấp Email hoặc Số điện thoại, hệ thống cũng kiểm tra tính duy nhất để tránh trùng lặp đối tác kinh doanh.

---

### 2.3. Chi tiết Quy tắc 2: Chuẩn hóa Thông tin Liên hệ (BR-SUPP-02)

1. **Tên nhà cung cấp (`SupplierName`)**:
   - Không được để trống (Not Null / Not Empty). Độ dài từ 3 đến 150 ký tự.
2. **Số điện thoại (`PhoneNumber`)**:
   - Đúng định dạng số điện thoại Việt Nam (độ dài 10-11 chữ số, bắt đầu bằng `0`).
3. **Email (`Email`)**:
   - Đúng định dạng chuẩn RFC 5322 (VD: `supplier@domain.com`). Độ dài tối đa 100 ký tự.
4. **Địa chỉ (`Address`)**:
   - Tối đa 255 ký tự.

---

### 2.4. Chi tiết Quy tắc 3: Ràng buộc Liên kết N-N ProductSupplier & Nhà cung cấp Mặc định (BR-SUPP-03)

1. **Mối quan hệ Many-to-Many**:
   - Một `Product` có thể được gán cho nhiều `Supplier`. Một `Supplier` có thể phân phối nhiều `Product`.
   - Mỗi liên kết giữa 1 Product và 1 Supplier là duy nhất (Dựa trên cặp Khóa chính `(ProductId, SupplierId)`). Nếu gán lại cặp đã tồn tại $\rightarrow$ Báo lỗi `400 Bad Request`.
2. **Nhà cung cấp Mặc định (`IsDefault`)**:
   - Mỗi sản phẩm **chỉ được phép có tối đa 1 Nhà cung cấp mặc định (`IsDefault = true`)**.
   - Khi tạo mới hoặc cập nhật một bản ghi `ProductSupplier` có `IsDefault = true`: Hệ thống tự động chuyển tất cả các `ProductSupplier` khác của sản phẩm đó về `IsDefault = false`.

---

### 2.5. Chi tiết Quy tắc 4: Kiểm soát Giá nhập & Điều khoản Cung ứng (BR-SUPP-04)

1. **Giá nhập hợp đồng (`PurchasePrice`)**:
   - Bắt buộc lớn hơn hoặc bằng 0 (`PurchasePrice >= 0`). Phải đạt độ chính xác đến 2 chữ số thập phân (`DECIMAL(18,2)`).
2. **Thời gian giao hàng (`LeadTime`)**:
   - Số ngày giao hàng dự kiến phải lớn hơn hoặc bằng 0 (`LeadTime >= 0`). Mặc định = 1 ngày.
3. **Số lượng đặt tối thiểu (`MinimumOrderQuantity` - MOQ)**:
   - Phải lớn hơn hoặc bằng 1 (`MinimumOrderQuantity >= 1`). Mặc định = 1.
4. **Điểm đánh giá chất lượng (`Rating`)**:
   - Điểm số nằm trong khoảng từ `1.00` đến `5.00` điểm. Mặc định khi mới tạo là `5.00`.

---

### 2.6. Chi tiết Quy tắc 5: Ràng buộc Quản lý & Hủy Liên kết Sản phẩm (BR-SUPP-05)

1. **Hủy liên kết Sản phẩm - NCC**:
   - Cho phép xóa bản ghi trong `ProductSupplier`.
   - Nếu liên kết bị xóa đang là `IsDefault = true` và sản phẩm đó vẫn còn các NCC khác: Hệ thống phát cảnh báo yêu cầu gán NCC mặc định mới cho sản phẩm.

---

### 2.7. Chi tiết Quy tắc 6: Quản lý Trạng thái & Xóa mềm (BR-SUPP-06)

1. **Định nghĩa trạng thái `Status`**:
   - `1` = **`Active` (Đang hợp tác)**: Cho phép tạo đơn nhập hàng `ImportReceipt` và lập liên kết sản phẩm mới.
   - `2` = **`Inactive` (Tạm ngừng hợp tác)**: Ẩn khỏi danh sách gợi ý nhập hàng, không cho phép tạo phiếu nhập hàng mới từ NCC này.
2. **Nguyên tắc Xóa mềm (Soft Delete)**:
   - **Tuyệt đối không xóa vật lý (Physical Delete)** nhà cung cấp trong bảng `Supplier` nếu nhà cung cấp đó đã phát sinh phiếu nhập hàng `ImportReceipt`.
   - Hành động DELETE từ API sẽ chuyển đổi thành cập nhật `Status = 2 (Inactive)`.

---

### 2.8. Chi tiết Quy tắc 7: Phân quyền Thao tác theo Vai trò (BR-SUPP-07)

| Vai trò (Role) | Xem danh sách / Chi tiết | Tạo / Sửa NCC | Xóa mềm NCC | Thao tác Liên kết `ProductSupplier` |
| :--- | :---: | :---: | :---: | :---: |
| **Admin (Role 1)** | ✅ | ✅ | ✅ | ✅ |
| **Manager (Role 2)** | ✅ | ✅ | ✅ | ✅ |
| **Staff (Role 3)** | ✅ | ✅ | ✅ | ✅ |
| **AI Agent** | ✅ *(Read-Only)* | ❌ | ❌ | ❌ |
| **Customer (Role 4)** | ❌ | ❌ | ❌ | ❌ |

---

### 2.9. Chi tiết Quy tắc 8: Quyền Truy cập Read-Only cho AI Agent (BR-SUPP-08)

1. **Phạm vi truy cập AI Agent**:
   - AI Agent (Gemini Assistant) chỉ được cấp quyền đọc dữ liệu thông tin Nhà cung cấp, danh sách sản phẩm phân phối và lịch sử nhập hàng qua Token xác thực AI.
2. **Ngăn chặn can thiệp dữ liệu**:
   - Bất kỳ yêu cầu ghi dữ liệu (`POST`, `PUT`, `DELETE`, `PATCH`) xuất phát từ Token AI sẽ bị Từ chối truy cập `403 Forbidden`.

---

## 3. GHI CHÚ
- Toàn bộ validation thông tin liên hệ được kiểm tra tự động qua DTO Attributes và Fluent Validation.
- Khi cập nhật `PurchasePrice` trên `ProductSupplier`, hệ thống giữ nguyên giá nhập của các phiếu nhập hàng `ImportReceipt` lịch sử trước đó.

---

## 4. KẾT LUẬN

Tài liệu `BusinessRules.md` đã quy định đầy đủ 8 Quy tắc Nghiệp vụ cho phân hệ Nhà cung cấp, đảm bảo tính nhất quán dữ liệu thương mại, an toàn quản lý chuỗi cung ứng và tuân thủ phân quyền RBAC.
