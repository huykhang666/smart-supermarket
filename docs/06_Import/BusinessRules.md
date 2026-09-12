# QUY TẮC NGHIỆP VỤ QUẢN LÝ NHẬP HÀNG (IMPORT BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Quy tắc Tạo Phiếu Nhập Hàng](#21-quy-tắc-tạo-phiếu-nhập-hàng)
   - 2.2. [Quy tắc Thêm / Sửa / Xóa Chi tiết Phiếu nhập (ImportDetail)](#22-quy-tắc-thêm--sửa--xóa-chi-tiết-phiếu-nhập-importdetail)
   - 2.3. [Quy tắc Xác nhận Phiếu nhập (Confirm)](#23-quy-tắc-xác-nhận-phiếu-nhập-confirm)
   - 2.4. [Quy tắc Hủy Phiếu nhập (Cancel)](#24-quy-tắc-hủy-phiếu-nhập-cancel)
   - 2.5. [Quy tắc Tính TotalAmount](#25-quy-tắc-tính-totalamount)
   - 2.6. [Quy tắc Validate Hạn Sử Dụng khi Nhập](#26-quy-tắc-validate-hạn-sử-dụng-khi-nhập)
   - 2.7. [Quy tắc Phân Quyền](#27-quy-tắc-phân-quyền)
   - 2.8. [Quy tắc Tự động Tạo StockHistory khi Confirm](#28-quy-tắc-tự-động-tạo-stockhistory-khi-confirm)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules — BR) liên quan đến quy trình nhập hàng, vòng đời phiếu nhập, validate dữ liệu và tương tác với module tồn kho trong hệ thống **Smart SuperMarket**.

Tất cả lập trình viên Backend và WinForms Desktop phải tuân thủ chính xác các quy tắc này.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Quy tắc Tạo Phiếu Nhập Hàng

- **BR-IMP-01 (Trạng thái khởi tạo)**: Mọi phiếu nhập mới tạo đều phải ở trạng thái `Draft (1)`. Không có cách nào tạo phiếu nhập thẳng vào `Confirmed`.
- **BR-IMP-02 (Thông tin bắt buộc khi tạo)**: Phiếu nhập bắt buộc phải có: `SupplierId` (nhà cung cấp hợp lệ, đang hoạt động), `BranchId` (chi nhánh hợp lệ) và `ImportedByUserId` (lấy từ JWT Claims của người đang đăng nhập).
- **BR-IMP-03 (Tự sinh mã phiếu)**: `ReceiptCode` được hệ thống tự động sinh theo định dạng `IMP-YYYYMMDD-XXXX` — người dùng không được nhập thủ công.
- **BR-IMP-04 (Phiếu rỗng cho phép)**: Phiếu nhập `Draft` có thể tạo mà không cần có `ImportDetail` ngay — cho phép lưu nháp trước, thêm sản phẩm sau.

---

### 2.2. Quy tắc Thêm / Sửa / Xóa Chi tiết Phiếu nhập (ImportDetail)

- **BR-IMP-05 (Chỉ Draft mới chỉnh sửa được)**: Chỉ được thêm, sửa hoặc xóa `ImportDetail` khi phiếu nhập đang ở trạng thái `Draft`. Phiếu `Confirmed` hoặc `Cancelled` không cho phép bất kỳ thay đổi nào.
- **BR-IMP-06 (Số lượng nhập > 0)**: `ImportDetail.Quantity` phải là số nguyên dương (> 0). Không chấp nhận số lượng = 0 hoặc âm.
- **BR-IMP-07 (Giá vốn > 0)**: `ImportDetail.CostPrice` phải lớn hơn 0. Không nhập hàng miễn phí qua phiếu nhập (trường hợp đặc biệt dùng `Adjustment` trong 05_Inventory).
- **BR-IMP-08 (Tính SubTotal tự động)**: `SubTotal = Quantity × CostPrice` được tính và lưu tự động tại Service — không cho phép client tự truyền `SubTotal`.
- **BR-IMP-09 (Sản phẩm phải tồn tại và Active)**: `ProductId` trong `ImportDetail` phải tham chiếu tới sản phẩm đang hoạt động (`Product.Status = Active`). Không cho nhập hàng cho sản phẩm đã bị vô hiệu hóa.
- **BR-IMP-10 (Cùng phiếu, nhiều lô cùng sản phẩm)**: Một phiếu nhập có thể có nhiều dòng `ImportDetail` với cùng `ProductId` nhưng `ExpiryDate` khác nhau — đây là hợp lệ và cần thiết để quản lý nhiều lô HSD.

---

### 2.3. Quy tắc Xác nhận Phiếu nhập (Confirm)

- **BR-IMP-11 (Phiếu phải có ít nhất 1 ImportDetail)**: Không được phép xác nhận phiếu nhập rỗng (không có dòng chi tiết nào). Hệ thống phải kiểm tra `ImportDetails.Count > 0` trước khi Confirm.
- **BR-IMP-12 (Chỉ Confirm từ Draft)**: Chỉ phiếu đang ở trạng thái `Draft (1)` mới được Confirm. Phiếu `Confirmed` hoặc `Cancelled` không thể Confirm lại.
- **BR-IMP-13 (Cập nhật tồn kho atomic)**: Khi Confirm phiếu, tất cả thao tác sau phải thực hiện trong **1 Database Transaction** duy nhất:
  1. Cập nhật `ImportReceipt.Status = Confirmed`.
  2. Cập nhật `ImportReceipt.ConfirmedAt = DateTime.UtcNow`.
  3. Cập nhật `ImportReceipt.ConfirmedByUserId` = UserId từ JWT.
  4. Với mỗi `ImportDetail`: Cộng tồn kho + ghi `StockHistory`.
  Nếu bất kỳ bước nào thất bại → Rollback toàn bộ.
- **BR-IMP-14 (Phiếu Confirmed là bất biến)**: Sau khi `Confirmed`, không ai được sửa, xóa `ImportReceipt` hoặc các `ImportDetail` liên quan — kể cả Admin. Đây là dữ liệu kế toán.

---

### 2.4. Quy tắc Hủy Phiếu nhập (Cancel)

- **BR-IMP-15 (Chỉ Cancel từ Draft)**: Chỉ phiếu đang `Draft` mới được hủy. Phiếu `Confirmed` **KHÔNG THỂ HỦY** — vì tồn kho đã được cập nhật.
- **BR-IMP-16 (Hủy không ảnh hưởng tồn kho)**: Hủy phiếu `Draft` chỉ đổi `Status = Cancelled`, không cần rollback tồn kho (vì Draft chưa ảnh hưởng đến Inventory).
- **BR-IMP-17 (Phiếu Cancelled là bất biến)**: Phiếu `Cancelled` không thể kích hoạt lại hay chuyển về `Draft`. Nếu cần, phải tạo phiếu nhập mới.

---

### 2.5. Quy tắc Tính TotalAmount

- **BR-IMP-18 (Tính tự động)**: `ImportReceipt.TotalAmount` = Tổng `SubTotal` của tất cả `ImportDetail` thuộc phiếu đó. Hệ thống tính và cập nhật mỗi khi thêm/xóa `ImportDetail`.
- **BR-IMP-19 (Không nhập thủ công)**: Client (WinForms / API) không được truyền `TotalAmount` trong Request Body — Backend tự tính.
- **BR-IMP-20 (Phiếu rỗng = TotalAmount = 0)**: Phiếu Draft chưa có ImportDetail có `TotalAmount = 0` — hợp lệ.

---

### 2.6. Quy tắc Validate Hạn Sử Dụng khi Nhập

- **BR-IMP-21 (HSD phải sau ngày nhập)**: `ImportDetail.ExpiryDate` (nếu có) phải lớn hơn `ImportReceipt.ImportDate`. Không cho phép nhập hàng đã hết hạn.
- **BR-IMP-22 (HSD tối thiểu 7 ngày)**: `ExpiryDate` phải lớn hơn `DateTime.Today + 7 ngày`. Không nhập hàng sẽ hết hạn trong vòng 7 ngày tới.
- **BR-IMP-23 (HSD nullable)**: Sản phẩm không có hạn sử dụng (đồ gia dụng, pin...) được phép để `ExpiryDate = NULL`. Hệ thống sẽ áp dụng FIFO thay vì FEFO cho những sản phẩm này.

---

### 2.7. Quy tắc Phân Quyền

- **BR-IMP-24 (Chỉ Admin và Manager)**: Chỉ tài khoản có `Role = Admin (1)` hoặc `Role = Manager (2)` được phép thực hiện bất kỳ thao tác nào trong phân hệ 06_Import (tạo, xem, Confirm, Cancel).
- **BR-IMP-25 (Staff không có quyền)**: Staff (`Role = 3`) không được xem danh sách phiếu nhập, không được tạo hay Confirm phiếu.
- **BR-IMP-26 (Phạm vi chi nhánh)**: Manager chỉ được tạo phiếu nhập cho chi nhánh của mình (`BranchId` từ JWT). Admin có thể tạo cho bất kỳ chi nhánh nào.

---

### 2.8. Quy tắc Tự động Tạo StockHistory khi Confirm

- **BR-IMP-27 (Ghi StockHistory bắt buộc)**: Khi Confirm phiếu nhập, với mỗi `ImportDetail`, hệ thống phải tạo 1 bản ghi `StockHistory` với:
  - `ChangeType = Import (1)`
  - `QuantityChange = ImportDetail.Quantity` (dương)
  - `ExpiryDate = ImportDetail.ExpiryDate`
  - `ReferenceId = ImportReceipt.ImportReceiptId`
  - `CreatedByUserId` = người Confirm
- **BR-IMP-28 (Cập nhật LastUpdated)**: Sau khi cập nhật `Inventory.QuantityOnHand`, bắt buộc cập nhật `Inventory.LastUpdated = DateTime.UtcNow`.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Nếu cùng `ProductId` xuất hiện nhiều lần trong 1 phiếu (lô khác nhau), khi Confirm có tạo nhiều bản ghi `StockHistory` không? | Ảnh hưởng tính chính xác FEFO. | Đề xuất: Có — mỗi `ImportDetail` → 1 `StockHistory` riêng. Đây là yêu cầu của FEFO để phân biệt từng lô HSD. |
| 2 | Ngưỡng HSD tối thiểu 7 ngày (BR-IMP-22) có cấu hình được không hay cố định? | Ảnh hưởng UX nhập kho. | Đề xuất: Cố định 7 ngày trong v1. v2: Thêm vào `DiscountRule` hoặc config table để Admin điều chỉnh. |

---

## 4. GHI CHÚ
- Toàn bộ luồng Confirm (BR-IMP-13) phải thực thi trong `await using var transaction = await _context.Database.BeginTransactionAsync()`.
- Nên tách riêng `ImportReceiptService` và gọi `IInventoryService.AddStockAsync()` — không ghi thẳng Inventory trong ImportService để tránh coupling.
- Commit message gợi ý: `feat: add import receipt confirmation with inventory update`.

---

## 5. KẾT LUẬN

Tài liệu `BusinessRules.md` đã xác lập **28 quy tắc nghiệp vụ** đầy đủ cho phân hệ `06_Import`. Các quy tắc then chốt cần ghi nhớ: BR-IMP-05 (chỉ Draft mới sửa), BR-IMP-11 (phiếu phải có Detail trước khi Confirm), BR-IMP-13 (Confirm phải atomic), BR-IMP-14 (Confirmed là bất biến) và BR-IMP-22 (HSD tối thiểu 7 ngày). Đây là nền tảng đảm bảo tính toàn vẹn dữ liệu nhập hàng và tồn kho trong Smart SuperMarket.
