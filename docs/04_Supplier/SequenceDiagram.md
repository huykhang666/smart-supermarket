# SƠ ĐỒ TUẦN TỰ PHÂN HỆ QUẢN LÝ NHÀ CUNG CẤP (SUPPLIER SEQUENCE DIAGRAM SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Sơ đồ Tuần tự Chi tiết](#2-sơ-đồ-tuần-tự-chi-tiết)
   - 2.1. [Luồng 1: Tạo mới Nhà cung cấp (Admin / Staff)](#21-luồng-1-tạo-mới-nhà-cung-cấp-admin--staff)
   - 2.2. [Luồng 2: Cấu hình Liên kết ProductSupplier N-N & Thiết lập NCC Mặc định](#22-luồng-2-cấu-hình-liên-kết-productsupplier-n-n--thiết-lập-ncc-mặc-định)
   - 2.3. [Luồng 3: Xóa mềm Nhà cung cấp (Soft Delete)](#23-luồng-3-xóa-mềm-nhà-cung-cấp-soft-delete)
   - 2.4. [Luồng 4: AI Agent Tra cứu Read-Only Phân tích Cung ứng](#24-luồng-4-ai-agent-tra-cứu-read-only-phân-tích-cung-ứng)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **SequenceDiagram.md** mô hình hóa trực quan luồng tương tác giữa Người dùng (User / AI Agent), Giao diện Client, Controller, Service Layer, Repository và CSDL PostgreSQL trong phân hệ Nhà cung cấp.

---

## 2. SƠ ĐỒ TUẦN TỰ CHI TIẾT

### 2.1. Luồng 1: Tạo mới Nhà cung cấp (Admin / Staff)

```mermaid
sequenceDiagram
    autonumber
    actor User as Nhân viên / Admin
    participant Client as Web Admin / Desktop App
    participant Controller as SupplierController
    participant Service as SupplierService
    participant Repo as SupplierRepository
    participant DB as Database (PostgreSQL)

    User->>Client: Nhập thông tin NCC (Tên, SĐT, Email, Địa chỉ)
    Client->>Controller: POST /api/v1/suppliers (CreateSupplierRequest)
    Controller->>Controller: Validate Authorize (Role: Admin/Manager/Staff)
    Controller->>Service: CreateSupplierAsync(request)
    
    Service->>Repo: ExistsByNameAsync(supplierName)
    Repo->>DB: SELECT COUNT(*) FROM Supplier WHERE SupplierName = @name
    DB-->>Repo: 0 (Không trùng)
    Repo-->>Service: false

    Service->>Service: Validate BR-SUPP-01 & BR-SUPP-02 (Email/Phone format)
    Service->>Repo: AddAsync(new Supplier)
    Repo->>DB: INSERT INTO Supplier (...) VALUES (...)
    DB-->>Repo: Supplier Record Created
    Service->>Repo: SaveChangesAsync()
    Repo-->>Service: OK

    Service-->>Controller: SupplierDto
    Controller-->>Client: 201 Created (ApiResult<SupplierDto>)
    Client-->>User: Hiển thị thông báo "Tạo mới nhà cung cấp thành công!"
```

---

### 2.2. Luồng 2: Cấu hình Liên kết ProductSupplier N-N & Thiết lập NCC Mặc định

```mermaid
sequenceDiagram
    autonumber
    actor User as Quản lý kho / Admin
    participant Client as Web Admin / POS App
    participant Controller as SupplierController
    participant Service as SupplierService
    participant Repo as SupplierRepository
    participant DB as Database (PostgreSQL)

    User->>Client: Chọn Sản phẩm & NCC + Nhập Giá nhập, LeadTime, IsDefault=true
    Client->>Controller: POST /api/v1/suppliers/products/link (LinkProductSupplierRequest)
    Controller->>Service: LinkProductSupplierAsync(request)

    Service->>Repo: GetByIdAsync(productId), GetByIdAsync(supplierId)
    Repo->>DB: Tra cứu tồn tại Product & Supplier
    DB-->>Repo: Cả hai tồn tại hợp lệ
    Repo-->>Service: Entities Exists

    alt Nếu IsDefault = true
        Service->>Repo: ResetOtherDefaultsAsync(productId)
        Repo->>DB: UPDATE ProductSupplier SET IsDefault = false WHERE ProductId = @pId
    end

    Service->>Repo: AddOrUpdateProductSupplierAsync(linkData)
    Repo->>DB: UPSERT INTO ProductSupplier (ProductId, SupplierId, PurchasePrice, IsDefault, ...)
    DB-->>Repo: Saved
    Service->>Repo: SaveChangesAsync()
    Repo-->>Service: OK

    Service-->>Controller: true
    Controller-->>Client: 200 OK (ApiResult<bool>)
    Client-->>User: Hiển thị thông báo "Đã thiết lập liên kết cung ứng thành công!"
```

---

### 2.3. Luồng 3: Xóa mềm Nhà cung cấp (Soft Delete)

```mermaid
sequenceDiagram
    autonumber
    actor User as Quản lý / Admin
    participant Client as Web Client
    participant Controller as SupplierController
    participant Service as SupplierService
    participant Repo as SupplierRepository
    participant DB as Database (PostgreSQL)

    User->>Client: Bấm "Xóa" Nhà cung cấp ID = 5
    Client->>Controller: DELETE /api/v1/suppliers/5
    Controller->>Service: SoftDeleteSupplierAsync(5)

    Service->>Repo: GetByIdAsync(5)
    Repo->>DB: SELECT * FROM Supplier WHERE SupplierId = 5
    DB-->>Repo: Supplier Record Found

    Service->>Repo: HasImportReceiptsAsync(5)
    Repo->>DB: SELECT COUNT(*) FROM ImportReceipt WHERE SupplierId = 5
    DB-->>Repo: > 0 (Đã có phiếu nhập hàng)

    Note over Service: Phát hiện đã có lịch sử nhập hàng -> Thực hiện Soft Delete BR-SUPP-06

    Service->>Service: Update Status = 2 (Inactive)
    Service->>Repo: Update(supplier)
    Repo->>DB: UPDATE Supplier SET Status = 2 WHERE SupplierId = 5
    DB-->>Repo: OK
    Service->>Repo: SaveChangesAsync()
    Repo-->>Service: OK

    Service-->>Controller: true
    Controller-->>Client: 200 OK (Chuyển trạng thái sang Tạm ngừng hợp tác)
    Client-->>User: Cập nhật giao diện: Trạng thái NCC chuyển sang "Tạm ngừng hợp tác"
```

---

### 2.4. Luồng 4: AI Agent Tra cứu Read-Only Phân tích Cung ứng

```mermaid
sequenceDiagram
    autonumber
    actor AI as AI Agent (Gemini)
    participant Controller as SupplierController
    participant Service as SupplierService
    participant Repo as SupplierRepository
    participant DB as Database (PostgreSQL)

    AI->>Controller: GET /api/v1/suppliers/1/products (Header Token AI)
    Controller->>Controller: Validate Authorize (Role: AI Agent - Read-Only)
    
    alt Nếu AI gọi API Ghi (POST / PUT / DELETE)
        Controller-->>AI: 403 Forbidden ("AI Agent chỉ có quyền Read-Only")
    else Nếu AI gọi API Đọc (GET)
        Controller->>Service: GetSuppliedProductsAsync(1)
        Service->>Repo: GetSuppliedProductsBySupplierIdAsync(1)
        Repo->>DB: SELECT * FROM ProductSupplier ps JOIN Product p ON ... WHERE ps.SupplierId = 1
        DB-->>Repo: Product list with PurchasePrice & LeadTime
        Repo-->>Service: Return DTO List
        Service-->>Controller: Return ApiResult List
        Controller-->>AI: 200 OK (Danh sách dữ liệu giá nhập & lead time cho AI phân tích)
    end
```

---

## 3. GHI CHÚ
- Mọi thao tác ghi dữ liệu từ client đều trải qua kiểm tra token xác thực và phân quyền RBAC trước khi đi vào Service.
- Luồng AI Agent tuyệt đối tuân thủ chỉ xem dữ liệu Read-Only.

---

## 4. KẾT LUẬN

Tài liệu `SequenceDiagram.md` đã mô tả đầy đủ 4 luồng thao tác tuần tự cốt lõi của phân hệ Nhà cung cấp, là nền tảng trực quan cho quá trình phát triển mã nguồn backend và frontend.
