# QUY TẮC NGHIỆP VỤ QUẢN LÝ TỒN KHO (INVENTORY BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Quy tắc Khởi tạo & Cập nhật Tồn kho](#21-quy-tắc-khởi-tạo--cập-nhật-tồn-kho)
   - 2.2. [Quy tắc Cảnh báo Tồn kho Thấp (Low Stock Alert)](#22-quy-tắc-cảnh-báo-tồn-kho-thấp-low-stock-alert)
   - 2.3. [Quy tắc Cảnh báo Sắp Hết Hạn Sử Dụng (Expiry Warning)](#23-quy-tắc-cảnh-báo-sắp-hết-hạn-sử-dụng-expiry-warning)
   - 2.4. [Quy tắc FEFO — First Expired, First Out](#24-quy-tắc-fefo--first-expired-first-out)
   - 2.5. [Quy tắc Giảm giá Tự động theo HSD (DiscountRule)](#25-quy-tắc-giảm-giá-tự-động-theo-hsd-discountrule)
   - 2.6. [Quy tắc Ghi Lịch sử Biến động (StockHistory)](#26-quy-tắc-ghi-lịch-sử-biến-động-stockhistory)
   - 2.7. [Quy tắc Điều chỉnh Tồn kho Thủ công](#27-quy-tắc-điều-chỉnh-tồn-kho-thủ-công)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules — BR) liên quan đến quản lý tồn kho, cảnh báo hàng tồn thấp, quản lý hạn sử dụng, áp dụng giảm giá theo HSD và ghi nhận lịch sử biến động trong hệ thống **Smart SuperMarket**.

Tất cả lập trình viên Backend, WinForms Desktop phải tuân thủ chính xác các quy tắc này khi viết mã nguồn và xây dựng luồng xử lý.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Quy tắc Khởi tạo & Cập nhật Tồn kho

- **BR-INV-01 (Khởi tạo tồn kho khi thêm sản phẩm mới)**: Khi Admin thêm sản phẩm mới vào hệ thống, hệ thống tự động tạo bản ghi `Inventory` với `QuantityOnHand = 0` và `MinStockLevel = 10` cho mỗi chi nhánh đang hoạt động.
- **BR-INV-02 (Tồn kho không âm)**: `QuantityOnHand` **TUYỆT ĐỐI KHÔNG ĐƯỢC ÂM**. Tầng Service phải kiểm tra `QuantityOnHand >= QuantityToDeduct` trước khi thực hiện bất kỳ thao tác trừ kho nào. Nếu vi phạm, ném `InvalidOperationException` và hủy toàn bộ transaction.
- **BR-INV-03 (Cập nhật atomic)**: Mọi thao tác cập nhật `Inventory.QuantityOnHand` phải được thực hiện trong một **Database Transaction** duy nhất, kèm theo ghi bản ghi `StockHistory` tương ứng. Không cho phép cập nhật tồn kho mà không có lịch sử.
- **BR-INV-04 (Phạm vi chi nhánh)**: Tồn kho chỉ được cập nhật trong phạm vi chi nhánh (`BranchId`) của nhân viên đang thực hiện. Staff không thể điều chỉnh tồn kho của chi nhánh khác.

---

### 2.2. Quy tắc Cảnh báo Tồn kho Thấp (Low Stock Alert)

- **BR-INV-05 (Ngưỡng cảnh báo)**: Khi `QuantityOnHand <= MinStockLevel`, sản phẩm được đánh dấu trạng thái **"Low Stock"** và hiển thị màu **ĐỎ** trong giao diện WinForms.
- **BR-INV-06 (Hiển thị Dashboard)**: Dashboard (12_Report) phải liệt kê tối đa **10 sản phẩm sắp hết hàng nhất** (theo `QuantityOnHand ASC`), kèm nút "Tạo phiếu nhập nhanh".
- **BR-INV-07 (Thông báo AI)**: Khi phát hiện sản phẩm `LowStock`, module AI (10_AI) sẽ tự động đề xuất số lượng nhập hàng dựa trên lịch sử bán hàng 7 ngày gần nhất.
- **BR-INV-08 (Cấu hình MinStockLevel)**: Chỉ Admin và Manager mới được phép thay đổi `MinStockLevel` của từng sản phẩm tại chi nhánh. Giá trị tối thiểu cho phép là 1.

---

### 2.3. Quy tắc Cảnh báo Sắp Hết Hạn Sử Dụng (Expiry Warning)

- **BR-INV-09 (Nguồn dữ liệu HSD)**: Hạn sử dụng được lấy từ trường `ExpiryDate` của bảng `ImportDetail` (lô hàng nhập vào). Hệ thống cần tổng hợp HSD gần nhất còn tồn kho cho mỗi sản phẩm.
- **BR-INV-10 (Cảnh báo 15 ngày)**: Sản phẩm có lô hàng còn **≤ 15 ngày** đến HSD → Hiển thị màu **VÀNG** — cảnh báo trung bình, nên đẩy bán.
- **BR-INV-11 (Cảnh báo 7 ngày)**: Sản phẩm có lô hàng còn **≤ 7 ngày** đến HSD → Hiển thị màu **ĐỎ** — cảnh báo khẩn cấp, cần hành động ngay.
- **BR-INV-12 (Tự động áp DiscountRule)**: Khi phát hiện sản phẩm trong vùng cảnh báo HSD, AI tự động đề xuất áp dụng `DiscountRule` phù hợp. Nhân viên hoặc Manager cần xác nhận trước khi áp dụng.
- **BR-INV-13 (Hủy hàng hết hạn)**: Sản phẩm đã **quá HSD** không được phép bán. Hệ thống phải tự động loại khỏi màn hình POS và nhắc Admin/Manager xác nhận hủy, ghi `StockHistory` với `ChangeType = Expired`.

---

### 2.4. Quy tắc FEFO — First Expired, First Out

- **BR-INV-14 (Nguyên tắc FEFO)**: Khi xuất kho (bán hàng hoặc điều chuyển), hệ thống **BẮT BUỘC** ưu tiên xuất lô hàng có `ExpiryDate` sớm nhất trước. Không cho phép xuất lô hàng có HSD xa hơn khi vẫn còn lô hàng có HSD gần hơn.
- **BR-INV-15 (FEFO với sản phẩm không có HSD)**: Sản phẩm không có `ExpiryDate` (ví dụ: đồ gia dụng, vật dụng không thực phẩm) được xử lý theo FIFO (First In, First Out) — xuất theo thứ tự nhập trước.
- **BR-INV-16 (Tự động, không thủ công)**: Thuật toán FEFO được thực thi tự động ở tầng Service — nhân viên bán hàng không cần và không được can thiệp thứ tự xuất kho.

---

### 2.5. Quy tắc Giảm giá Tự động theo HSD (DiscountRule)

- **BR-INV-17 (Áp dụng DiscountRule)**: Hệ thống tự động tính giá bán thực tế của sản phẩm dựa trên HSD của lô hàng đang xuất (theo FEFO) và so khớp với `DiscountRule` đang `IsActive = true`.
- **BR-INV-18 (Ưu tiên quy tắc khắt khe hơn)**: Nếu 1 sản phẩm thỏa mãn nhiều `DiscountRule` (vừa ≤ 7 ngày vừa ≤ 15 ngày), áp dụng quy tắc có `DiscountPercent` **cao hơn** (quy tắc ≤ 7 ngày: 50% > quy tắc ≤ 15 ngày: 20%).
- **BR-INV-19 (Chỉ Admin cấu hình DiscountRule)**: Chỉ tài khoản Admin mới được thêm, sửa, bật/tắt các `DiscountRule`. Manager và Staff chỉ xem.
- **BR-INV-20 (Không giảm giá âm)**: Giá sau giảm phải luôn > 0. Hệ thống không cho phép `DiscountPercent = 100`.

---

### 2.6. Quy tắc Ghi Lịch sử Biến động (StockHistory)

- **BR-INV-21 (Bắt buộc ghi lịch sử)**: Mọi thay đổi `QuantityOnHand` (dù tăng hay giảm) đều phải tạo ra ít nhất 1 bản ghi trong `StockHistory`. Không có ngoại lệ.
- **BR-INV-22 (Không sửa/xóa lịch sử)**: `StockHistory` là bảng **append-only**. Mọi thao tác UPDATE hoặc DELETE đều bị cấm ở tầng Service và Repository.
- **BR-INV-23 (ReferenceId)**: Khi `ChangeType = Import`, `ReferenceId` là `ImportReceiptId`. Khi `ChangeType = Sale`, `ReferenceId` là `OrderId`. Khi `ChangeType = Adjustment` hoặc `Expired`, `ReferenceId = NULL`.
- **BR-INV-24 (CreatedByUserId bắt buộc)**: Mọi bản ghi `StockHistory` phải có `CreatedByUserId` — không cho phép ghi lịch sử ẩn danh.

---

### 2.7. Quy tắc Điều chỉnh Tồn kho Thủ công

- **BR-INV-25 (Phân quyền điều chỉnh)**: Chỉ Admin và Manager được thực hiện điều chỉnh tồn kho thủ công (`ChangeType = Adjustment`). Staff không có quyền.
- **BR-INV-26 (Lý do bắt buộc)**: Khi điều chỉnh thủ công, trường `Note` trong `StockHistory` là **bắt buộc** — phải ghi rõ lý do (ví dụ: "Kiểm kê thực tế thiếu 3 chai Pepsi").
- **BR-INV-27 (Audit trail)**: Thao tác điều chỉnh phải ghi đầy đủ `QuantityBefore`, `QuantityChange`, `QuantityAfter`, `CreatedByUserId` và `CreatedAt` để phục vụ audit sau này.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần gửi thông báo email/push notification cho Manager khi phát hiện sản phẩm `LowStock` không? | Ảnh hưởng hạ tầng notification. | Đề xuất v1: Chỉ hiển thị cảnh báo trong giao diện WinForms. v2: Tích hợp email notification. |
| 2 | `DiscountRule` áp dụng tự động khi bán hàng POS hay chỉ hiển thị đề xuất để nhân viên xác nhận? | Ảnh hưởng UX màn hình POS. | Đề xuất: Tự động áp dụng tại POS — giá bán đã tính giảm sẵn khi quét mã. Nhân viên thấy giá đã giảm. |

---

## 4. GHI CHÚ
- Tất cả thao tác cập nhật tồn kho phải dùng `DbContext.Database.BeginTransactionAsync()` để đảm bảo tính ACID.
- Khi viết Service method cập nhật tồn kho, luôn đặt `LastUpdated = DateTime.UtcNow` trước khi `SaveChangesAsync()`.
- Mã hóa `ChangeType` dùng Enum `StockChangeType` — không dùng magic number trực tiếp trong code.

---

## 5. KẾT LUẬN

Tài liệu `BusinessRules.md` đã xác lập đầy đủ 27 quy tắc nghiệp vụ cho phân hệ `05_Inventory`. Việc tuân thủ nghiêm ngặt các quy tắc này — đặc biệt BR-INV-02 (không âm tồn kho), BR-INV-03 (atomic transaction), BR-INV-14 (FEFO) và BR-INV-21 (ghi lịch sử bắt buộc) — là nền tảng đảm bảo tính chính xác và minh bạch của dữ liệu tồn kho trong hệ thống Smart SuperMarket.
