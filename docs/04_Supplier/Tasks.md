# DANH SÁCH CÔNG VIỆC TRIỂN KHAI PHÂN HỆ QUẢN LÝ NHÀ CUNG CẤP (SUPPLIER TASKS SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Chi tiết Các Phase Triển khai](#2-chi-tiết-các-phase-triển-khai)
   - 2.1. [Phase 1: Database Layer (Entity, Mapping, Migration)](#21-phase-1-database-layer-entity-mapping-migration)
   - 2.2. [Phase 2: Repository Layer (Data Access, CRUD, N-N Queries)](#22-phase-2-repository-layer-data-access-crud-n-n-queries)
   - 2.3. [Phase 3: Service Layer (Business Logic & Business Rules)](#23-phase-3-service-layer-business-logic--business-rules)
   - 2.4. [Phase 4: API Layer (Controller, DTOs, Authorization)](#24-phase-4-api-layer-controller-dtos-authorization)
   - 2.5. [Phase 5: Test Layer (Unit Tests, Integration Tests)](#25-phase-5-test-layer-unit-tests-integration-tests)
3. [Quy định Kiểm thử & Báo cáo Completeness](#3-quy-định-kiểm-thử--báo-cáo-completeness)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Tasks.md** chia nhỏ lộ trình thực thi phân hệ **Quản lý Nhà cung cấp (04_Supplier)** thành 5 Phase độc lập theo kiến trúc Clean Architecture. Mỗi Phase định nghĩa rõ danh sách công việc (Checklist), tiêu chí hoàn thành (Acceptance Criteria) và các hạn chế không được phép vi phạm.

---

## 2. CHI TIẾT CÁC PHASE TRIỂN KHAI

### 2.1. Phase 1: Database Layer (Entity, Mapping, Migration)

- [ ] **Task 1.1**: Cập nhật Domain Entity `Supplier.cs` trong `Backend/Domain/Entities/`:
  - Thêm các thuộc tính: `SupplierId`, `SupplierName`, `ContactPerson`, `PhoneNumber`, `Email`, `Address`, `Status`, `CreatedAt`, `UpdatedAt`.
  - Thêm thuộc tính điều hướng: `ICollection<ProductSupplier> ProductSuppliers`, `ICollection<ImportReceipt> ImportReceipts`.
- [ ] **Task 1.2**: Tạo mới Domain Entity `ProductSupplier.cs` trong `Backend/Domain/Entities/`:
  - Khóa chính phức hợp: `ProductId`, `SupplierId`.
  - Các trường thương mại: `PurchasePrice`, `SupplierProductCode`, `LeadTime`, `MinimumOrderQuantity`, `Rating`, `IsDefault`, `CreatedAt`, `UpdatedAt`.
  - Navigation Properties: `Product`, `Supplier`.
- [ ] **Task 1.3**: Cập nhật EF Core Fluent API Configurations:
  - Cập nhật `SupplierConfiguration.cs`: Bảng `"Supplier"`, PK `SupplierId`, Unique index `SupplierName`, Status default 1.
  - Tạo mới `ProductSupplierConfiguration.cs`: Bảng `"ProductSupplier"`, PK `(ProductId, SupplierId)`, precision `PurchasePrice(18,2)`, precision `Rating(3,2)`, FK relationships `DeleteBehavior.Cascade`.
- [ ] **Task 1.4**: Cập nhật `AppDbContext.cs`: Bổ sung `DbSet<ProductSupplier> ProductSuppliers { get; set; }`.
- [ ] **Task 1.5**: Thêm Migration EF Core:
  - Chạy lệnh `dotnet ef migrations add AddSupplierAndProductSupplierSchema`.
- [ ] **Task 1.6**: Seed Data ban đầu: Bổ sung 3 Nhà cung cấp mẫu trong `DbInitializer.cs`.

**Tiêu chí hoàn thành (Phase 1)**: `dotnet build` thành công 0 Error, Migration tạo đầy đủ bảng `Supplier` và `ProductSupplier`.

---

### 2.2. Phase 2: Repository Layer (Data Access, CRUD, N-N Queries)

- [ ] **Task 2.1**: Tạo mới Interface `ISupplierRepository.cs` trong `Features/Suppliers/Repositories/`:
  - Định nghĩa các hàm: `GetByIdAsync`, `GetAllAsync`, `GetPagedAsync`, `SearchAsync`, `ExistsNameAsync`, `GetSuppliedProductsAsync`, `AddAsync`, `Update`, `Delete`, `LinkProductSupplierAsync`, `UnlinkProductSupplierAsync`, `ResetDefaultSupplierAsync`, `SaveChangesAsync`.
- [ ] **Task 2.2**: Triển khai class `SupplierRepository.cs`:
  - Sử dụng `AppDbContext` thực thi tất cả các truy vấn CRUD.
  - Xử lý các thao tác Many-to-Many với bảng `ProductSupplier`.
  - Tối ưu hóa truy vấn với `AsNoTracking()` và `Include()`.

**Tiêu chí hoàn thành (Phase 2)**: `dotnet build` thành công, Repository hỗ trợ đầy đủ các thao tác N-N.

---

### 2.3. Phase 3: Service Layer (Business Logic & Business Rules)

- [ ] **Task 3.1**: Định nghĩa DTOs trong `Features/Suppliers/DTOs/SupplierDTOs.cs`:
  - `SupplierDto`, `SupplierDetailDto`, `CreateSupplierRequest`, `UpdateSupplierRequest`, `ProductSupplierDto`, `LinkProductSupplierRequest`, `SupplierPagedResult`.
- [ ] **Task 3.2**: Tạo mới Interface `ISupplierService.cs` trong `Features/Suppliers/Services/`.
- [ ] **Task 3.3**: Triển khai class `SupplierService.cs` thực thi toàn bộ 8 Quy tắc Nghiệp vụ (Business Rules):
  - **BR-SUPP-01**: Kiểm tra duy nhất `SupplierName`.
  - **BR-SUPP-02**: Validate số điện thoại VN và email RFC 5322.
  - **BR-SUPP-03**: Xử lý logic chỉ có 1 `IsDefault = true` cho mỗi sản phẩm.
  - **BR-SUPP-04**: Validate `PurchasePrice >= 0`, `LeadTime >= 0`, `MinimumOrderQuantity >= 1`, `Rating 1-5`.
  - **BR-SUPP-05**: Xử lý hủy liên kết sản phẩm.
  - **BR-SUPP-06**: Chuyển thành Soft Delete (`Status = 2`) nếu đã có lịch sử nhập hàng.

**Tiêu chí hoàn thành (Phase 3)**: Toàn bộ Business Rules được thực thi chặt chẽ, không đẩy logic xuống Controller.

---

### 2.4. Phase 4: API Layer (Controller, DTOs, Authorization)

- [ ] **Task 4.1**: Tạo mới `SupplierController.cs` trong `Features/Suppliers/Controllers/`:
  - Đăng ký Route `api/v1/suppliers`.
  - Triển khai 9 Endpoints chuẩn RESTful (Paged, Detail, Supplied Products, Create, Update, Delete, Restore, Link Product, Unlink Product).
- [ ] **Task 4.2**: Cấu hình Phân quyền RBAC (Attribute `[Authorize]`):
  - Đăng ký `[Authorize(Roles = "Admin,Manager,Staff")]` cho các API ghi (`POST`, `PUT`, `DELETE`).
  - Đăng ký `[Authorize(Roles = "Admin,Manager,Staff,AI")]` cho các API đọc (`GET`).
- [ ] **Task 4.3**: Chuẩn hóa Phản hồi `ApiResult<T>` và Swagger OpenApi Annotations.

**Tiêu chí hoàn thành (Phase 4)**: Đầy đủ 9 Endpoints hiển thị chuẩn trên Swagger UI, phân quyền đúng vai trò.

---

### 2.5. Phase 5: Test Layer (Unit Tests, Integration Tests)

- [ ] **Task 5.1**: Tạo mới class test `SupplierServiceTests.cs` trong `Backend.Tests/`:
  - Viết 20+ Unit Test Cases bằng `xUnit` và `Moq` kiểm thử tất cả các Business Rules (BR-SUPP-01 đến BR-SUPP-08).
- [ ] **Task 5.2**: Chạy kiểm thử toàn bộ giải pháp:
  - Chạy `dotnet test` đảm bảo 100% test cases PASSED.

**Tiêu chí hoàn thành (Phase 5)**: `dotnet test` trả về tất cả Test Passed, 0 Failure.

---

## 3. QUY ĐỊNH KIỂM THỬ & BÁO CÁO COMPLETENESS

Khi hoàn thành từng Phase, người thực hiện phải chạy `dotnet build` và đối chiếu mã nguồn với tài liệu `docs/04_Supplier/` để lập báo cáo theo cấu trúc:
1. Danh sách file đã tạo/sửa.
2. Bảng Mapping Business Rules -> Code implementation.
3. Kết quả `dotnet build` & `dotnet test`.

---

## 4. KẾT LUẬN

Tài liệu `Tasks.md` cung cấp lộ trình 5 Phase chi tiết và chặt chẽ, giúp đội ngũ phát triển nhanh chóng hoàn thiện phân hệ Quản lý Nhà cung cấp đạt chuẩn chất lượng cao.
