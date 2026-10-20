Markdown
# API Specification - VietQR Quick Link (Smart SuperMarket)

## 1. VietQR Quick Link API (Miễn phí 100%)

Hệ thống không cần gọi API Key hay Backend riêng, chỉ ghép URL dạng ảnh PNG và load trực tiếp vào Control `PictureBox` trong WinForms.

* **HTTP Method**: `GET`
* **URL Structure**: 
  `https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}`

* **Bảng tham số**:
  | Tham số | Kiểu dữ liệu | Mô tả | Ví dụ |
  | :--- | :--- | :--- | :--- |
  | `BANK_CODE` | String | Mã NAPAS ngân hàng siêu thị | `MB`, `VCB`, `ICB`, `TCB` |
  | `ACCOUNT_NO` | String | Số tài khoản nhận tiền | `0987654321` |
  | `TEMPLATE` | String | Kiểu giao diện QR (`compact2`, `compact`, `qr_only`) | `compact2` |
  | `amount` | Decimal | Số tiền hóa đơn | `150000` |
  | `addInfo` | String | Nội dung chuyển khoản (URL Encoded) | `SSM10293` |
  | `accountName` | String | Tên chủ tài khoản (URL Encoded) | `SMART SUPERMARKET` |