# LƯU ĐỒ NGHIỆP VỤ & VÒNG ĐỜI ĐƠN HÀNG (07_ORDER FLOW)

Tài liệu mô tả chi tiết vòng đời của một đơn hàng và quy trình luân chuyển dữ liệu bán lẻ tại quầy POS, tuân thủ đúng lược đồ CSDL ERD v2.0.

---

## 1. SƠ ĐỒ CHUYỂN DỊCH TRẠNG THÁI (STATE MACHINE)

Đơn hàng trong hệ thống tuân theo các trạng thái định nghĩa tại cột `Status` của bảng `Order` (1 = Hoàn tất, 2 = Đã hủy):

```mermaid
stateDiagram-v2
    [*] --> InitOrder : 1. Quét sản phẩm tại WinForms POS
    InitOrder --> ValidateStock : 2. Bấm thanh toán (Gửi CreateOrderRequest)
    
    state ValidateStock {
        [*] --> CheckBranchInventory
        CheckBranchInventory --> StockAvailable : Tồn kho (ProductId, BranchId) đủ
        CheckBranchInventory --> StockShortage : Tồn kho không đủ
    }
    
    StockShortage --> [*] : Báo lỗi 400 & Dừng giao dịch
    
    StockAvailable --> CheckPromotions : 3. Kiểm tra Voucher & Promotions
    CheckPromotions --> ExecuteTransaction : 4. Bắt đầu Database Transaction
    
    state ExecuteTransaction {
        [*] --> DeductInventory : Trừ QuantityInStock & Ghi StockHistory (Reason=2)
        DeductInventory --> MarkVoucher : Đánh dấu Voucher.IsUsed = 1 (nếu có)
        MarkVoucher --> SaveOrderDetails : Insert Order, OrderDetail, OrderPromotion
        SaveOrderDetails --> RecordPayment : Insert Payment (Method, AmountPaid)
        RecordPayment --> AddLoyaltyPoints : Cộng điểm Customer & ghi PointHistory (nếu có CustomerId)
    }

    ExecuteTransaction --> Completed : 5. Commit Transaction thành công (Status = 1)
    ExecuteTransaction --> [*] : Lỗi hệ thống -> Rollback toàn bộ
    
    Completed --> Cancelled : 6. Quản lý yêu cầu hủy đơn (POST /cancel)
    
    state Cancelled {
        [*] --> RestoreInventory : Cộng trả lại tồn kho & ghi StockHistory
        RestoreInventory --> RestoreVoucher : Khôi phục Voucher.IsUsed = 0
        RestoreVoucher --> DeductPoints : Thu hồi LoyaltyPoints & ghi PointHistory (Type=3)
        DeductPoints --> UpdateStatus : Cập nhật Order.Status = 2
    }

    Cancelled --> [*] : Hoàn tất chu trình hủy đơn