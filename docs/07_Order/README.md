# TỔNG QUAN PHÂN HỆ ĐƠN HÀNG (07_ORDER MODULE)

---

## 1. GIỚI THIỆU
Phân hệ Đơn hàng (Orders & POS Feature) là trung tâm giao dịch của hệ thống Smart SuperMarket. Phân hệ chịu trách nhiệm xử lý nghiệp vụ bán lẻ trực tiếp tại quầy POS và đơn hàng trực tuyến, cấn trừ tồn kho theo từng chi nhánh, áp dụng đa khuyến mãi (N-N), thanh toán đa hình thức, tích lũy điểm thưởng thành viên và lưu vết lịch sử kho hàng.

Toàn bộ thiết kế dữ liệu tuân thủ chuẩn hóa 3NF theo tài liệu CSDL ERD v2.0.

---

## 2. DANH MỤC TÀI LIỆU KỸ THUẬT
- **`OrderEntity.md`**: Đặc tả lược đồ dữ liệu các bảng `Order`, `OrderDetail`, `OrderPromotion`, `Payment` và các khóa ngoại liên kết.
- **`BusinessRules.md`**: Quy tắc tính tiền, kiểm tra tồn kho chi nhánh, áp dụng voucher/khuyến mãi, tích điểm và hoàn hủy giao dịch.
- **`OrderAPI.md`**: Đặc tả các endpoint RESTful API phục vụ quầy thu ngân POS và quản lý hóa đơn.
- **`SequenceDiagram.md`**: Sơ đồ tuần tự thể hiện tương tác hệ thống khi thanh toán và hủy đơn hàng.
- **`OrderFlow.md`**: Lưu đồ trạng thái vòng đời của một hóa đơn bán lẻ.
- **`Tasks.md`**: Danh mục công việc chi tiết cho lập trình viên Backend.

---

## 3. CÁC TÁC TỬ TƯƠNG TÁC
- **Nhân viên thu ngân (Staff / Cashier)**: Quét mã vạch sản phẩm, nhập mã giảm giá, tiếp nhận thanh toán và in hóa đơn tại WinForms POS.
- **Quản trị viên / Cửa hàng trưởng (Admin / Manager)**: Xem danh sách hóa đơn, tra cứu chi tiết, xử lý khiếu nại và hủy đơn hoàn kho.
- **Khách hàng (Customer)**: Mua sắm, sử dụng voucher cá nhân, tích lũy điểm thưởng thành viên.
