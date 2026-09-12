# SƠ ĐỒ TUẦN TỰ QUẢN LÝ NHẬP HÀNG (IMPORT SEQUENCE DIAGRAM SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Luồng: Tạo phiếu nhập & Thêm chi tiết sản phẩm (Draft)](#21-luồng-tạo-phiếu-nhập--thêm-chi-tiết-sản-phẩm-draft)
   - 2.2. [Luồng: Xác nhận phiếu nhập → Cập nhật tồn kho](#22-luồng-xác-nhận-phiếu-nhập--cập-nhật-tồn-kho)
   - 2.3. [Luồng: Hủy phiếu nhập (Cancel Draft)](#23-luồng-hủy-phiếu-nhập-cancel-draft)
   - 2.4. [Luồng: Validate HSD khi thêm ImportDetail](#24-luồng-validate-hsd-khi-thêm-importdetail)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **SequenceDiagram.md** mô tả bằng sơ đồ tuần tự (Sequence Diagram) các tương tác theo thời gian giữa Manager, WinForms Desktop, Backend API, Services và SQL Server cho các luồng nghiệp vụ chính của phân hệ `06_Import`.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Luồng: Tạo phiếu nhập & Thêm chi tiết sản phẩm (Draft)

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Manager (WinForms Desktop)
    participant ImportCtrl as ImportReceiptController
    participant ImportSvc as ImportReceiptService
    participant DB as SQL Server

    Note over Manager: Bước 1: Tạo phiếu nhập mới

    Manager->>ImportCtrl: POST /api/import-receipts\n{supplierId, branchId, note}
    ImportCtrl->>ImportSvc: CreateImportReceiptAsync(request, userId)

    ImportSvc->>DB: Kiểm tra Supplier tồn tại và Active
    DB-->>ImportSvc: Supplier hợp lệ

    ImportSvc->>DB: Đếm phiếu trong ngày để sinh ReceiptCode
    DB-->>ImportSvc: countToday = 2
    Note over ImportSvc: ReceiptCode = "IMP-20260911-0003"

    ImportSvc->>DB: INSERT INTO ImportReceipt\n{ReceiptCode, SupplierId, BranchId, Status=Draft, TotalAmount=0}
    DB-->>ImportSvc: ImportReceiptId = 17

    ImportSvc-->>ImportCtrl: Trả về ImportReceipt đã tạo
    ImportCtrl-->>Manager: 201 Created - Phiếu IMP-20260911-0003 tạo thành công

    Note over Manager: Bước 2: Thêm sản phẩm vào phiếu

    loop Từng sản phẩm cần nhập
        Manager->>ImportCtrl: POST /api/import-receipts/17/details\n{productId, quantity, costPrice, expiryDate}
        ImportCtrl->>ImportSvc: AddImportDetailAsync(importReceiptId=17, request)

        ImportSvc->>DB: Kiểm tra phiếu tồn tại và Status=Draft
        DB-->>ImportSvc: ImportReceipt {Status=Draft}

        ImportSvc->>DB: Kiểm tra Product tồn tại và Active
        DB-->>ImportSvc: Product hợp lệ

        ImportSvc->>ImportSvc: Validate ExpiryDate\n(phải > Today + 7 ngày)
        ImportSvc->>ImportSvc: Tính SubTotal = Quantity × CostPrice

        ImportSvc->>DB: INSERT INTO ImportDetail
        ImportSvc->>DB: UPDATE ImportReceipt SET TotalAmount += SubTotal

        ImportSvc-->>ImportCtrl: Trả về ImportDetail đã thêm
        ImportCtrl-->>Manager: 201 Created - Thêm sản phẩm thành công
    end
```

---

### 2.2. Luồng: Xác nhận phiếu nhập → Cập nhật tồn kho

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Manager (WinForms Desktop)
    participant ImportCtrl as ImportReceiptController
    participant ImportSvc as ImportReceiptService
    participant InvSvc as InventoryService
    participant DB as SQL Server

    Manager->>ImportCtrl: POST /api/import-receipts/17/confirm
    ImportCtrl->>ImportSvc: ConfirmImportReceiptAsync(importReceiptId=17, confirmedByUserId)

    ImportSvc->>DB: Lấy ImportReceipt kèm toàn bộ ImportDetails
    DB-->>ImportSvc: ImportReceipt {Status=Draft, Details=[3 dòng]}

    alt Phiếu không phải Draft
        ImportSvc-->>ImportCtrl: Ném InvalidOperationException
        ImportCtrl-->>Manager: 400 - "Chỉ phiếu Draft mới được xác nhận"
    end

    alt Phiếu không có ImportDetail nào
        ImportSvc-->>ImportCtrl: Ném InvalidOperationException
        ImportCtrl-->>Manager: 400 - "Phiếu nhập cần ít nhất 1 sản phẩm"
    end

    ImportSvc->>DB: BEGIN TRANSACTION

    ImportSvc->>DB: UPDATE ImportReceipt\nSET Status=Confirmed, ConfirmedAt=NOW, ConfirmedByUserId=X

    loop Từng ImportDetail trong phiếu
        ImportSvc->>InvSvc: AddStockAsync(productId, branchId, quantity, expiryDate, receiptId, userId)

        InvSvc->>DB: SELECT FROM Inventory WHERE ProductId=X AND BranchId=Y FOR UPDATE

        alt Chưa có bản ghi Inventory
            InvSvc->>DB: INSERT INTO Inventory\n{ProductId, BranchId, QuantityOnHand=qty, MinStockLevel=10}
        else Đã có bản ghi
            InvSvc->>DB: UPDATE Inventory\nSET QuantityOnHand += qty, LastUpdated=NOW
        end

        InvSvc->>DB: INSERT INTO StockHistory\n{ChangeType=Import, QuantityChange=+qty,\nExpiryDate=..., ReferenceId=ReceiptId}
    end

    ImportSvc->>DB: COMMIT TRANSACTION
    DB-->>ImportSvc: Thành công

    ImportSvc-->>ImportCtrl: Trả về kết quả Confirm kèm danh sách inventory đã cập nhật
    ImportCtrl-->>Manager: 200 OK - "Xác nhận thành công, đã cập nhật 3 sản phẩm vào kho"
```

---

### 2.3. Luồng: Hủy phiếu nhập (Cancel Draft)

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Manager (WinForms Desktop)
    participant ImportCtrl as ImportReceiptController
    participant ImportSvc as ImportReceiptService
    participant DB as SQL Server

    Manager->>ImportCtrl: DELETE /api/import-receipts/16
    ImportCtrl->>ImportSvc: CancelImportReceiptAsync(importReceiptId=16, userId)

    ImportSvc->>DB: Lấy ImportReceipt theo ID
    DB-->>ImportSvc: ImportReceipt {Status=Draft}

    alt Phiếu không phải Draft
        ImportSvc-->>ImportCtrl: Ném InvalidOperationException
        ImportCtrl-->>Manager: 400 - "Chỉ phiếu Draft mới được hủy.\nPhiếu đã Confirmed không thể hủy"
    end

    Note over ImportSvc: Phiếu Draft chưa ảnh hưởng tồn kho\n→ Chỉ cần đổi Status, không cần rollback kho

    ImportSvc->>DB: UPDATE ImportReceipt SET Status=Cancelled
    DB-->>ImportSvc: Thành công

    ImportSvc-->>ImportCtrl: Trả về phiếu đã Cancelled
    ImportCtrl-->>Manager: 200 OK - "Hủy phiếu nhập IMP-20260911-0016 thành công"
```

---

### 2.4. Luồng: Validate HSD khi thêm ImportDetail

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Manager (WinForms)
    participant ImportCtrl as ImportReceiptController
    participant ImportSvc as ImportReceiptService
    participant ValSvc as ValidationService
    participant DB as SQL Server

    Manager->>ImportCtrl: POST /api/import-receipts/17/details\n{productId=5, quantity=50, costPrice=8000, expiryDate="2026-09-14"}

    ImportCtrl->>ImportSvc: AddImportDetailAsync(...)
    ImportSvc->>DB: Kiểm tra phiếu Status=Draft ✅

    ImportSvc->>ValSvc: ValidateExpiryDateAsync(expiryDate="2026-09-14")
    
    ValSvc->>ValSvc: So sánh với DateTime.Today (11/09/2026)
    Note over ValSvc: 14/09 - 11/09 = 3 ngày\n3 ngày < 7 ngày → FAIL

    ValSvc-->>ImportSvc: ValidationException\n"HSD chỉ còn 3 ngày — tối thiểu phải còn 7 ngày"

    ImportSvc-->>ImportCtrl: Ném ValidationException
    ImportCtrl-->>Manager: 400 Bad Request\n"Hạn sử dụng 2026-09-14 quá gần (còn 3 ngày).\nHệ thống yêu cầu HSD ít nhất 7 ngày từ ngày nhập"

    Note over Manager: Manager kiểm tra lại hàng hóa\nvà nhập lại HSD đúng từ bao bì sản phẩm
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Luồng Confirm (2.2) nếu xảy ra lỗi giữa chừng (ví dụ mạng mất kết nối sau khi đã Confirm 2/3 sản phẩm), rollback toàn bộ hay giữ lại những gì đã Confirm? | Ảnh hưởng tính toàn vẹn dữ liệu. | Đề xuất: Rollback toàn bộ — Transaction đảm bảo hoặc tất cả hoặc không có gì. Dùng `BeginTransactionAsync()` và `RollbackAsync()` trong catch block. |

---

## 4. GHI CHÚ
- Tất cả luồng trong `2.2` (Confirm) phải thực hiện trong 1 Transaction bao gồm cả phần gọi `InvSvc.AddStockAsync()`.
- Sơ đồ `2.4` (Validate HSD) là validation quan trọng nhất — nếu bỏ qua, sẽ nhập vào kho hàng sắp hết hạn và gây thiệt hại ngay.
- Khi implement, validate HSD nên đặt trong `ImportReceiptService` (không phải Controller) để tái sử dụng.

---

## 5. KẾT LUẬN

Tài liệu `SequenceDiagram.md` đã mô tả 4 luồng tuần tự chính của phân hệ `06_Import`. Luồng quan trọng nhất là luồng `2.2 (Confirm → Cập nhật tồn kho)` — đây là điểm kết nối trực tiếp giữa module nhập hàng và module tồn kho. Việc thực thi đúng Transaction và validate đầy đủ đảm bảo tính toàn vẹn dữ liệu trong hệ thống Smart SuperMarket.
