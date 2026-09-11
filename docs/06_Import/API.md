# THIẾT KẾ DANH SÁCH REST API QUẢN LÝ NHẬP HÀNG (IMPORT API SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh sách REST API Endpoints Phân hệ 06](#21-tổng-quan-danh-sách-rest-api-endpoints-phân-hệ-06)
   - 2.2. [Chi tiết API 1: Lấy danh sách phiếu nhập](#22-chi-tiết-api-1-lấy-danh-sách-phiếu-nhập)
   - 2.3. [Chi tiết API 2: Chi tiết phiếu nhập](#23-chi-tiết-api-2-chi-tiết-phiếu-nhập)
   - 2.4. [Chi tiết API 3: Tạo phiếu nhập mới](#24-chi-tiết-api-3-tạo-phiếu-nhập-mới)
   - 2.5. [Chi tiết API 4: Thêm sản phẩm vào phiếu nhập](#25-chi-tiết-api-4-thêm-sản-phẩm-vào-phiếu-nhập)
   - 2.6. [Chi tiết API 5: Xác nhận phiếu nhập (Confirm)](#26-chi-tiết-api-5-xác-nhận-phiếu-nhập-confirm)
   - 2.7. [Chi tiết API 6: Hủy phiếu nhập (Cancel)](#27-chi-tiết-api-6-hủy-phiếu-nhập-cancel)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **API.md** định nghĩa chi tiết hợp đồng giao tiếp (API Contract) cho toàn bộ các Endpoints thuộc phân hệ **06_Import**. Tài liệu quy định URL, HTTP Method, cấu trúc Request Body, Query Parameters, Response Body (`ApiResult<T>`) và các mã lỗi HTTP tương ứng.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh sách REST API Endpoints Phân hệ 06

| HTTP Method | Endpoint URL | Vai trò | Mục đích sử dụng |
| :---: | :--- | :---: | :--- |
| `GET` | `/api/import-receipts` | Admin, Manager | Danh sách phiếu nhập (filter, phân trang) |
| `GET` | `/api/import-receipts/{id}` | Admin, Manager | Chi tiết phiếu nhập kèm danh sách sản phẩm |
| `POST` | `/api/import-receipts` | Admin, Manager | Tạo phiếu nhập mới (trạng thái Draft) |
| `POST` | `/api/import-receipts/{id}/details` | Admin, Manager | Thêm sản phẩm vào phiếu Draft |
| `POST` | `/api/import-receipts/{id}/confirm` | Admin, Manager | Xác nhận phiếu → Cập nhật tồn kho |
| `DELETE` | `/api/import-receipts/{id}` | Admin, Manager | Hủy phiếu Draft (Cancel) |

---

### 2.2. Chi tiết API 1: Lấy danh sách phiếu nhập

**`GET /api/import-receipts`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Query Parameters**:

| Tham số | Kiểu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `branchId` | `int` | ❌ | Lọc theo chi nhánh |
| `supplierId` | `int` | ❌ | Lọc theo nhà cung cấp |
| `status` | `int` | ❌ | `1=Draft`, `2=Confirmed`, `3=Cancelled` |
| `fromDate` | `date` | ❌ | Từ ngày nhập (yyyy-MM-dd) |
| `toDate` | `date` | ❌ | Đến ngày nhập (yyyy-MM-dd) |
| `page` | `int` | ❌ | Trang (mặc định: 1) |
| `pageSize` | `int` | ❌ | Số bản ghi/trang (mặc định: 20) |

- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách phiếu nhập thành công",
  "data": {
    "items": [
      {
        "importReceiptId": 15,
        "receiptCode": "IMP-20260911-0015",
        "supplierName": "Công ty TNHH PepsiCo Việt Nam",
        "importedByUser": "Nguyễn Thị Manager",
        "importDate": "2026-09-11T08:00:00Z",
        "totalAmount": 1000000.00,
        "status": "Confirmed",
        "itemCount": 2
      },
      {
        "importReceiptId": 16,
        "receiptCode": "IMP-20260911-0016",
        "supplierName": "Công ty CP Acecook Việt Nam",
        "importedByUser": "Trần Văn Manager",
        "importDate": "2026-09-11T14:00:00Z",
        "totalAmount": 0.00,
        "status": "Draft",
        "itemCount": 0
      }
    ],
    "totalCount": 48,
    "page": 1,
    "pageSize": 20
  },
  "errors": null
}
```

---

### 2.3. Chi tiết API 2: Chi tiết phiếu nhập

**`GET /api/import-receipts/{id}`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy chi tiết phiếu nhập thành công",
  "data": {
    "importReceiptId": 15,
    "receiptCode": "IMP-20260825-0015",
    "supplierId": 3,
    "supplierName": "Công ty TNHH PepsiCo Việt Nam",
    "branchId": 1,
    "branchName": "Chi nhánh Quận 1",
    "importedByUser": "Nguyễn Thị Manager",
    "importDate": "2026-08-25T08:00:00Z",
    "confirmedAt": "2026-08-25T09:00:00Z",
    "confirmedByUser": "Nguyễn Thị Manager",
    "totalAmount": 1000000.00,
    "status": "Confirmed",
    "note": null,
    "details": [
      {
        "importDetailId": 28,
        "productId": 5,
        "productName": "Pepsi 330ml",
        "barcode": "8934588010215",
        "unit": "Lon",
        "quantity": 50,
        "costPrice": 8000.00,
        "expiryDate": "2026-09-20",
        "subTotal": 400000.00
      },
      {
        "importDetailId": 29,
        "productId": 5,
        "productName": "Pepsi 330ml",
        "barcode": "8934588010215",
        "unit": "Lon",
        "quantity": 80,
        "costPrice": 7500.00,
        "expiryDate": "2026-12-31",
        "subTotal": 600000.00
      }
    ]
  },
  "errors": null
}
```

---

### 2.4. Chi tiết API 3: Tạo phiếu nhập mới

**`POST /api/import-receipts`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Request Body (JSON)**:
```json
{
  "supplierId": 3,
  "branchId": 1,
  "note": "Nhập hàng định kỳ tuần 3 tháng 9"
}
```
- **Response thành công (201 Created)**:
```json
{
  "isSuccess": true,
  "message": "Tạo phiếu nhập hàng thành công",
  "data": {
    "importReceiptId": 17,
    "receiptCode": "IMP-20260911-0017",
    "supplierId": 3,
    "branchId": 1,
    "status": "Draft",
    "totalAmount": 0.00,
    "importDate": "2026-09-11T19:00:00Z"
  },
  "errors": null
}
```
- **Response lỗi (400 Bad Request)**:
```json
{
  "isSuccess": false,
  "message": "Tạo phiếu nhập thất bại",
  "data": null,
  "errors": ["Nhà cung cấp với SupplierId = 99 không tồn tại hoặc đã ngưng hoạt động"]
}
```

---

### 2.5. Chi tiết API 4: Thêm sản phẩm vào phiếu nhập

**`POST /api/import-receipts/{id}/details`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Request Body (JSON)**:
```json
{
  "productId": 12,
  "quantity": 200,
  "costPrice": 3500.00,
  "expiryDate": "2027-06-30"
}
```
- **Response thành công (201 Created)**:
```json
{
  "isSuccess": true,
  "message": "Thêm sản phẩm vào phiếu nhập thành công",
  "data": {
    "importDetailId": 35,
    "importReceiptId": 17,
    "productId": 12,
    "productName": "Mì Hảo Hảo Tôm Chua Cay 75g",
    "quantity": 200,
    "costPrice": 3500.00,
    "expiryDate": "2027-06-30",
    "subTotal": 700000.00,
    "receiptTotalAmount": 700000.00
  },
  "errors": null
}
```
- **Response lỗi (400 Bad Request) — HSD không hợp lệ**:
```json
{
  "isSuccess": false,
  "message": "Thêm sản phẩm thất bại",
  "data": null,
  "errors": ["Hạn sử dụng 2026-09-14 quá gần (còn 3 ngày). Hệ thống yêu cầu HSD phải cách ngày nhập ít nhất 7 ngày"]
}
```
- **Response lỗi (409 Conflict) — Phiếu đã Confirmed**:
```json
{
  "isSuccess": false,
  "message": "Không thể chỉnh sửa phiếu nhập",
  "data": null,
  "errors": ["Phiếu nhập IMP-20260825-0015 đã ở trạng thái Confirmed. Không thể thêm sản phẩm"]
}
```

---

### 2.6. Chi tiết API 5: Xác nhận phiếu nhập (Confirm)

**`POST /api/import-receipts/{id}/confirm`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Request Body**: Không cần (chỉ cần `{id}` trong URL)
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Xác nhận phiếu nhập thành công. Đã cập nhật tồn kho cho 2 sản phẩm",
  "data": {
    "importReceiptId": 17,
    "receiptCode": "IMP-20260911-0017",
    "status": "Confirmed",
    "confirmedAt": "2026-09-11T19:30:00Z",
    "confirmedByUser": "Nguyễn Thị Manager",
    "totalAmount": 1150000.00,
    "inventoryUpdated": [
      { "productName": "Mì Hảo Hảo Tôm Chua Cay 75g", "quantityAdded": 200, "newQuantityOnHand": 350 },
      { "productName": "Bánh Oreo Chocolate 137g", "quantityAdded": 100, "newQuantityOnHand": 102 }
    ]
  },
  "errors": null
}
```
- **Response lỗi (400 Bad Request) — Phiếu rỗng**:
```json
{
  "isSuccess": false,
  "message": "Không thể xác nhận phiếu nhập",
  "data": null,
  "errors": ["Phiếu nhập phải có ít nhất 1 sản phẩm trước khi xác nhận"]
}
```

---

### 2.7. Chi tiết API 6: Hủy phiếu nhập (Cancel)

**`DELETE /api/import-receipts/{id}`**

- **Xác thực**: Bearer JWT Token — Roles: Admin, Manager
- **Response thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Hủy phiếu nhập thành công",
  "data": {
    "importReceiptId": 16,
    "receiptCode": "IMP-20260911-0016",
    "status": "Cancelled"
  },
  "errors": null
}
```
- **Response lỗi (400 Bad Request) — Phiếu đã Confirmed**:
```json
{
  "isSuccess": false,
  "message": "Không thể hủy phiếu nhập",
  "data": null,
  "errors": ["Phiếu nhập IMP-20260825-0015 đã ở trạng thái Confirmed. Chỉ được hủy phiếu Draft"]
}
```

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần API `DELETE /api/import-receipts/{id}/details/{detailId}` để xóa từng dòng chi tiết không? | Ảnh hưởng UX WinForms khi nhập sai. | Đề xuất: Có — thêm endpoint này để Manager xóa dòng nhầm trong phiếu Draft mà không cần xóa toàn bộ phiếu. |

---

## 4. GHI CHÚ
- Endpoint `POST /api/import-receipts/{id}/confirm` là endpoint quan trọng nhất — cần test kỹ với Transaction rollback scenario.
- Tất cả response bọc trong `ApiResult<T>` theo `CodingConvention.md`.
- Phân quyền: Thêm `[Authorize(Roles = "Admin,Manager")]` trên toàn bộ `ImportReceiptController`.

---

## 5. KẾT LUẬN

Tài liệu `API.md` đã định nghĩa đầy đủ 6 REST API Endpoints cho phân hệ `06_Import`. Endpoint Confirm (`POST /api/import-receipts/{id}/confirm`) là trung tâm của toàn bộ luồng nhập hàng — kết nối trực tiếp với `05_Inventory` để cập nhật tồn kho và ghi lịch sử biến động trong Smart SuperMarket.
