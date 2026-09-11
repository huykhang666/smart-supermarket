# DANH SÁCH TASK TRIỂN KHAI PHÂN HỆ 05_INVENTORY (TASKS SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Task Backend Developer](#21-task-backend-developer)
   - 2.2. [Task WinForms Developer](#22-task-winforms-developer)
   - 2.3. [Task Testing](#23-task-testing)
   - 2.4. [Thứ tự ưu tiên triển khai](#24-thứ-tự-ưu-tiên-triển-khai)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Tasks.md** liệt kê toàn bộ công việc cần thực hiện để triển khai hoàn chỉnh phân hệ `05_Inventory`, phân chia rõ ràng theo vai trò Backend Developer và WinForms Desktop Developer. Thứ tự các task được sắp xếp theo phụ thuộc (dependency) để tránh block lẫn nhau.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Task Backend Developer

#### Nhóm 1: Domain Layer (Ưu tiên cao — làm trước)

- [ ] **INV-BE-01**: Tạo Entity class `Inventory.cs` tại `Domain/Entities/` theo đúng `InventoryEntity.md` mục 2.5.
- [ ] **INV-BE-02**: Tạo Entity class `StockHistory.cs` tại `Domain/Entities/` theo đúng `InventoryEntity.md` mục 2.5.
- [ ] **INV-BE-03**: Tạo Entity class `DiscountRule.cs` tại `Domain/Entities/` theo đúng `InventoryEntity.md` mục 2.5.
- [ ] **INV-BE-04**: Tạo Enum `StockChangeType.cs` tại `Domain/Enums/` với 4 giá trị: `Import=1`, `Sale=2`, `Adjustment=3`, `Expired=4`.

#### Nhóm 2: Infrastructure Layer (EF Core Configuration)

- [ ] **INV-BE-05**: Thêm `DbSet<Inventory>`, `DbSet<StockHistory>`, `DbSet<DiscountRule>` vào `AppDbContext.cs`.
- [ ] **INV-BE-06**: Cấu hình EF Core Fluent API cho bảng `Inventory`:
  - Ràng buộc UNIQUE trên `(ProductId, BranchId)`.
  - Giá trị mặc định `QuantityOnHand = 0`, `MinStockLevel = 10`.
  - Quan hệ FK tới `Product` và `Branch`.
- [ ] **INV-BE-07**: Cấu hình EF Core cho `StockHistory`:
  - Column `ChangeType` map sang Enum `StockChangeType`.
  - Ràng buộc `QuantityBefore >= 0`, `QuantityAfter >= 0`.
- [ ] **INV-BE-08**: Chạy `dotnet ef migrations add AddInventoryModule` và kiểm tra file Migration sinh ra.
- [ ] **INV-BE-09**: Chạy `dotnet ef database update` và seed data mặc định cho `DiscountRule` (2 quy tắc: 7 ngày → 50%, 15 ngày → 20%).

#### Nhóm 3: Feature Layer (Service + Controller)

- [ ] **INV-BE-10**: Tạo folder `Features/Inventory/` với cấu trúc:
  ```
  Features/Inventory/
  ├── DTOs/
  │   ├── InventoryDto.cs
  │   ├── StockHistoryDto.cs
  │   ├── StockAdjustRequest.cs
  │   └── ExpiringProductDto.cs
  ├── IInventoryService.cs
  ├── InventoryService.cs
  └── InventoryController.cs
  ```
- [ ] **INV-BE-11**: Implement `InventoryService.GetInventoryListAsync()` — lấy danh sách tồn kho theo branchId, hỗ trợ filter và phân trang.
- [ ] **INV-BE-12**: Implement `InventoryService.GetInventoryByProductAsync()` — chi tiết tồn kho 1 sản phẩm kèm danh sách lô hàng và HSD.
- [ ] **INV-BE-13**: Implement `InventoryService.GetLowStockProductsAsync()` — sản phẩm tồn kho ≤ MinStockLevel.
- [ ] **INV-BE-14**: Implement `InventoryService.GetExpiringProductsAsync(withinDays)` — sản phẩm có lô hàng sắp hết hạn trong `withinDays` ngày tới.
- [ ] **INV-BE-15**: Implement `InventoryService.AdjustStockAsync()` — điều chỉnh thủ công, validate Note bắt buộc, ghi StockHistory, thực hiện trong Transaction.
- [ ] **INV-BE-16**: Implement `InventoryService.AddStockAsync()` — **internal method** được gọi bởi `ImportReceiptService` khi Confirm phiếu nhập (không expose API riêng).
- [ ] **INV-BE-17**: Implement `InventoryService.DeductStockFEFOAsync()` — **internal method** được gọi bởi `OrderService` khi bán hàng, thực hiện FEFO theo `FEFO.md`.
- [ ] **INV-BE-18**: Implement `InventoryController` với các endpoints theo `API.md`.
- [ ] **INV-BE-19**: Tạo `Features/DiscountRules/` với Service và Controller cho 2 endpoints: GET list và PUT update.
- [ ] **INV-BE-20**: Thêm `[Authorize(Roles = "Admin,Manager,Staff")]` và `[Authorize(Roles = "Admin,Manager")]` đúng theo ma trận phân quyền trong `README.md`.

---

### 2.2. Task WinForms Developer

#### Form quản lý tồn kho

- [ ] **INV-WIN-01**: Tạo Form `frmInventoryList.cs` hiển thị danh sách tồn kho dạng DataGridView:
  - Cột: Tên sản phẩm, Barcode, Danh mục, Tồn kho, Ngưỡng tối thiểu, HSD gần nhất.
  - Tô màu hàng: ĐỎ khi LowStock, VÀNG khi ExpiryDate ≤ 15 ngày, ĐỎ đậm khi ExpiryDate ≤ 7 ngày.
  - Bộ lọc: Search theo tên/barcode, lọc theo trạng thái cảnh báo.
- [ ] **INV-WIN-02**: Thêm nút "Điều chỉnh tồn kho" mở Dialog `frmStockAdjust.cs`:
  - Nhập số lượng điều chỉnh (dương/âm).
  - Nhập lý do bắt buộc (Note).
  - Confirm trước khi gửi API.
- [ ] **INV-WIN-03**: Thêm tab "Lịch sử tồn kho" trong Form chi tiết sản phẩm, hiển thị StockHistory có filter theo khoảng thời gian và loại biến động.

#### Widget Dashboard

- [ ] **INV-WIN-04**: Tạo User Control `ucLowStockAlert.cs` cho Dashboard:
  - Hiển thị Top 5 sản phẩm sắp hết hàng nhất.
  - Nút "Tạo phiếu nhập" → Mở Form 06_Import.
- [ ] **INV-WIN-05**: Tạo User Control `ucExpiryAlert.cs` cho Dashboard:
  - Hiển thị sản phẩm sắp hết hạn trong 15 ngày.
  - Phân màu theo mức độ cảnh báo.
  - Hiển thị đề xuất giảm giá từ DiscountRule.

#### Form cấu hình DiscountRule

- [ ] **INV-WIN-06**: Tạo Form `frmDiscountRules.cs` (chỉ Admin):
  - Hiển thị danh sách quy tắc giảm giá HSD.
  - Cho phép chỉnh sửa `DaysBeforeExpiry`, `DiscountPercent`, bật/tắt `IsActive`.

---

### 2.3. Task Testing

- [ ] **INV-TEST-01**: Viết Unit Test cho `InventoryService.DeductStockFEFOAsync()`:
  - Test case: Sản phẩm có 2 lô HSD khác nhau → Đảm bảo lô HSD gần hơn được xuất trước.
  - Test case: Không đủ hàng → Ném `InsufficientStockException`.
  - Test case: Tồn kho về 0 sau khi xuất → `QuantityOnHand = 0` (không âm).
- [ ] **INV-TEST-02**: Viết Unit Test cho logic `DiscountRuleService.GetApplicableDiscountAsync()`:
  - Test case: Sản phẩm còn 5 ngày → Áp dụng quy tắc 7 ngày (50%).
  - Test case: Sản phẩm còn 12 ngày → Áp dụng quy tắc 15 ngày (20%).
  - Test case: Sản phẩm còn 30 ngày → Không áp dụng quy tắc nào (0%).
- [ ] **INV-TEST-03**: Test integration: Confirm phiếu nhập → Kiểm tra `Inventory.QuantityOnHand` tăng đúng và có bản ghi `StockHistory` tương ứng.
- [ ] **INV-TEST-04**: Test API endpoints qua Postman Collection (tạo file `SmartSupermarket_Inventory_Postman_Collection.json`).

---

### 2.4. Thứ tự ưu tiên triển khai

```
Tuần 1-2 (Nền tảng):
INV-BE-01 → INV-BE-02 → INV-BE-03 → INV-BE-04
INV-BE-05 → INV-BE-06 → INV-BE-07 → INV-BE-08 → INV-BE-09

Tuần 3-4 (Core Service):
INV-BE-10 → INV-BE-16 → INV-BE-17 (Internal methods trước)
INV-BE-11 → INV-BE-12 → INV-BE-13 → INV-BE-14 → INV-BE-15
INV-BE-18 → INV-BE-19 → INV-BE-20

Tuần 5-6 (WinForms UI):
INV-WIN-01 → INV-WIN-02 → INV-WIN-03
INV-WIN-04 → INV-WIN-05 → INV-WIN-06

Tuần 7 (Testing & Fix):
INV-TEST-01 → INV-TEST-02 → INV-TEST-03 → INV-TEST-04
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | `InventoryService.AddStockAsync()` và `DeductStockFEFOAsync()` là internal method hay có interface riêng để mock trong test? | Ảnh hưởng thiết kế interface và testability. | Đề xuất: Tách thành `IInventoryService` với đầy đủ method — kể cả internal methods — để có thể mock trong unit test của `OrderService` và `ImportReceiptService`. |

---

## 4. GHI CHÚ
- Task **INV-BE-16** (`AddStockAsync`) và **INV-BE-17** (`DeductStockFEFOAsync`) là các internal method, được gọi bởi `ImportReceiptService` và `OrderService`. Cần implement xong trước khi các module 06_Import và 07_Order có thể tích hợp.
- Ưu tiên hoàn thành task INV-BE-01 đến INV-BE-09 (Domain + Migration) trước tuần 3 để WinForms Developer có thể bắt đầu bind dữ liệu.
- Xem quy chuẩn commit tại `CodingConvention.md`: prefix `feat: add inventory service`, `fix: fefo stock deduction`, v.v.

---

## 5. KẾT LUẬN

Tài liệu `Tasks.md` đã liệt kê đầy đủ **20 Backend tasks**, **6 WinForms tasks** và **4 Testing tasks** cho phân hệ `05_Inventory`. Việc thực hiện theo đúng thứ tự ưu tiên và phụ thuộc sẽ đảm bảo phân hệ được hoàn thiện đúng tiến độ, sẵn sàng tích hợp với 06_Import và 07_Order trong hệ thống Smart SuperMarket.
