# DANH SÁCH TASK TRIỂN KHAI PHÂN HỆ 06_IMPORT (TASKS SPECIFICATION)

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

Tài liệu **Tasks.md** liệt kê toàn bộ công việc cần thực hiện để triển khai hoàn chỉnh phân hệ `06_Import`, phân chia rõ ràng theo vai trò Backend Developer và WinForms Desktop Developer. Các task có đánh dấu dependency với module `05_Inventory` để tránh block lẫn nhau.

> ⚠️ **Lưu ý phụ thuộc**: Phần lớn các task Backend của `06_Import` phụ thuộc vào `InventoryService.AddStockAsync()` từ `05_Inventory` (INV-BE-16). Phải hoàn thành `05_Inventory` Domain Layer và Service trước.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Task Backend Developer

#### Nhóm 1: Domain Layer

- [ ] **IMP-BE-01**: Tạo Entity class `ImportReceipt.cs` tại `Domain/Entities/` theo `ImportReceipt.md` mục 2.5.
- [ ] **IMP-BE-02**: Tạo Entity class `ImportDetail.cs` tại `Domain/Entities/` theo `ImportDetail.md` mục 2.5.
- [ ] **IMP-BE-03**: Tạo Enum `ImportStatus.cs` tại `Domain/Enums/` với 3 giá trị: `Draft=1`, `Confirmed=2`, `Cancelled=3`.

#### Nhóm 2: Infrastructure Layer (EF Core Configuration)

- [ ] **IMP-BE-04**: Thêm `DbSet<ImportReceipt>` và `DbSet<ImportDetail>` vào `AppDbContext.cs`.
- [ ] **IMP-BE-05**: Cấu hình EF Core Fluent API cho `ImportReceipt`:
  - Index UNIQUE trên `ReceiptCode`.
  - 2 quan hệ FK riêng biệt tới `User`: `ImportedByUserId` (Required) và `ConfirmedByUserId` (Optional).
  - Quan hệ FK tới `Supplier` và `Branch`.
  - Giá trị mặc định `Status = Draft`, `TotalAmount = 0`.
- [ ] **IMP-BE-06**: Cấu hình EF Core cho `ImportDetail`:
  - Quan hệ FK tới `ImportReceipt` với `OnDelete(DeleteBehavior.Cascade)`.
  - Quan hệ FK tới `Product` với `OnDelete(DeleteBehavior.Restrict)`.
  - Ràng buộc `Quantity > 0`, `CostPrice > 0`, `SubTotal >= 0`.
- [ ] **IMP-BE-07**: Chạy `dotnet ef migrations add AddImportModule` và kiểm tra file Migration.
- [ ] **IMP-BE-08**: Chạy `dotnet ef database update`.

#### Nhóm 3: Feature Layer (Service + Controller)

- [ ] **IMP-BE-09**: Tạo folder `Features/Import/` với cấu trúc:
  ```
  Features/Import/
  ├── DTOs/
  │   ├── ImportReceiptDto.cs
  │   ├── ImportReceiptSummaryDto.cs
  │   ├── ImportDetailDto.cs
  │   ├── CreateImportReceiptRequest.cs
  │   ├── AddImportDetailRequest.cs
  │   └── ConfirmImportReceiptResponse.cs
  ├── IImportReceiptService.cs
  ├── ImportReceiptService.cs
  └── ImportReceiptController.cs
  ```
- [ ] **IMP-BE-10**: Implement `ImportReceiptService.GenerateReceiptCodeAsync()` — sinh mã phiếu `IMP-YYYYMMDD-XXXX` tự động.
- [ ] **IMP-BE-11**: Implement `ImportReceiptService.CreateImportReceiptAsync()` — tạo phiếu mới, validate Supplier và Branch.
- [ ] **IMP-BE-12**: Implement `ImportReceiptService.GetImportReceiptsAsync()` — danh sách có filter và phân trang.
- [ ] **IMP-BE-13**: Implement `ImportReceiptService.GetImportReceiptByIdAsync()` — chi tiết phiếu kèm ImportDetails.
- [ ] **IMP-BE-14**: Implement `ImportReceiptService.AddImportDetailAsync()`:
  - Validate phiếu `Status = Draft`.
  - Validate Product tồn tại và Active.
  - Validate `ExpiryDate > Today + 7 ngày` (nếu có HSD).
  - Tính `SubTotal = Quantity × CostPrice`.
  - Cập nhật `ImportReceipt.TotalAmount`.
- [ ] **IMP-BE-15**: Implement `ImportReceiptService.ConfirmImportReceiptAsync()`:
  - Validate `Status = Draft` và `ImportDetails.Count > 0`.
  - Thực thi trong Transaction.
  - Gọi `IInventoryService.AddStockAsync()` cho mỗi ImportDetail.
  - Cập nhật `Status = Confirmed`, `ConfirmedAt`, `ConfirmedByUserId`.
- [ ] **IMP-BE-16**: Implement `ImportReceiptService.CancelImportReceiptAsync()`:
  - Validate `Status = Draft`.
  - Cập nhật `Status = Cancelled`.
  - Không cần rollback tồn kho.
- [ ] **IMP-BE-17**: Implement `ImportReceiptController` với đầy đủ 6 endpoints theo `API.md`.
- [ ] **IMP-BE-18**: Thêm `[Authorize(Roles = "Admin,Manager")]` trên toàn bộ `ImportReceiptController`.

---

### 2.2. Task WinForms Developer

#### Form danh sách phiếu nhập

- [ ] **IMP-WIN-01**: Tạo Form `frmImportReceiptList.cs`:
  - DataGridView hiển thị: Mã phiếu, Nhà cung cấp, Ngày nhập, Tổng tiền, Trạng thái, Người tạo.
  - Màu sắc trạng thái: `Draft` = xanh dương, `Confirmed` = xanh lá, `Cancelled` = xám.
  - Bộ lọc: Theo ngày, nhà cung cấp, trạng thái.
  - Nút "Tạo phiếu nhập" và "Xem chi tiết".

#### Form tạo & chỉnh sửa phiếu nhập

- [ ] **IMP-WIN-02**: Tạo Form `frmImportReceiptCreate.cs`:
  - Dropdown chọn Nhà cung cấp (gọi API `/api/suppliers`).
  - Dropdown chọn Chi nhánh (lấy từ JWT Claims).
  - TextBox Ghi chú.
  - Nút "Tạo phiếu" → Gọi `POST /api/import-receipts`.
- [ ] **IMP-WIN-03**: Tạo Form `frmImportReceiptDetail.cs` (xem + chỉnh sửa Draft):
  - Hiển thị thông tin phiếu và trạng thái.
  - DataGridView danh sách sản phẩm (ImportDetails).
  - Khi Draft: Nút "Thêm sản phẩm", "Xóa dòng", "Xác nhận phiếu", "Hủy phiếu".
  - Khi Confirmed/Cancelled: Giao diện readonly, không có nút chỉnh sửa.
- [ ] **IMP-WIN-04**: Tạo Dialog `frmAddImportDetail.cs`:
  - Search sản phẩm theo tên hoặc barcode (gọi API `/api/products`).
  - Nhập số lượng, giá vốn, hạn sử dụng (DatePicker).
  - Validate HSD ngay trên UI trước khi gọi API.
  - Hiển thị SubTotal tính real-time khi nhập Số lượng và Giá vốn.

#### Confirm Dialog

- [ ] **IMP-WIN-05**: Thêm Dialog xác nhận trước khi Confirm phiếu:
  - Hiển thị tóm tắt: Số sản phẩm, Tổng tiền, Nhà cung cấp.
  - Cảnh báo: "Phiếu sau khi xác nhận không thể sửa đổi".
  - Nút "Xác nhận" và "Hủy bỏ".

---

### 2.3. Task Testing

- [ ] **IMP-TEST-01**: Unit Test `ImportReceiptService.ConfirmImportReceiptAsync()`:
  - Test case: Confirm thành công → Inventory cập nhật đúng.
  - Test case: Confirm phiếu rỗng → Ném exception.
  - Test case: Confirm phiếu đã Confirmed → Ném exception.
  - Test case: Lỗi giữa chừng → Transaction rollback, Status vẫn = Draft.
- [ ] **IMP-TEST-02**: Unit Test `ImportReceiptService.AddImportDetailAsync()`:
  - Test case: HSD còn 3 ngày → Ném ValidationException.
  - Test case: HSD còn 10 ngày → Thêm thành công.
  - Test case: Thêm vào phiếu Confirmed → Ném exception.
  - Test case: SubTotal tính đúng = Quantity × CostPrice.
- [ ] **IMP-TEST-03**: Unit Test `GenerateReceiptCodeAsync()`:
  - Test case: Ngày 11/09/2026, phiếu đầu tiên → `IMP-20260911-0001`.
  - Test case: Ngày 11/09/2026, phiếu thứ 15 → `IMP-20260911-0015`.
- [ ] **IMP-TEST-04**: Integration Test: Tạo Draft → Thêm 2 ImportDetail → Confirm → Kiểm tra Inventory và StockHistory.
- [ ] **IMP-TEST-05**: Tạo file `SmartSupermarket_Import_Postman_Collection.json` test toàn bộ 6 endpoints.

---

### 2.4. Thứ tự ưu tiên triển khai

```
Phụ thuộc trước (từ 05_Inventory):
INV-BE-01 → INV-BE-16 (AddStockAsync) phải xong trước

Tuần 3 (Domain + Migration):
IMP-BE-01 → IMP-BE-02 → IMP-BE-03
IMP-BE-04 → IMP-BE-05 → IMP-BE-06 → IMP-BE-07 → IMP-BE-08

Tuần 4 (Core Service):
IMP-BE-09 → IMP-BE-10 → IMP-BE-11
IMP-BE-12 → IMP-BE-13 → IMP-BE-14 → IMP-BE-15 → IMP-BE-16
IMP-BE-17 → IMP-BE-18

Tuần 5-6 (WinForms UI):
IMP-WIN-01 → IMP-WIN-02 → IMP-WIN-03 → IMP-WIN-04 → IMP-WIN-05

Tuần 7 (Testing):
IMP-TEST-01 → IMP-TEST-02 → IMP-TEST-03 → IMP-TEST-04 → IMP-TEST-05
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần tính năng sửa `ImportDetail` (không chỉ thêm/xóa) trong phiếu Draft không? | Ảnh hưởng API và UX WinForms. | Đề xuất: Có — thêm `PUT /api/import-receipts/{id}/details/{detailId}` để sửa số lượng, giá vốn, HSD khi phiếu còn Draft. |

---

## 4. GHI CHÚ
- Task **IMP-BE-15** (ConfirmImportReceiptAsync) là task phức tạp nhất — cần test kỹ với nhiều scenario Transaction failure.
- WinForms Form `frmImportReceiptDetail` cần xử lý 2 mode: Chỉnh sửa (Draft) và Readonly (Confirmed/Cancelled).
- Commit message gợi ý: `feat: add import receipt service with confirm logic`, `feat: add winforms import receipt form`.

---

## 5. KẾT LUẬN

Tài liệu `Tasks.md` đã liệt kê đầy đủ **18 Backend tasks**, **5 WinForms tasks** và **5 Testing tasks** cho phân hệ `06_Import`. Task quan trọng nhất là **IMP-BE-15** (Confirm với Transaction) và **IMP-TEST-01** (Unit test Transaction rollback). Phân hệ này khi hoàn thiện sẽ kết nối liền mạch với `05_Inventory` để hình thành chuỗi cung ứng hoàn chỉnh: Nhà cung cấp → Phiếu nhập → Kho hàng trong Smart SuperMarket.
