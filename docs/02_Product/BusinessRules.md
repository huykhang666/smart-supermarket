# QUY TẮC NGHIỆP VỤ QUẢN LÝ SẢN PHẨM (PRODUCT BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh mục Quy tắc Nghiệp vụ (Business Rules Summary)](#21-tổng-quan-danh-mục-quy-tắc-nghiệp-vụ-business-rules-summary)
   - 2.2. [Chi tiết Quy tắc 1: Mã vạch Duy nhất (BR-PROD-01: Barcode Uniqueness Rule)](#22-chi-tiết-quy-tắc-1-mã-vạch-duy-nhất-br-prod-01-barcode-uniqueness-rule)
   - 2.3. [Chi tiết Quy tắc 2: Kiểm soát Giá bán & Giá vốn (BR-PROD-02: Price Validation & Cost Rule)](#23-chi-tiết-quy-tắc-2-kiểm-soát-giá-bán--giá-vốn-br-prod-02-price-validation--cost-rule)
   - 2.4. [Chi tiết Quy tắc 3: Quản lý Trạng thái & Xóa mềm (BR-PROD-03: Product Status & Soft Delete Rule)](#24-chi-tiết-quy-tắc-3-quản-lý-trạng-thái--xóa-mềm-br-prod-03-product-status--soft-delete-rule)
   - 2.5. [Chi tiết Quy tắc 4: Chuẩn hóa Đơn vị tính (BR-PROD-04: Unit Standardization Rule)](#25-chi-tiết-quy-tắc-4-chuẩn-hóa-đơn-vị-tính-br-prod-04-unit-standardization-rule)
   - 2.6. [Chi tiết Quy tắc 5: Ràng buộc Danh mục & Nhà cung cấp (BR-PROD-05: Category & Supplier Constraint)](#26-chi-tiết-quy-tắc-5-ràng-buộc-danh-mục--nhà-cung-cấp-br-prod-05-category--supplier-constraint)
   - 2.7. [Chi tiết Quy tắc 6: Quản lý Hình ảnh Sản phẩm (BR-PROD-06: Product Image Rule)](#27-chi-tiết-quy-tắc-6-quản-lý-hình-ảnh-sản-phẩm-br-prod-06-product-image-rule)
   - 2.8. [Chi tiết Quy tắc 7: Phân quyền Thao tác theo Vai trò (BR-PROD-07: Role-based Authorization Rule)](#28-chi-tiết-quy-tắc-7-phân-quyền-thao-tác-theo-vai-trò-br-prod-07-role-based-authorization-rule)
   - 2.9. [Chi tiết Quy tắc 8: Kiểm soát Dữ liệu Tồn kho khi Đổi Trạng thái (BR-PROD-08: Inventory Status Linkage Rule)](#29-chi-tiết-quy-tắc-8-kiểm-soát-dữ-liệu-tồn-kho-khi-đổi-trạng-thái-br-prod-08-inventory-status-linkage-rule)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** định nghĩa tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules) bắt buộc phải tuân thủ cho phân hệ **Quản lý Sản phẩm (02_Product)** trong hệ thống **Smart SuperMarket**. Các quy tắc này đóng vai trò là điều kiện tiên quyết cho việc xây dựng logic tầng Business Service (`ProductService.cs`), tầng Validation DTOs, các bộ lọc CSDL và giao diện ứng dụng (WinForms POS / Admin & React Web Client).

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh mục Quy tắc Nghiệp vụ (Business Rules Summary)

| Mã quy tắc | Tên quy tắc nghiệp vụ | Phạm vi áp dụng | Mức độ nghiêm ngặt |
| :---: | :--- | :--- | :---: |
| **BR-PROD-01** | Tính Duy nhất của Mã vạch (Barcode Uniqueness) | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-PROD-02** | Quy tắc Giá bán & Giá vốn (Pricing Controls) | Backend Service / WinForms UI | **Bắt buộc (Critical)** |
| **BR-PROD-03** | Trạng thái Kinh doanh & Xóa mềm (Soft Delete Only) | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-PROD-04** | Chuẩn hóa Đơn vị tính (Unit Standardization) | DTO Validation / WinForms UI | **Bắt buộc (High)** |
| **BR-PROD-05** | Ràng buộc Danh mục & Nhà cung cấp chính | Backend Service / CSDL | **Bắt buộc (High)** |
| **BR-PROD-06** | Quy định Quản lý & Lưu trữ Hình ảnh | Backend API / Static Server | **Khuyên dùng (Medium)** |
| **BR-PROD-07** | Phân quyền Quản lý Sản phẩm theo Vai trò | Controller Authorize Filter | **Bắt buộc (Critical)** |
| **BR-PROD-08** | Liên kết Trạng thái Sản phẩm với Tồn kho POS | Backend Order Service / POS Form | **Bắt buộc (High)** |

---

### 2.2. Chi tiết Quy tắc 1: Mã vạch Duy nhất (BR-PROD-01: Barcode Uniqueness Rule)

1. **Mô tả**: Mỗi sản phẩm lưu trong CSDL bắt buộc phải sở hữu một mã vạch (`Barcode`) hoàn toàn duy nhất.
2. **Quy định chi tiết**:
   - Khi tạo mới sản phẩm: Hệ thống kiểm tra `Barcode` trong CSDL. Nếu đã tồn tại $\rightarrow$ Báo lỗi `400 Bad Request` ("Mã vạch này đã tồn tại trên hệ thống").
   - Khi cập nhật sản phẩm: Cho phép giữ nguyên mã vạch cũ của sản phẩm đó, nhưng nếu đổi sang mã vạch khác thì mã vạch mới cũng phải không trùng với bất kỳ sản phẩm nào khác trong CSDL.
   - Định dạng mã vạch: Hỗ trợ mã vạch tiêu chuẩn quốc tế EAN-13, EAN-8, CODE-128 (độ dài 8 đến 50 ký tự), chỉ bao gồm chữ cái, chữ số, không chứa ký tự đặc biệt nguy hiểm (`<`, `>`, `'`, `"`, `%`).

---

### 2.3. Chi tiết Quy tắc 2: Kiểm soát Giá bán & Giá vốn (BR-PROD-02: Price Validation & Cost Rule)

1. **Giá bán niêm yết (`Price`)**:
   - Phải lớn hơn hoặc bằng 0 (`Price >= 0`). Không chấp nhận giá bán âm.
   - Phải được nhập đến 2 chữ số thập phân (`DECIMAL(18,2)`), đơn vị tiền tệ là VNĐ.
2. **Giá vốn trung bình (`CostPrice`)**:
   - Có thể bằng `NULL` khi mới khởi tạo sản phẩm chưa nhập kho.
   - Khi đã nhập kho: Giá vốn phải lớn hơn hoặc bằng 0 (`CostPrice >= 0`).
3. **Cảnh báo nghiệp vụ Bán lỗ (Negative Margin Warning)**:
   - Nếu `Price < CostPrice` (Giá bán thấp hơn giá vốn): Backend vẫn cho phép lưu nhưng trả về cảnh báo (Warning flag) để hiển thị ô vàng trên giao diện WinForms Admin, giúp Quản lý phát hiện việc thiết lập giá bán bị lỗ.

---

### 2.4. Chi tiết Quy tắc 3: Quản lý Trạng thái & Xóa mềm (BR-PROD-03: Product Status & Soft Delete Rule)

1. **Định nghĩa trạng thái `Status`**:
   - `1` = **`Active` (Đang bán)**: Sản phẩm hiển thị trên Web Khách hàng, cho phép tìm kiếm và quét mã vạch bán hàng tại POS, cho phép nhập kho.
   - `2` = **`Inactive` (Ngừng kinh doanh)**: Sản phẩm bị ẩn khỏi Web Khách hàng, máy quét POS khi quét mã vạch sản phẩm này sẽ báo lỗi "Sản phẩm đã ngừng kinh doanh", không cho phép lập phiếu nhập kho mới.
2. **Nguyên tắc Xóa mềm (Soft Delete)**:
   - **Tuyệt đối không xóa vật lý (Physical Delete)** bản ghi trong bảng `Product` nếu sản phẩm đã phát sinh dữ liệu trong các bảng: `OrderDetail`, `Inventory`, `ImportDetail`, `StockHistory`.
   - Lệnh DELETE từ Client/Admin Controller sẽ tự động chuyển đổi thành hành động cập nhật `Status = 2 (Ngừng kinh doanh)`.

---

### 2.5. Chi tiết Quy tắc 4: Chuẩn hóa Đơn vị tính (BR-PROD-04: Unit Standardization Rule)

1. **Đơn vị tính (`Unit`)**:
   - Không được để trống (Not Null / Not Empty).
   - Độ dài tối đa 20 ký tự, viết bằng tiếng Việt thường (VD: `chai`, `lon`, `hộp`, `gói`, `kg`, `gram`, `lốc`, `thùng`, `túi`, `bó`, `vỉ`).
2. **Quy tắc hiển thị**:
   - Trên hóa đơn POS và Web Client: Giá bán luôn đi kèm đơn vị tính tương ứng (Ví dụ: `36.000 VNĐ / hộp`).

---

### 2.6. Chi tiết Quy tắc 5: Ràng buộc Danh mục & Nhà cung cấp (BR-PROD-05: Category & Supplier Constraint)

1. **Bắt buộc có Danh mục (`CategoryId`)**:
   - Mỗi sản phẩm bắt buộc phải thuộc về đúng 1 Danh mục sản phẩm (`CategoryId` NOT NULL).
   - `CategoryId` phải tồn tại trong bảng `Category`. Nếu không tồn tại $\rightarrow$ Báo lỗi `400 Bad Request`.
2. **Nhà cung cấp chính (`SupplierId` - Theo ERD v2.0)**:
   - Cho phép `NULL` (nếu sản phẩm chưa gán nhà cung cấp cố định).
   - Nếu `SupplierId` được truyền vào: Phải kiểm tra tồn tại trong bảng `Supplier`.

---

### 2.7. Chi tiết Quy tắc 6: Quản lý Hình ảnh Sản phẩm (BR-PROD-06: Product Image Rule)

1. **Đường dẫn ảnh (`ImageUrl`)**:
   - Lưu dưới dạng đường dẫn tương đối (Relative URL), ví dụ: `/images/products/prod_8935001800012.jpg`.
2. **Ảnh mặc định (Fallback Image)**:
   - Nếu `ImageUrl` là `NULL` hoặc chuỗi rỗng: Backend/Client tự động trả về đường dẫn ảnh mặc định hệ thống: `/images/products/no-image.png`.
3. **Định dạng & Dung lượng**:
   - Chỉ chấp nhận file ảnh định dạng `.jpg`, `.jpeg`, `.png`, `.webp`. Dung lượng tối đa **5 MB / file**.

---

### 2.8. Chi tiết Quy tắc 7: Phân quyền Thao tác theo Vai trò (BR-PROD-07: Role-based Authorization Rule)

| Hành động / API Endpoint | Admin (1) | Manager (2) | Staff (3) | Customer (4) / Public |
| :--- | :---: | :---: | :---: | :---: |
| **Xem danh sách / Chi tiết sản phẩm** | ✅ | ✅ | ✅ | ✅ |
| **Quét Barcode tìm sản phẩm POS** | ✅ | ✅ | ✅ | ❌ |
| **Tạo mới Sản phẩm (`POST /api/products`)** | ✅ | ✅ | ❌ | ❌ |
| **Cập nhật Sản phẩm (`PUT /api/products/{id}`)** | ✅ | ✅ | ❌ | ❌ |
| **Đổi trạng thái Ngừng bán (`DELETE`)** | ✅ | ✅ | ❌ | ❌ |
| **Upload Hình ảnh Sản phẩm** | ✅ | ✅ | ❌ | ❌ |

---

### 2.9. Chi tiết Quy tắc 8: Liên kết Trạng thái Sản phẩm với Tồn kho POS (BR-PROD-08: Inventory Status Linkage Rule)

1. Khi nhân viên thu ngân quét Barcode tại quầy POS (`PosForm` WinForms):
   - Hệ thống tra cứu `Product` theo `Barcode`.
   - Nếu `Status == 2 (Ngừng kinh doanh)` $\rightarrow$ Không cho phép thêm vào giỏ hàng POS, hiển thị thông báo lỗi rõ ràng.
   - Khái niệm tồn kho (`QuantityInStock`) sẽ được kiểm tra ở bảng `Inventory` tương ứng với `BranchId` của nhân viên thu ngân.

---

## 3. GHI CHÚ
- Toàn bộ các quy tắc nghiệp vụ trên phải được kiểm tra (Validate) ở cả 2 tầng: **Client Side** (Giao diện WinForms ErrorProvider / React Form Validation) và **Server Side** (Fluent Validation DTOs & Service Layer).
- Khi có thay đổi về quy tắc nghiệp vụ giá bán hoặc mã vạch, phải cập nhật đồng thời tài liệu này và bộ test case tự động.

---

## 4. KẾT LUẬN

Tài liệu `BusinessRules.md` đã thiết lập hệ thống 8 Quy tắc Nghiệp vụ chuẩn mực cho phân hệ Sản phẩm. Các quy tắc này đảm bảo tính nhất quán dữ liệu, an toàn giao dịch bán hàng POS và kiểm soát phân quyền chặt chẽ cho toàn bộ hệ thống Smart SuperMarket.
