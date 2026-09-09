# SƠ ĐỒ TUẦN HOÀN QUẢN LÝ SẢN PHẨM (PRODUCT SEQUENCE DIAGRAM SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Sơ đồ 1: Quyết định Bán hàng POS Quét Mã vạch (POS Barcode Scanning Flow)](#21-sơ-đồ-1-quyết-định-bán-hàng-pos-quét-mã-vạch-pos-barcode-scanning-flow)
   - 2.2. [Sơ đồ 2: Admin Tạo mới Sản phẩm & Upload Ảnh (Product Creation & Image Upload Flow)](#22-sơ-đồ-2-admin-tạo-mới-sản-phẩm--upload-ảnh-product-creation--image-upload-flow)
   - 2.3. [Sơ đồ 3: Admin Cập nhật Giá & Thông tin Sản phẩm (Product Update Flow)](#23-sơ-đồ-3-admin-cập-nhật-giá--thông-tin-sản-phẩm-product-update-flow)
   - 2.4. [Sơ đồ 4: Khách hàng Duyệt & Tìm kiếm Sản phẩm trên Web (Web Customer Search Flow)](#24-sơ-đồ-4-khách-hàng-duyệt--tìm-kiếm-sản-phẩm-trên-web-web-customer-search-flow)
   - 2.5. [Sơ đồ 5: Admin Khóa mềm / Ngừng Kinh doanh Sản phẩm (Soft Delete Flow)](#25-sơ-đồ-5-admin-khóa-mềm--ngừng-kinh-doanh-sản-phẩm-soft-delete-flow)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **SequenceDiagram.md** mô tả các Sơ đồ Tuần hoàn (Sequence Diagrams) cho các quy trình nghiệp vụ cốt lõi thuộc phân hệ **02_Product (Quản lý Sản phẩm)**. Các sơ đồ này minh họa trực quan trình tự tương tác giữa các tác nhân (Thu ngân POS, Quản trị viên, Khách hàng), ứng dụng WinForms Desktop / React Web Client, tầng Backend REST API (.NET 8/9) và CSDL PostgreSQL.

Tất cả các sơ đồ đều được vẽ bằng cú pháp Mermaid chuẩn, sẵn sàng để render trực tiếp trên GitHub và VS Code.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Sơ đồ 1: Quyết định Bán hàng POS Quét Mã vạch (POS Barcode Scanning Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Cashier as Thu ngân POS
    participant POS as WinForms PosForm
    participant API as Backend .NET API
    participant DB as CSDL PostgreSQL

    Cashier->>POS: Quét mã vạch sản phẩm bằng Máy quét USB
    POS->>POS: Bắt sự kiện KeyPress & Trích xuất chuỗi Barcode
    POS->>API: GET /api/products/barcode/{barcode}
    API->>DB: Query SELECT * FROM Product WHERE Barcode = @barcode
    DB-->>API: Trả về bản ghi Product
    alt Sản phẩm tồn tại & Status == 1 (Active)
        API-->>POS: HTTP 200 OK (ProductDto)
        POS->>POS: Tự động nhảy giỏ hàng POS & Phát âm thanh Beep 🔔
        POS-->>Cashier: Hiển thị sản phẩm trên danh sách tính tiền
    else Sản phẩm bị khóa (Status == 2) hoặc Không tồn tại
        API-->>POS: HTTP 404 Not Found / HTTP 400 Bad Request
        POS-->>Cashier: Hiển thị thông báo đỏ & Phát âm thanh Cảnh báo ⚠️
    end
```

---

### 2.2. Sơ đồ 2: Admin Tạo mới Sản phẩm & Upload Ảnh (Product Creation & Image Upload Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Quản trị viên (Admin/Manager)
    participant UI as WinForms Admin Form
    participant API as Backend .NET API
    participant Storage as File Storage (wwwroot)
    participant DB as CSDL PostgreSQL

    Admin->>UI: Nhập thông tin (Tên, Mã vạch, Giá, ĐVT, Danh mục, NCC)
    Admin->>UI: Kéo-Thả hoặc Chọn file hình ảnh sản phẩm
    UI->>API: POST /api/products/upload-image (Multipart File)
    API->>API: Validate định dạng (.jpg/png/webp) & Dung lượng (< 5MB)
    API->>Storage: Lưu file ảnh vào wwwroot/images/products/
    Storage-->>API: Trả về ImageUrl tương đối
    API-->>UI: HTTP 200 OK (ImageUrl)
    
    UI->>API: POST /api/products (CreateProductRequest JSON)
    API->>DB: Kiểm tra Barcode đã tồn tại chưa
    alt Barcode chưa tồn tại
        API->>DB: INSERT INTO Product (...) VALUES (...)
        DB-->>API: Trả về ProductId vừa tạo
        API-->>UI: HTTP 201 Created (ProductDto)
        UI-->>Admin: Hiển thị thông báo "Tạo mới sản phẩm thành công"
    else Barcode đã tồn tại
        API-->>UI: HTTP 400 Bad Request ("Mã vạch đã tồn tại")
        UI-->>Admin: Hiển thị lỗi đỏ trên Form
    end
```

---

### 2.3. Sơ đồ 3: Admin Cập nhật Giá & Thông tin Sản phẩm (Product Update Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Quản trị viên (Admin/Manager)
    participant UI as WinForms Admin Form
    participant API as Backend .NET API
    participant DB as CSDL PostgreSQL

    Admin->>UI: Chọn sản phẩm & Thay đổi Giá bán / Thông tin
    UI->>API: PUT /api/products/{id} Header [Authorization: Bearer JWT]
    API->>API: Validate JWT Token & Kiểm tra Role (Admin/Manager)
    API->>DB: Query SELECT * FROM Product WHERE ProductId = @id
    alt Tìm thấy sản phẩm
        API->>DB: UPDATE Product SET Price = @price, UpdatedAt = UTC_NOW
        DB-->>API: Cập nhật thành công
        API-->>UI: HTTP 200 OK (ProductDto)
        UI-->>Admin: Cập nhật lại danh sách DataGridView trên Form
    else Không tìm thấy sản phẩm
        API-->>UI: HTTP 404 Not Found
        UI-->>Admin: Thông báo "Sản phẩm không tồn tại"
    end
```

---

### 2.4. Sơ đồ 4: Khách hàng Duyệt & Tìm kiếm Sản phẩm trên Web (Web Customer Search Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Customer as Khách hàng
    participant Web as React Web Client
    participant API as Backend .NET API
    participant DB as CSDL PostgreSQL

    Customer->>Web: Nhập từ khóa tìm kiếm & Chọn lọc Danh mục
    Web->>API: GET /api/products?search=coca&categoryId=1&page=1
    API->>DB: Query SELECT * FROM Product WHERE Status=1 AND Name LIKE %coca%
    DB-->>API: Trả về tập danh sách bản ghi & TotalCount
    API-->>Web: HTTP 200 OK (PagedResult<ProductDto>)
    Web->>Web: Render lưới sản phẩm ProductGrid & Phân trang
    Web-->>Customer: Hiển thị danh sách sản phẩm kèm giá & hình ảnh
```

---

### 2.5. Sơ đồ 5: Admin Khóa mềm / Ngừng Kinh doanh Sản phẩm (Soft Delete Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Quản trị viên (Admin/Manager)
    participant UI as WinForms Admin Form
    participant API as Backend .NET API
    participant DB as CSDL PostgreSQL

    Admin->>UI: Bấm nút "Ngừng kinh doanh" sản phẩm
    UI->>UI: Hiện Dialog xác nhận "Bạn có chắc muốn ngưng bán sản phẩm này?"
    Admin->>UI: Bấm "Đồng ý"
    UI->>API: DELETE /api/products/{id} Header [Authorization: Bearer JWT]
    API->>DB: UPDATE Product SET Status = 2 (Inactive), UpdatedAt = UTC_NOW
    DB-->>API: Cập nhật trạng thái thành công
    API-->>UI: HTTP 200 OK (ApiResult<bool>)
    UI->>UI: Đổi màu dòng sản phẩm sang Xám trên DataGridView
    UI-->>Admin: Thông báo "Đã chuyển trạng thái Ngừng kinh doanh"
```

---

## 3. GHI CHÚ
- Tất cả các luồng thay đổi dữ liệu (`POST`, `PUT`, `DELETE`) bắt buộc phải kiểm tra quyền `[Authorize(Roles = "Admin,Manager")]` tại Controller.
- Luồng POS quét mã vạch là luồng nhạy cảm nhất về thời gian phản hồi, yêu cầu thời gian xử lý toàn trình từ máy quét đến khi hiển thị UI dưới 100ms.

---

## 4. KẾT LUẬN

Tài liệu `SequenceDiagram.md` đã mô tả chi tiết 5 sơ đồ tuần hoàn thể hiện chính xác các luồng tương tác dữ liệu cho phân hệ Sản phẩm. Các sơ đồ này là căn cứ thiết kế luồng xử lý (Control Flow) cho lập trình viên Backend và Frontend/WinForms.
