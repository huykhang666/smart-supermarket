# 09_Payment Module - Hệ Thống Thanh Toán Smart SuperMarket

## 1. Tổng Quan Module
Module `09_Payment` chịu trách nhiệm xử lý toàn bộ quy trình thanh toán tại quầy POS (WinForms) cho hệ thống Quản lý Siêu thị **Smart SuperMarket**, lưu lịch sử giao dịch và phục vụ đối soát doanh thu.

## 2. Các Phương Thức Thanh Toán Hỗ Trợ (100% Miễn Phí)
1. **Tiền mặt (Cash)**: Thu ngân nhận tiền mặt, hệ thống tự động tính tiền thừa trả khách.
2. **VietQR / QR Ngân hàng (Free)**: Tự động sinh mã QR động chứa sẵn Số tài khoản, Số tiền và Nội dung chuyển khoản. Khách quét chuyển tiền $\rightarrow$ Thu ngân nhìn màn hình/nghe loa báo $\rightarrow$ Bấm xác nhận hoàn tất.
3. **Thẻ Ngân hàng (POS)**: Thanh toán quẹt thẻ ATM / VISA qua máy POS quầy.

## 3. Cấu Trúc Thư Mục Tài Liệu
* `README.md`: Tài liệu tổng quan module.
* `Payment.md`: Định nghĩa các Cấu trúc dữ liệu, DTO và Enum thanh toán.
* `PaymentTransaction.md`: Thiết kế Bảng CSDL SQL Server lưu vết giao dịch.
* `PaymentFlow.md`: Sơ đồ luồng nghiệp vụ thanh toán VietQR thủ công và Tiền mặt.
* `BusinessRules.md`: Các quy tắc nghiệp vụ bắt buộc (BR-01 đến BR-08).
* `API.md`: Chi tiết API tạo VietQR Dynamic Quick Link miễn phí.
* `VietQR.md`: Code C# WinForms mẫu hiển thị mã QR và nút xác nhận thanh toán.