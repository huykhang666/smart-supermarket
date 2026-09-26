# ĐẶC TẢ RESTFUL API PHÂN HỆ KHÁCH HÀNG (09_CUSTOMER API)

## 1. BASE URL

```text
/api/v1/customers
```

Tất cả API sử dụng cấu trúc:

```json
{
  "isSuccess": true,
  "message": "...",
  "data": {},
  "errors": null
}
```

Khi thất bại:

```json
{
  "isSuccess": false,
  "message": "...",
  "data": null,
  "errors": [
    "..."
  ]
}
```

---

# 2. TẠO CUSTOMER

## Endpoint

```http
POST /api/v1/customers
```

## Quyền

```text
Staff
Admin
```

## Request

```json
{
  "fullName": "Nguyễn Văn An",
  "phone": "0901234567",
  "email": "an@example.com",
  "dateOfBirth": "2000-05-20",
  "gender": 1,
  "address": "TP. Hồ Chí Minh"
}
```

## Response

```json
{
  "isSuccess": true,
  "message": "Tạo khách hàng thành công",
  "data": {
    "customerId": 1,
    "fullName": "Nguyễn Văn An",
    "phone": "0901234567",
    "email": "an@example.com",
    "dateOfBirth": "2000-05-20",
    "gender": 1,
    "address": "TP. Hồ Chí Minh",
    "loyaltyPoints": 0,
    "status": 1
  },
  "errors": null
}
```

---

# 3. TÌM KIẾM CUSTOMER

## Endpoint

```http
GET /api/v1/customers
```

## Query Parameters

| Tham số    | Kiểu      | Mô tả                           |
| ---------- | --------- | ------------------------------- |
| `keyword`  | `string?` | Tìm theo tên hoặc số điện thoại |
| `status`   | `byte?`   | Lọc trạng thái                  |
| `page`     | `int`     | Trang                           |
| `pageSize` | `int`     | Số bản ghi                      |

Ví dụ:

```http
GET /api/v1/customers?keyword=0901234567&page=1&pageSize=10
```

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy danh sách khách hàng thành công",
  "data": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "items": [
      {
        "customerId": 1,
        "fullName": "Nguyễn Văn An",
        "phone": "0901234567",
        "email": "an@example.com",
        "loyaltyPoints": 25,
        "status": 1
      }
    ]
  },
  "errors": null
}
```

---

# 4. XEM CUSTOMER THEO ID

## Endpoint

```http
GET /api/v1/customers/{id}
```

## Quyền

```text
Staff
Admin
Customer
```

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy thông tin khách hàng thành công",
  "data": {
    "customerId": 1,
    "fullName": "Nguyễn Văn An",
    "phone": "0901234567",
    "email": "an@example.com",
    "dateOfBirth": "2000-05-20",
    "gender": 1,
    "address": "TP. Hồ Chí Minh",
    "loyaltyPoints": 25,
    "status": 1
  },
  "errors": null
}
```

---

# 5. CẬP NHẬT CUSTOMER

## Endpoint

```http
PUT /api/v1/customers/{id}
```

## Quyền

```text
Admin
Manager
```

## Request

```json
{
  "fullName": "Nguyễn Văn An",
  "phone": "0901234567",
  "email": "an.new@example.com",
  "dateOfBirth": "2000-05-20",
  "gender": 1,
  "address": "TP. Hồ Chí Minh"
}
```

Không cho phép API này tự ý thay đổi:

```text
CustomerId
LoyaltyPoints
CreatedAt
```

LoyaltyPoints chỉ được thay đổi thông qua nghiệp vụ Loyalty.

---

# 6. XEM ĐIỂM LOYALTY

## Endpoint

```http
GET /api/v1/customers/{id}/loyalty
```

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy điểm thưởng thành công",
  "data": {
    "customerId": 1,
    "loyaltyPoints": 25
  },
  "errors": null
}
```

---

# 7. XEM LỊCH SỬ ĐIỂM

## Endpoint

```http
GET /api/v1/customers/{id}/loyalty/history
```

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy lịch sử điểm thành công",
  "data": {
    "items": [
      {
        "pointHistoryId": 100,
        "orderId": 105,
        "pointChange": 25,
        "type": 1,
        "createdAt": "2026-09-16T17:40:00Z"
      },
      {
        "pointHistoryId": 101,
        "orderId": 104,
        "pointChange": -10,
        "type": 3,
        "createdAt": "2026-09-17T10:20:00Z"
      }
    ]
  },
  "errors": null
}
```

---

# 8. XEM LỊCH SỬ MUA HÀNG

## Endpoint

```http
GET /api/v1/customers/{id}/orders
```

## Query Parameters

| Tham số     | Kiểu        | Mô tả            |
| ----------- | ----------- | ---------------- |
| `branchId`  | `int?`      | Chi nhánh        |
| `status`    | `byte?`     | Trạng thái Order |
| `startDate` | `DateTime?` | Từ ngày          |
| `endDate`   | `DateTime?` | Đến ngày         |
| `page`      | `int`       | Trang            |
| `pageSize`  | `int`       | Số lượng/trang   |

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy lịch sử mua hàng thành công",
  "data": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 2,
    "totalPages": 1,
    "items": [
      {
        "orderId": 105,
        "branchId": 1,
        "orderDate": "2026-09-16T17:40:00Z",
        "totalAmount": 300000,
        "discountAmount": 50000,
        "finalAmount": 250000,
        "status": 1
      },
      {
        "orderId": 104,
        "branchId": 1,
        "orderDate": "2026-09-15T15:20:00Z",
        "totalAmount": 100000,
        "discountAmount": 0,
        "finalAmount": 100000,
        "status": 1
      }
    ]
  },
  "errors": null
}
```

---

# 9. XEM VOUCHER CỦA CUSTOMER

## Endpoint

```http
GET /api/v1/customers/{id}/vouchers
```

## Query Parameters

```text
status=available
```

Có thể hỗ trợ các trạng thái:

```text
available
used
expired
```

## Response

```json
{
  "isSuccess": true,
  "message": "Lấy danh sách voucher thành công",
  "data": [
    {
      "voucherId": 3,
      "code": "CHAOMUNG2026",
      "isUsed": false,
      "expiryDate": "2026-12-31"
    }
  ],
  "errors": null
}
```

---

# 10. TÌM CUSTOMER TẠI POS

## Endpoint

```http
GET /api/v1/customers/lookup?phone=0901234567
```

## Mục đích

API này phục vụ trực tiếp cho WinForms POS.

Quy trình:

```text
POS
 |
 | Nhập số điện thoại
 v
Customer Lookup API
 |
 +---- Không tìm thấy
 |
 +---- Tìm thấy
          |
          v
     Hiển thị Customer
     LoyaltyPoints
     Voucher
```

## Response

```json
{
  "isSuccess": true,
  "message": "Tìm thấy khách hàng",
  "data": {
    "customerId": 1,
    "fullName": "Nguyễn Văn An",
    "phone": "0901234567",
    "loyaltyPoints": 25,
    "status": 1
  },
  "errors": null
}
```

---

# 11. CUSTOMER KHÔNG TỰ CỘNG ĐIỂM

Không cung cấp API kiểu:

```http
POST /api/v1/customers/{id}/loyalty/add
```

cho Customer hoặc POS tùy ý gọi để cộng điểm.

Điểm phải được phát sinh từ nghiệp vụ Order:

```text
Checkout
   |
   v
FinalAmount
   |
   v
Calculate Points
   |
   v
Customer.LoyaltyPoints
   |
   v
PointHistory
```

Điều này tránh việc nhân viên hoặc client tự tạo điểm giả.

---

# 12. ERROR CODES

| HTTP  | Trường hợp                    |
| ----- | ----------------------------- |
| `400` | Dữ liệu Customer không hợp lệ |
| `401` | Chưa xác thực                 |
| `403` | Không có quyền                |
| `404` | Không tìm thấy Customer       |
| `409` | Số điện thoại đã tồn tại      |
| `500` | Lỗi hệ thống                  |

## Ví dụ Customer không tồn tại

```json
{
  "isSuccess": false,
  "message": "Không tìm thấy khách hàng",
  "data": null,
  "errors": [
    "Không tìm thấy Customer có ID = 999"
  ]
}
```

## Ví dụ trùng số điện thoại

```json
{
  "isSuccess": false,
  "message": "Không thể tạo khách hàng",
  "data": null,
  "errors": [
    "Số điện thoại 0901234567 đã được đăng ký"
  ]
}
```
