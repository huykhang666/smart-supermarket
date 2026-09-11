# THIẾT KẾ DANH SÁCH REST API QUẢN LÝ TỒN KHO (INVENTORY API SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh sách REST API Endpoints Phân hệ 05](#21-tổng-quan-danh-sách-rest-api-endpoints-phân-hệ-05)
   - 2.2. [Chi tiết API 1: Lấy danh sách tồn kho](#22-chi-tiết-api-1-lấy-danh-sách-tồn-kho)
   - 2.3. [Chi tiết API 2: Chi tiết tồn kho theo sản phẩm](#23-chi-tiết-api-2-chi-tiết-tồn-kho-theo-sản-phẩm)
   - 2.4. [Chi tiết API 3: Danh sách sản phẩm sắp hết hàng](#24-chi-tiết-api-3-danh-sách-sản-phẩm-sắp-hết-hàng)
   - 2.5. [Chi tiết API 4: Danh sách sản phẩm sắp hết hạn](#25-chi-tiết-api-4-danh-sách-sản-phẩm-sắp-hết-hạn)
   - 2.6. [Chi tiết API 5: Điều chỉnh tồn kho thủ công](#26-chi-tiết-api-5-điều-chỉnh-tồn-kho-thủ-công)
   - 2.7. [Chi tiết API 6: Lịch sử biến động tồn kho](#27-chi-tiết-api-6-lịch-sử-biến-động-tồn-kho)
   - 2.8. [Chi tiết API 7: Xem danh sách DiscountRule](#28-chi-tiết-api-7-xem-danh-sách-discountrule)
   - 2.9. [Chi tiết API 8: Cập nhật DiscountRule](#29-chi-tiết-api-8-cập-nhật-discountrule)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **API.md** định nghĩa chi tiết hợp đồng giao tiếp (API Contract) cho toàn bộ các Endpoints thuộc phân hệ **05_Inventory**. Tài liệu quy định URL, HTTP Method, cấu trúc Request Body, Query Parameters, Response Body (`ApiResult<T>`) và các mã lỗi HTTP tương ứng.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh sách REST API Endpoints Phân hệ 05

| HTTP Method | Endpoint URL | Vai trò được gọi | Mục đích sử dụng |
| :---: | :--- | :---: | :--- |
| `GET` | `/api/inventory` | Admin, Manager, Staff | Lấy danh sách tồn kho (filter theo branchId, trạng thái cảnh báo) |
| `GET` | `/api/inventory/{productId}` | Admin, Manager, Staff | Chi tiết tồn kho của 1 sản phẩm tại chi nhánh hiện tại |
| `GET` | `/api/inventory/low-stock` | Admin, Manager, Staff | Danh sách sản phẩm tồn kho ≤ MinStockLevel |
| `GET` | `/api/inventory/expiring` | Admin, Manager, Staff | Danh sách sản phẩm có lô hàng sắp hết hạn (≤ 15 ngày) |
| `PUT` | `/api/inventory/{productId}/adjust` | Admin, Manager | Điều chỉnh tồn kho thủ công (kiểm kê) |
| `GET` | `/api/inventory/stock-history` | Admin, Manager | Lịch sử biến động tồn kho (có filter, phân trang) |
| `GET` | `/api/discount-rules` | Admin, Manager, Staff | Xem danh sách quy tắc giảm giá theo HSD |
| `PUT` | `/api/discount-rules/{id}` | Admin | Admin cập nhật ngưỡng/mức giảm giá theo HSD |

---

### 2.2. Chi tiết API 1: Lấy danh sách tồn kho

**`GET /api/inventory`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager, Staff
- **Query Parameters**:

| Tham số | Kiểu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `branchId` | `int` | ❌ | Lọc theo chi nhánh (mặc định: chi nhánh của user hiện tại) |
| `status` | `string` | ❌ | `"low"` = chỉ sản phẩm LowStock, `"normal"` = bình thường, bỏ trống = tất cả |
| `search` | `string` | ❌ | Tìm theo tên sản phẩm hoặc barcode |
| `page` | `int` | ❌ | Trang hiện tại (mặc định: 1) |
| `pageSize` | `int` | ❌ | Số bản ghi mỗi trang (mặc định: 20) |

- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách tồn kho thành công",
  "data": {
    "items": [
      {
        "inventoryId": 1,
        "productId": 5,
        "productName": "Pepsi 330ml",
        "barcode": "8934588010215",
        "unit": "Lon",
        "quantityOnHand": 4,
        "minStockLevel": 10,
        "isLowStock": true,
        "nearestExpiryDate": "2026-09-18",
        "daysUntilExpiry": 7,
        "lastUpdated": "2026-09-11T10:30:00Z"
      },
      {
        "inventoryId": 2,
        "productId": 12,
        "productName": "Mì Hảo Hảo Tôm Chua Cay 75g",
        "barcode": "8935001710048",
        "unit": "Gói",
        "quantityOnHand": 150,
        "minStockLevel": 20,
        "isLowStock": false,
        "nearestExpiryDate": "2027-03-15",
        "daysUntilExpiry": 185,
        "lastUpdated": "2026-09-10T08:00:00Z"
      }
    ],
    "totalCount": 45,
    "page": 1,
    "pageSize": 20
  },
  "errors": null
}
```

---

### 2.3. Chi tiết API 2: Chi tiết tồn kho theo sản phẩm

**`GET /api/inventory/{productId}`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager, Staff
- **Path Parameter**: `productId` (int) — Mã sản phẩm cần xem tồn kho
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy chi tiết tồn kho thành công",
  "data": {
    "inventoryId": 1,
    "productId": 5,
    "productName": "Pepsi 330ml",
    "categoryName": "Nước ngọt",
    "unit": "Lon",
    "quantityOnHand": 4,
    "minStockLevel": 10,
    "isLowStock": true,
    "stockBatches": [
      {
        "importReceiptId": 8,
        "receiptCode": "IMP-20260825-0008",
        "quantity": 4,
        "expiryDate": "2026-09-18",
        "daysUntilExpiry": 7,
        "applicableDiscount": 50.00
      }
    ],
    "lastUpdated": "2026-09-11T10:30:00Z"
  },
  "errors": null
}
```
- **Response lỗi (404 Not Found)**:
```json
{
  "isSuccess": false,
  "message": "Không tìm thấy thông tin tồn kho cho sản phẩm này",
  "data": null,
  "errors": ["Sản phẩm với ProductId = 999 không tồn tại hoặc chưa có trong kho"]
}
```

---

### 2.4. Chi tiết API 3: Danh sách sản phẩm sắp hết hàng

**`GET /api/inventory/low-stock`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager, Staff
- **Query Parameters**: `branchId` (int, tùy chọn), `top` (int, mặc định 10)
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách sản phẩm sắp hết hàng thành công",
  "data": [
    {
      "productId": 5,
      "productName": "Pepsi 330ml",
      "quantityOnHand": 4,
      "minStockLevel": 10,
      "shortage": 6,
      "unit": "Lon"
    },
    {
      "productId": 18,
      "productName": "Bánh Oreo Chocolate 137g",
      "quantityOnHand": 2,
      "minStockLevel": 15,
      "shortage": 13,
      "unit": "Hộp"
    }
  ],
  "errors": null
}
```

---

### 2.5. Chi tiết API 4: Danh sách sản phẩm sắp hết hạn

**`GET /api/inventory/expiring`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager, Staff
- **Query Parameters**: `branchId` (int, tùy chọn), `withinDays` (int, mặc định 15)
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách sản phẩm sắp hết hạn thành công",
  "data": [
    {
      "productId": 5,
      "productName": "Pepsi 330ml",
      "quantity": 4,
      "expiryDate": "2026-09-18",
      "daysUntilExpiry": 7,
      "urgencyLevel": "Critical",
      "suggestedDiscountPercent": 50.00
    },
    {
      "productId": 22,
      "productName": "Sữa tươi Vinamilk 1L",
      "quantity": 15,
      "expiryDate": "2026-09-24",
      "daysUntilExpiry": 13,
      "urgencyLevel": "Warning",
      "suggestedDiscountPercent": 20.00
    }
  ],
  "errors": null
}
```

---

### 2.6. Chi tiết API 5: Điều chỉnh tồn kho thủ công

**`PUT /api/inventory/{productId}/adjust`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Request Body (JSON)**:
```json
{
  "branchId": 1,
  "quantityChange": -3,
  "note": "Kiểm kê thực tế phát hiện thiếu 3 chai Pepsi do vỡ vỡ khi vận chuyển"
}
```
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Điều chỉnh tồn kho thành công",
  "data": {
    "productId": 5,
    "productName": "Pepsi 330ml",
    "quantityBefore": 7,
    "quantityChange": -3,
    "quantityAfter": 4,
    "adjustedAt": "2026-09-11T14:00:00Z",
    "adjustedBy": "Nguyễn Văn Manager"
  },
  "errors": null
}
```
- **Response lỗi (400 Bad Request)**:
```json
{
  "isSuccess": false,
  "message": "Điều chỉnh tồn kho thất bại",
  "data": null,
  "errors": ["Số lượng sau điều chỉnh không được âm. Tồn kho hiện tại: 4, điều chỉnh giảm: 10"]
}
```

---

### 2.7. Chi tiết API 6: Lịch sử biến động tồn kho

**`GET /api/inventory/stock-history`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Query Parameters**:

| Tham số | Kiểu | Mô tả |
| :--- | :--- | :--- |
| `productId` | `int` | Lọc theo sản phẩm |
| `branchId` | `int` | Lọc theo chi nhánh |
| `changeType` | `int` | `1=Import`, `2=Sale`, `3=Adjustment`, `4=Expired` |
| `fromDate` | `datetime` | Từ ngày |
| `toDate` | `datetime` | Đến ngày |
| `page` | `int` | Trang hiện tại |

- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy lịch sử tồn kho thành công",
  "data": {
    "items": [
      {
        "stockHistoryId": 102,
        "productName": "Pepsi 330ml",
        "changeType": "Import",
        "quantityChange": 100,
        "quantityBefore": 4,
        "quantityAfter": 104,
        "expiryDate": "2026-12-31",
        "referenceId": 15,
        "note": null,
        "createdAt": "2026-09-11T08:00:00Z",
        "createdByUser": "Trần Thị Manager"
      }
    ],
    "totalCount": 250,
    "page": 1,
    "pageSize": 20
  },
  "errors": null
}
```

---

### 2.8. Chi tiết API 7: Xem danh sách DiscountRule

**`GET /api/discount-rules`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager, Staff
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách quy tắc giảm giá thành công",
  "data": [
    {
      "discountRuleId": 1,
      "daysBeforeExpiry": 7,
      "discountPercent": 50.00,
      "isActive": true,
      "description": "Sản phẩm còn ≤ 7 ngày HSD — Giảm 50%"
    },
    {
      "discountRuleId": 2,
      "daysBeforeExpiry": 15,
      "discountPercent": 20.00,
      "isActive": true,
      "description": "Sản phẩm còn ≤ 15 ngày HSD — Giảm 20%"
    }
  ],
  "errors": null
}
```

---

### 2.9. Chi tiết API 8: Cập nhật DiscountRule

**`PUT /api/discount-rules/{id}`**

- **Xác thực**: Bearer JWT Token — Roles: Admin
- **Request Body (JSON)**:
```json
{
  "daysBeforeExpiry": 7,
  "discountPercent": 60.00,
  "isActive": true,
  "description": "Cập nhật: còn ≤ 7 ngày HSD giảm 60%"
}
```
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Cập nhật quy tắc giảm giá thành công",
  "data": {
    "discountRuleId": 1,
    "daysBeforeExpiry": 7,
    "discountPercent": 60.00,
    "isActive": true
  },
  "errors": null
}
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | API `GET /api/inventory` nên trả về tồn kho theo chi nhánh của Staff đang đăng nhập hay cho phép chọn bất kỳ chi nhánh? | Ảnh hưởng logic filter và phân quyền. | Đề xuất: Staff chỉ thấy chi nhánh của mình (`BranchId` lấy từ JWT Claims). Admin/Manager có thể truyền `branchId` tùy ý. |

---

## 4. GHI CHÚ
- Tất cả response đều bọc trong `ApiResult<T>` theo chuẩn quy định tại `CodingConvention.md`.
- Endpoint `/api/inventory/low-stock` và `/api/inventory/expiring` được gọi bởi Dashboard (12_Report) và AI module (10_AI).
- Cần thêm Index vào cột `ExpiryDate` của bảng `StockHistory` để query expiring products nhanh hơn.

---

## 5. KẾT LUẬN

Tài liệu `API.md` đã định nghĩa đầy đủ 8 REST API Endpoints cho phân hệ `05_Inventory`. Các endpoint này cung cấp nền tảng dữ liệu cho giao diện WinForms quản lý kho, Dashboard tổng quan và module AI phân tích tồn kho trong hệ thống Smart SuperMarket.
