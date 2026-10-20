## 4. `09_Payment/PaymentFlow.md`

```markdown
# Flow Thanh Toán Tại Quầy POS - Smart SuperMarket

## 1. Luồng Thanh Toán VietQR (Xác Nhận Thủ Công)

```text
[Thu ngân]             [C# WinForms POS]                [Khách hàng]          [App Ngân hàng]
    |                          |                             |                       |
    |-- 1. Chọn "VietQR" ----->|                             |                       |
    |                          |-- 2. Sinh URL VietQR ------->|                       |
    |                          |-- 3. Load mã vào PictureBox>|                       |
    |                          |                             |-- 4. Quét mã QR ----->|
    |                          |                             |-- 5. Chuyển tiền ---->|
    |                          |                                                     |
    |<-- 6. Nghe loa / Xem App-| <===================================================|
    |-- 7. Bấm "Đã nhận tiền"->|                                                     |
    |                          |-- 8. Lưu DB & In hóa đơn -------------------------->|
2. Luồng Thanh Toán Tiền Mặt (Cash)
Plaintext
1. Thu ngân chọn phương thức "Tiền mặt".
2. Thu ngân nhập "Số tiền khách đưa".
3. WinForms tự động tính: Tiền thừa = Số tiền khách đưa - Tổng tiền hóa đơn.
4. Kiểm tra:
   - Nếu Tiền thừa >= 0: Bật sáng nút "Hoàn tất & In Bill", lưu DB với Status = Success.
   - Nếu Tiền thừa < 0: Báo lỗi "Chưa đủ tiền".