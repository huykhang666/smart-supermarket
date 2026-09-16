Dưới đây là nguyên văn nội dung đầy đủ của file **`docs/07_Order/OrderAPI.md`**, không rút gọn bất kỳ mục nào, bám sát cấu trúc CSDL ERD v2.0 (bảng `Order`, `OrderDetail`, `OrderPromotion`, `Payment`) và định dạng bọc kết quả `ApiResult<T>`:

markdown
# ĐẶC TẢ RESTFUL API PHÂN HỆ ĐƠN HÀNG (07_ORDER API SPECIFICATION)

Base URL: `/api/v1/orders`

Tất cả phản hồi API đều tuân thủ cấu trúc chuẩn bọc trong lớp `ApiResult<T>`:
- Thành công: `isSuccess = true`, `data` chứa payload, `errors = null`.
- Thất bại: `isSuccess = false`, `data = null`, `errors` chứa thông điệp lỗi.

---

## 1. TẠO ĐƠN HÀNG & THANH TOÁN TẠI QUẦY POS (CHECKOUT)

- **Endpoint**: `POST /api/v1/orders`
- **Mô tả**: Tiếp nhận danh sách sản phẩm xuất bán từ quầy POS, kiểm tra tồn kho theo chi nhánh (`BranchId`), kiểm tra mã khuyến mãi và voucher cá nhân, ghi nhận thông tin hóa đơn, trừ tồn kho và ghi log lịch sử biến động kho trong một Database Transaction duy nhất.
- **Quyền hạn**: `Staff`, `Admin`, `Customer`

### Request Body (`CreateOrderRequest`)
```json
{
  "employeeId": 2,
  "customerId": 1,
  "branchId": 1,
  "voucherCode": "CHAOMUNG2026",
  "items": [
    {
      "productId": 10,
      "quantity": 2
    },
    {
      "productId": 15,
      "quantity": 1
    }
  ],
  "payments": [
    {
      "paymentMethod": 1,
      "amountPaid": 250000
    }
  ]
}


### Chi tiết tham số Request Body

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
| --- | --- | --- | --- |
| `employeeId` | `int` | Có | ID tài khoản nhân viên thu ngân lập đơn |
| `customerId` | `int?` | Không | ID khách hàng thành viên (nếu khách tích điểm) |
| `branchId` | `int` | Có | ID chi nhánh phát sinh giao dịch |
| `voucherCode` | `string?` | Không | Mã voucher cá nhân muốn sử dụng |
| `items` | `Array` | Có | Danh sách sản phẩm mua hàng (tối thiểu 1 món) |
| `items[].productId` | `int` | Có | Mã sản phẩm |
| `items[].quantity` | `int` | Có | Số lượng mua (> 0) |
| `payments` | `Array` | Có | Danh sách giao dịch thanh toán |
| `payments[].paymentMethod` | `byte` | Có | 1 = Tiền mặt, 2 = QR Code, 3 = Thẻ |
| `payments[].amountPaid` | `decimal` | Có | Số tiền trả qua phương thức này |

### Response Thành công (`201 Created`)

json
{
  "isSuccess": true,
  "message": "Tạo đơn hàng và thanh toán thành công",
  "data": {
    "orderId": 105,
    "employeeId": 2,
    "customerId": 1,
    "branchId": 1,
    "orderDate": "2026-09-16T17:40:00Z",
    "totalAmount": 300000,
    "discountAmount": 50000,
    "voucherId": 3,
    "finalAmount": 250000,
    "status": 1,
    "details": [
      {
        "orderDetailId": 210,
        "productId": 10,
        "productName": "Sữa chua Vinamilk 100g",
        "quantity": 2,
        "unitPrice": 50000,
        "subTotal": 100000
      },
      {
        "orderDetailId": 211,
        "productId": 15,
        "productName": "Dầu ăn Simply 1L",
        "quantity": 1,
        "unitPrice": 200000,
        "subTotal": 200000
      }
    ],
    "appliedPromotions": [
      {
        "promotionId": 2,
        "promotionName": "Giảm giá sữa đầu tuần",
        "discountAmount": 20000
      }
    ],
    "payments": [
      {
        "paymentId": 88,
        "paymentMethod": 1,
        "amountPaid": 250000,
        "paymentDate": "2026-09-16T17:40:00Z"
      }
    ]
  },
  "errors": null
}


### Phản hồi thất bại mẫu

* **Tồn kho không đủ (`400 Bad Request`)**:

json
{
  "isSuccess": false,
  "message": "Không thể tạo đơn hàng",
  "data": null,
  "errors": ["Sản phẩm có ID 10 không đủ tồn kho tại chi nhánh 1 (Còn 1, yêu cầu 2)"]
}



 **Voucher không hợp lệ hoặc đã dùng (`400 Bad Request`)**:
 `json
{
  "isSuccess": false,
  "message": "Mã voucher không hợp lệ",
  "data": null,
  "errors": ["Voucher CHAOMUNG2026 đã được sử dụng hoặc đã hết hạn"]
}


## 2. LẤY DANH SÁCH ĐƠN HÀNG CÓ PHÂN TRANG & BỘ LỌC

* **Endpoint**: `GET /api/v1/orders`
* **Mô tả**: Tra cứu danh sách đơn hàng phục vụ quản trị, xem lịch sử bán hàng theo ca hoặc theo chi nhánh.
* **Quyền hạn**: `Staff`, `Admin`

### Query Parameters

| Tham số | Kiểu dữ liệu | Mặc định | Mô tả |
| --- | --- | --- | --- |
| `branchId` | `int?` | null | Lọc theo chi nhánh |
| `employeeId` | `int?` | null | Lọc theo nhân viên thu ngân |
| `customerId` | `int?` | null | Lọc theo khách hàng |
| `status` | `byte?` | null | 1 = Hoàn tất, 2 = Đã hủy |
| `startDate` | `DateTime?` | null | Lọc từ ngày (định dạng ISO) |
| `endDate` | `DateTime?` | null | Lọc đến ngày (định dạng ISO) |
| `page` | `int` | 1 | Số thứ tự trang hiện tại |
| `pageSize` | `int` | 10 | Số lượng bản ghi trên một trang |

### Response Thành công (`200 OK`)
json
{
  "isSuccess": true,
  "message": "Lấy danh sách đơn hàng thành công",
  "data": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 45,
    "totalPages": 5,
    "items": [
      {
        "orderId": 105,
        "employeeId": 2,
        "customerId": 1,
        "branchId": 1,
        "orderDate": "2026-09-16T17:40:00Z",
        "totalAmount": 300000,
        "discountAmount": 50000,
        "finalAmount": 250000,
        "status": 1
      },
      {
        "orderId": 104,
        "employeeId": 2,
        "customerId": null,
        "branchId": 1,
        "orderDate": "2026-09-16T16:20:00Z",
        "totalAmount": 120000,
        "discountAmount": 0,
        "finalAmount": 120000,
        "status": 1
      }
    ]
  },
  "errors": null
}


---

## 3. XEM CHI TIẾT ĐƠN HÀNG THEO ID

* **Endpoint**: `GET /api/v1/orders/{id}`
* **Mô tả**: Xem thông tin chi tiết toàn bộ hóa đơn bao gồm danh sách mặt hàng, tiền giảm chi tiết và thông tin các lần thanh toán.
* **Quyền hạn**: `Staff`, `Admin`, `Customer`

### Parameters

*`id` (`int`, path): Mã định danh đơn hàng (`OrderId`)

### Response Thành công (`200 OK`)
json
{
  "isSuccess": true,
  "message": "Lấy chi tiết đơn hàng thành công",
  "data": {
    "orderId": 105,
    "employeeId": 2,
    "customerId": 1,
    "branchId": 1,
    "orderDate": "2026-09-16T17:40:00Z",
    "totalAmount": 300000,
    "discountAmount": 50000,
    "voucherId": 3,
    "finalAmount": 250000,
    "status": 1,
    "details": [
      {
        "orderDetailId": 210,
        "productId": 10,
        "productName": "Sữa chua Vinamilk 100g",
        "quantity": 2,
        "unitPrice": 50000,
        "subTotal": 100000
      },
      {
        "orderDetailId": 211,
        "productId": 15,
        "productName": "Dầu ăn Simply 1L",
        "quantity": 1,
        "unitPrice": 200000,
        "subTotal": 200000
      }
    ],
    "appliedPromotions": [
      {
        "promotionId": 2,
        "promotionName": "Giảm giá sữa đầu tuần",
        "discountAmount": 20000
      }
    ],
    "payments": [
      {
        "paymentId": 88,
        "paymentMethod": 1,
        "amountPaid": 250000,
        "paymentDate": "2026-09-16T17:40:00Z"
      }
    ]
  },
  "errors": null
}

### Phản hồi không tìm thấy (`404 Not Found`)

json
{
  "isSuccess": false,
  "message": "Không tìm thấy đơn hàng",
  "data": null,
  "errors": ["Không tìm thấy đơn hàng có ID = 999"]
}



## 4. HỦY ĐƠN HÀNG & HOÀN KHO

* **Endpoint**: `POST /api/v1/orders/{id}/cancel`
* **Mô tả**: Hủy đơn hàng đã tạo, cập nhật trạng thái `Status = 2` (Đã hủy), tự động hoàn trả số lượng hàng vào bảng `Inventory` tại đúng `BranchId`, ghi log hoàn kho vào `StockHistory`, khôi phục trạng thái `Voucher` nếu có và thu hồi điểm tích lũy thành viên.
* **Quyền hạn**: `Admin`, `Manager`

### Parameters

* `id` (`int`, path): Mã định danh đơn hàng cần hủy

### Response Thành công (`200 OK`)
json
{
  "isSuccess": true,
  "message": "Hủy đơn hàng thành công, đã hoàn kho và thu hồi điểm thưởng",
  "data": true,
  "errors": null
}


### Phản hồi lỗi nghiệp vụ (`400 Bad Request`)

json
{
  "isSuccess": false,
  "message": "Không thể hủy đơn hàng",
  "data": null,
  "errors": ["Đơn hàng này đã bị hủy trước đó"]
}
