# QUY TẮC NGHIỆP VỤ PHÂN HỆ ĐƠN HÀNG (07_ORDER BUSINESS RULES)

---

## 1. QUY TẮC TẠO VÀ THANH TOÁN ĐƠN HÀNG TẠI QUẦY POS

1. **Ràng buộc Chi nhánh & Tồn kho (`Inventory`)**:
   - Mọi đơn hàng bắt buộc phải gắn với một `BranchId`.
   - Kiểm tra tồn kho tại bảng `Inventory` dựa trên ràng buộc `(ProductId, BranchId)`.
   - Nếu bất kỳ sản phẩm nào có `QuantityInStock < Quantity`, giao dịch lập tức bị từ chối với lỗi `400 Bad Request`.
   - Trừ trực tiếp số lượng bán khỏi `Inventory.QuantityInStock`.

2. **Ghi vết biến động kho (`StockHistory`)**:
   - Với mỗi sản phẩm xuất bán, thêm 1 bản ghi vào bảng `StockHistory`:
     - `ProductId`: Mã sản phẩm.
     - `BranchId`: Mã chi nhánh.
     - `ChangeQuantity = -Quantity` (giá trị âm).
     - `Reason = 2` (Bán POS).
     - `ReferenceId = OrderId`.

3. **Áp dụng Giảm giá & Khuyến mãi (Voucher & Promotion)**:
   - **Voucher cá nhân**:
     - Tra cứu bảng `Voucher` theo `CustomerId` và `Code`.
     - Điều kiện: `IsUsed == 0` và `ExpiryDate >= Today`.
     - Sau khi đơn tạo thành công, cập nhật `IsUsed = 1`.
   - **Chương trình khuyến mãi (`Promotion`)**:
     - Kiểm tra các khuyến mãi còn hiệu lực (`Status == 1`, trong khoảng `StartDate` và `EndDate`).
     - Ghi nhận chi tiết tiền giảm vào bảng `OrderPromotion`.
   - Tổng tiền giảm: `Order.DiscountAmount = Sum(OrderPromotion.DiscountAmount) + Voucher.DiscountAmount`.
   - Tiền thực thu: `Order.FinalAmount = Order.TotalAmount - Order.DiscountAmount`.

4. **Tích lũy Điểm thưởng Khách hàng (`Customer` & `PointHistory`)**:
   - Nếu `CustomerId` khác NULL:
     - Tỷ lệ tích điểm: 1 điểm cho mỗi 10,000 VNĐ trên `FinalAmount` (tính phần nguyên).
     - Cộng dồn vào `Customer.LoyaltyPoints`.
     - Tạo bản ghi trong `PointHistory`:
       - `CustomerId`: Khách hàng tương ứng.
       - `OrderId`: Mã đơn hàng.
       - `PointChange = +PointsEarned`.
       - `Type = 1` (Tích điểm).
       - `Description = "Tích điểm từ đơn hàng #" + OrderId`.

5. **Giao dịch Thanh toán (`Payment`)**:
   - Ghi nhận đầy đủ thông tin thanh toán vào bảng `Payment`.
   - Hỗ trợ thanh toán hỗn hợp (ví dụ: một phần tiền mặt, một phần chuyển khoản) miễn là `Sum(AmountPaid) >= FinalAmount`.

6. **Tính toàn vẹn Dữ liệu (Database Transaction)**:
   - Toàn bộ thao tác ghi vào `Order`, `OrderDetail`, `OrderPromotion`, `Payment`, `Inventory`, `StockHistory`, `Voucher`, `Customer`, `PointHistory` phải được bọc trong một **Database Transaction** duy nhất. Lỗi ở bất kỳ bước nào đều phải `Rollback` hoàn toàn.

---

## 2. QUY TẮC HỦY ĐƠN HÀNG (CANCEL ORDER)

1. **Điều kiện hủy**:
   - Chỉ được hủy các đơn hàng đang có `Status == 1` (Hoàn tất).
   - Chỉ người dùng có vai trò `Admin` hoặc `Manager` mới có quyền thực hiện.

2. **Khôi phục Dữ liệu**:
   - Đổi `Order.Status = 2` (Đã hủy).
   - **Hoàn kho**: Cộng trả lại số lượng từng mặt hàng vào `Inventory(QuantityInStock)` tại đúng `BranchId`.
   - **Ghi log biến động**: Thêm bản ghi `StockHistory` với `Reason = 2`, `ChangeQuantity = +Quantity`, `ReferenceId = OrderId`.
   - **Hoàn Voucher**: Nếu đơn có dùng voucher, cập nhật lại `Voucher.IsUsed = 0`.
   - **Thu hồi Điểm thưởng**: Nếu đơn đã tích điểm, trừ lại `Customer.LoyaltyPoints` và ghi nhận một dòng vào `PointHistory` (`Type = 3` - Adjust) với `PointChange = -PointsEarned`.
