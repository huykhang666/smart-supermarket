# SƠ ĐỒ TUẦN TỰ QUẢN LÝ TỒN KHO (INVENTORY SEQUENCE DIAGRAM SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Luồng: Xác nhận Phiếu nhập → Cập nhật Tồn kho](#21-luồng-xác-nhận-phiếu-nhập--cập-nhật-tồn-kho)
   - 2.2. [Luồng: Bán hàng POS → Trừ tồn kho theo FEFO](#22-luồng-bán-hàng-pos--trừ-tồn-kho-theo-fefo)
   - 2.3. [Luồng: Cảnh báo tự động khi tồn kho thấp](#23-luồng-cảnh-báo-tự-động-khi-tồn-kho-thấp)
   - 2.4. [Luồng: AI phát hiện sắp hết hạn → Đề xuất DiscountRule](#24-luồng-ai-phát-hiện-sắp-hết-hạn--đề-xuất-discountrule)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **SequenceDiagram.md** mô tả bằng sơ đồ tuần tự (Sequence Diagram) các tương tác theo thời gian giữa người dùng, WinForms Desktop, Backend API, Services và SQL Server cho các luồng nghiệp vụ chính của phân hệ `05_Inventory`.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Luồng: Xác nhận Phiếu nhập → Cập nhật Tồn kho

Khi Manager xác nhận phiếu nhập hàng (`Confirmed`), hệ thống tự động cập nhật tồn kho và ghi lịch sử.

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Manager (WinForms)
    participant ImportCtrl as ImportReceiptController
    participant ImportSvc as ImportReceiptService
    participant InvSvc as InventoryService
    participant DB as SQL Server

    Manager->>ImportCtrl: POST /api/import-receipts/{id}/confirm
    ImportCtrl->>ImportSvc: ConfirmImportReceiptAsync(importReceiptId)
    
    ImportSvc->>DB: Lấy ImportReceipt kèm ImportDetails
    DB-->>ImportSvc: ImportReceipt {Status=Draft, Details=[...]}
    
    alt Phiếu không ở trạng thái Draft
        ImportSvc-->>ImportCtrl: Ném InvalidOperationException
        ImportCtrl-->>Manager: 400 Bad Request - "Chỉ phiếu Draft mới được xác nhận"
    end

    ImportSvc->>DB: BEGIN TRANSACTION
    ImportSvc->>DB: UPDATE ImportReceipt SET Status=Confirmed

    loop Từng dòng ImportDetail
        ImportSvc->>InvSvc: AddStockAsync(productId, branchId, quantity, expiryDate)
        InvSvc->>DB: SELECT Inventory WHERE ProductId=X AND BranchId=Y
        
        alt Chưa có bản ghi Inventory
            InvSvc->>DB: INSERT INTO Inventory (ProductId, BranchId, QuantityOnHand=qty, MinStockLevel=10)
        else Đã có bản ghi Inventory
            InvSvc->>DB: UPDATE Inventory SET QuantityOnHand += qty, LastUpdated=NOW
        end

        InvSvc->>DB: INSERT INTO StockHistory (ChangeType=Import, QuantityChange=+qty, ExpiryDate=...)
    end

    ImportSvc->>DB: COMMIT TRANSACTION
    DB-->>ImportSvc: Thành công
    ImportSvc-->>ImportCtrl: Trả về ImportReceipt đã Confirmed
    ImportCtrl-->>Manager: 200 OK - "Xác nhận phiếu nhập thành công, đã cập nhật tồn kho"
```

---

### 2.2. Luồng: Bán hàng POS → Trừ tồn kho theo FEFO

```mermaid
sequenceDiagram
    autonumber
    actor Cashier as Thu ngân (Staff - WinForms POS)
    participant OrderCtrl as OrderController
    participant OrderSvc as OrderService
    participant InvSvc as InventoryService
    participant DiscSvc as DiscountRuleService
    participant DB as SQL Server

    Cashier->>OrderCtrl: POST /api/orders (danh sách sản phẩm đã quét)
    OrderCtrl->>OrderSvc: CreateOrderAsync(orderRequest)

    loop Từng sản phẩm trong giỏ hàng
        OrderSvc->>InvSvc: CheckAndDeductStockFEFOAsync(productId, branchId, quantity)
        
        InvSvc->>DB: Lấy tồn kho: SELECT QuantityOnHand FROM Inventory WHERE ProductId=X AND BranchId=Y
        DB-->>InvSvc: QuantityOnHand = 54

        alt Không đủ hàng (QuantityOnHand < quantity)
            InvSvc-->>OrderSvc: Ném InsufficientStockException
            OrderSvc-->>OrderCtrl: Rollback
            OrderCtrl-->>Cashier: 400 - "Pepsi 330ml không đủ số lượng tồn kho"
        end

        InvSvc->>DB: Query FEFO: SELECT từ StockHistory WHERE ChangeType=Import AND ExpiryDate > TODAY ORDER BY ExpiryDate ASC
        DB-->>InvSvc: [Lô A: 4 chai HSD 18/09] [Lô B: 50 chai HSD 31/12]

        InvSvc->>DiscSvc: GetApplicableDiscountAsync(expiryDate=18/09)
        DiscSvc-->>InvSvc: DiscountPercent = 50% (vì còn 7 ngày)

        InvSvc->>DB: BEGIN TRANSACTION
        InvSvc->>DB: UPDATE Inventory SET QuantityOnHand -= quantity
        InvSvc->>DB: INSERT StockHistory (ChangeType=Sale, QuantityChange=-qty, ExpiryDate=18/09)
        InvSvc->>DB: COMMIT
    end

    OrderSvc->>DB: INSERT INTO Order + OrderDetails
    OrderSvc-->>OrderCtrl: Order tạo thành công
    OrderCtrl-->>Cashier: 201 Created - Hóa đơn + thông tin giảm giá FEFO
```

---

### 2.3. Luồng: Cảnh báo tự động khi tồn kho thấp

```mermaid
sequenceDiagram
    autonumber
    participant Scheduler as Background Job (Scheduler)
    participant InvSvc as InventoryService
    participant DB as SQL Server
    participant Dashboard as WinForms Dashboard

    Note over Scheduler: Chạy mỗi khi mở Dashboard hoặc theo schedule
    
    Scheduler->>InvSvc: GetLowStockProductsAsync(branchId)
    InvSvc->>DB: SELECT * FROM Inventory WHERE QuantityOnHand <= MinStockLevel AND BranchId=X
    DB-->>InvSvc: [Pepsi: 4/10, Bánh Oreo: 2/15, ...]
    InvSvc-->>Scheduler: Danh sách sản phẩm LowStock

    Scheduler->>Dashboard: Cập nhật widget "Sắp hết hàng" trên Dashboard

    Dashboard-->>Dashboard: Hiển thị màu ĐỎ cho sản phẩm QuantityOnHand <= MinStockLevel
    Dashboard-->>Dashboard: Hiển thị badge số lượng sản phẩm cần nhập thêm

    Note over Dashboard: Manager thấy cảnh báo → Click "Tạo phiếu nhập nhanh" → Chuyển sang 06_Import
```

---

### 2.4. Luồng: AI phát hiện sắp hết hạn → Đề xuất DiscountRule

```mermaid
sequenceDiagram
    autonumber
    participant Scheduler as Background Job / Dashboard Load
    participant InvSvc as InventoryService
    participant AISvc as AIService (Gemini API)
    participant DB as SQL Server
    participant Dashboard as WinForms Dashboard

    Scheduler->>InvSvc: GetExpiringProductsAsync(branchId, withinDays=15)
    InvSvc->>DB: Query StockHistory lấy lô hàng có ExpiryDate trong 15 ngày tới
    DB-->>InvSvc: [Pepsi 4 chai HSD 18/09 - 7 ngày, Sữa Vinamilk 15 hộp HSD 24/09 - 13 ngày]
    InvSvc-->>Scheduler: Danh sách sản phẩm sắp hết hạn

    Scheduler->>InvSvc: GetApplicableDiscountRules()
    InvSvc->>DB: SELECT * FROM DiscountRule WHERE IsActive=1 ORDER BY DaysBeforeExpiry ASC
    DB-->>InvSvc: [7 ngày → 50%, 15 ngày → 20%]

    Scheduler->>AISvc: AnalyzeExpiringStockAsync(expiringProducts, discountRules)
    AISvc->>AISvc: Gọi Gemini API với prompt phân tích tồn kho sắp hết hạn
    AISvc-->>Scheduler: Đề xuất: "Giảm 50% Pepsi, Giảm 20% Sữa Vinamilk"

    Scheduler->>Dashboard: Hiển thị widget "Cảnh báo HSD" kèm đề xuất AI

    Dashboard-->>Dashboard: Màu ĐỎ cho ≤ 7 ngày, màu VÀNG cho ≤ 15 ngày
    Dashboard-->>Dashboard: Nút "Áp dụng khuyến mãi đề xuất" → Chuyển sang 08_Promotion
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Luồng cảnh báo tồn kho thấp nên chạy theo schedule định kỳ (background service) hay chỉ khi mở Dashboard? | Ảnh hưởng kiến trúc backend. | Đề xuất v1: Chỉ khi mở Dashboard (đơn giản hơn). v2: Thêm `IHostedService` chạy mỗi giờ gửi cảnh báo. |

---

## 4. GHI CHÚ
- Tất cả các luồng cập nhật tồn kho (2.1, 2.2) đều phải thực hiện trong Database Transaction để đảm bảo tính ACID.
- Luồng 2.4 (AI đề xuất) hoạt động bất đồng bộ — không block UI khi đang load Dashboard.
- Tham chiếu thêm `FEFO.md` để hiểu chi tiết thuật toán chọn lô hàng theo HSD trong luồng 2.2.

---

## 5. KẾT LUẬN

Tài liệu `SequenceDiagram.md` đã mô tả 4 luồng tuần tự chính của phân hệ `05_Inventory`. Các sơ đồ này là cơ sở để Backend Developer triển khai đúng thứ tự gọi Service, xử lý Transaction và tích hợp với module AI cho phân hệ Quản lý Tồn kho Smart SuperMarket.
