# QUY TẮC NGHIỆP VỤ PHÂN HỆ ĐƠN HÀNG (07_ORDER BUSINESS RULES)

---

## 1. QUY TẮC TẠO VÀ THANH TOÁN ĐƠN HÀNG TẠI QUẦY POS

1. **Ràng buộc Chi nhánh & Tồn kho (`Inventory`)**:
   - Mọi đơn hàng bắt buộc phải gắn với một `BranchId`.
   - Kiểm tra tồn kho tại bảng `Inventory` theo cặp khóa `(ProductId, BranchId)`.
   - Nếu bất kỳ sản phẩm nào có `QuantityInStock < Quantity`, giao dịch lập tức bị từ chối với mã lỗi `400 Bad Request`.
   - Trừ trực tiếp số lượng bán ra khỏi `Inventory.QuantityInStock`.

2. **Ghi vết biến động kho (`StockHistory`)**:
   - Với mỗi sản phẩm xuất bán, ghi 1 bản ghi vào bảng `StockHistory`:
     - `ProductId`: Mã sản phẩm.
     - `BranchId`: Mã chi nhánh thực hiện giao dịch.
     - `ChangeQuantity = -Quantity` (giá trị âm).
     - `Reason = 2` (Bán POS).
     - `ReferenceId = OrderId` (Lấy từ mã đơn hàng sau khi insert `Order`).

3. **Áp dụng Giảm giá & Khuyến mãi (Voucher & Promotion)**:
   - **Chương trình khuyến mãi tự động (`Promotion`)**:
     - Hệ thống tự động quét các chương trình đang hiệu lực (`Status == 1`, trong khoảng `StartDate` - `EndDate`) khớp theo `ProductId` hoặc `CategoryId` của giỏ hàng.
     - Số tiền giảm được ghi nhận chi tiết theo từng khuyến mãi vào bảng `OrderPromotion`.
   - **Voucher cá nhân (`Voucher`)**:
     - Áp dụng khi thu ngân nhập mã voucher hợp lệ của khách hàng.
     - Điều kiện: `IsUsed == 0` và `ExpiryDate >= Today`. Sau khi tạo đơn, cập nhật `IsUsed = 1`.
   - **Tính toán tiền hàng**:
     - `TotalAmount` = Tổng (`UnitPrice * Quantity`) của toàn bộ sản phẩm.
     - `DiscountAmount` = Tổng tiền giảm từ các `OrderPromotion` + Tiền giảm từ `Voucher`.
     - `FinalAmount = Math.Max(0, TotalAmount - DiscountAmount)` (Tiền thực thu không âm, ràng buộc `CHECK >= 0`).

4. **Giao dịch Thanh toán (`Payment`)**:
   - Bắt buộc kiểm tra: `Sum(payments[].AmountPaid) >= FinalAmount`.
   - Nếu tổng tiền trả nhỏ hơn `FinalAmount`, trả về lỗi `400 Bad Request`.
   - Lưu vết từng phương thức thanh toán vào bảng `Payment`.

5. **Tích lũy Điểm thưởng Khách hàng (`Customer` & `PointHistory`)**:
   - Nếu đơn hàng có gắn `CustomerId`:
     - Tỷ lệ: 1 điểm cho mỗi 10,000 VNĐ tính trên `FinalAmount` (lấy phần nguyên).
     - Cộng dồn vào `Customer.LoyaltyPoints`.
     - Tạo bản ghi trong `PointHistory`: `CustomerId`, `OrderId`, `PointChange = +PointsEarned`, `Type = 1` (Tích lũy).

6. **Thứ tự thực thi trong Database Transaction**:
   - Bước 1: Mở `BeginTransactionAsync()`.
   - Bước 2: Kiểm tra tồn kho toàn bộ sản phẩm.
   - Bước 3: Insert bản ghi `Order` để sinh `OrderId`.
   - Bước 4: Insert danh sách `OrderDetail`, `OrderPromotion`, `Payment`.
   - Bước 5: Cấn trừ `Inventory.QuantityInStock` và Insert các dòng `StockHistory` (gắn `ReferenceId = OrderId`).
   - Bước 6: Cập nhật `Voucher.IsUsed = 1` (nếu có).
   - Bước 7: Cập nhật điểm tích lũy `Customer` và ghi `PointHistory` (nếu có `CustomerId`).
   - Bước 8: `CommitTransactionAsync()`.

---

## 2. QUY TẮC HỦY ĐƠN HÀNG (CANCEL ORDER)

1. **Điều kiện hủy**:
   - Chỉ được hủy đơn hàng đang có trạng thái `Status == 1` (Hoàn tất).
   - Chỉ người dùng có vai trò `Admin` hoặc `Manager` mới có quyền thực hiện.

2. **Khôi phục dữ liệu**:
   - Cập nhật `Order.Status = 2` (Đã hủy).
   - **Hoàn kho**: Cộng trả lại số lượng từng mặt hàng vào `Inventory.QuantityInStock` tại đúng `BranchId`.
   - **Ghi log biến động**: Thêm bản ghi `StockHistory` với `Reason = 2`, `ChangeQuantity = +Quantity`, `ReferenceId = OrderId`.
   - **Hoàn Voucher**: Nếu đơn có sử dụng voucher, cập nhật lại `Voucher.IsUsed = 0`.
   - **Thu hồi Điểm**: Trừ lại `Customer.LoyaltyPoints` tương ứng và ghi log `PointHistory` với `Type = 3` (Thu hồi/Điều chỉnh), `PointChange = -PointsEarned`.
