### 5. `09_Payment/BusinessRules.md`

```markdown
# Business Rules - Quy Tắc Nghiệp Vụ Thanh Toán (Smart SuperMarket)

## 1. Các Quy Tắc Nghiệp Vụ
* **BR-01: Order tồn tại**: Chỉ khởi tạo thanh toán cho các hóa đơn đã tồn tại trong hệ thống.
* **BR-02: Số tiền hợp lệ**: Số tiền thanh toán phải lớn hơn 0 (`Amount > 0`).
* **BR-03: Cấu trúc nội dung VietQR**: Nội dung chuyển khoản bắt buộc theo định dạng `SSM + [Mã_Hóa_Đơn]` (Ví dụ: `SSM10293`).
* **BR-04: Chuẩn hóa chuỗi VietQR**: Nội dung chuyển khoản **không dùng tiếng Việt có dấu**, không chứa ký tự đặc biệt.
* **BR-05: Phương thức thanh toán**: Hệ thống chấp nhận `1` (Tiền mặt), `2` (VietQR), `3` (Thẻ POS).
* **BR-06: Xác nhận thanh toán**: Thu ngân trực tiếp kiểm tra thông báo tiền về (thông báo App hoặc loa báo) trước khi bấm nút "Đã nhận tiền" trên WinForms.
* **BR-07: Tiền thừa tiền mặt**: Số tiền khách đưa phải lớn hơn hoặc bằng tổng hóa đơn. Tiền thừa làm tròn đến hàng trăm đồng.
* **BR-08: Cập nhật CSDL**: Thao tác Cập nhật giao dịch $\rightarrow$ Tạo hóa đơn $\rightarrow$ Trừ kho phải nằm trong 1 **Database Transaction** (Rollback nếu xảy ra lỗi).