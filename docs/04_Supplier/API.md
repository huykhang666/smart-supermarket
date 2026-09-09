# ĐẶC TẢ KỸ THUẬT RESTFUL API PHÂN HỆ NHÀ CUNG CẤP (SUPPLIER API SPECIFICATION)

---

## MỤC LỤC
1. [Tổng quan API](#1-tổng-quan-api)
2. [Cấu trúc Phản hồi Chuẩn (ApiResult Response Wrapper)](#2-cấu-trúc-phản-hồi-chuẩn-apiresult-response-wrapper)
3. [Danh sách API Endpoints](#3-danh-sách-api-endpoints)
   - 3.1. [Lấy danh sách Nhà cung cấp (Phân trang & Tìm kiếm)](#31-lấy-danh-sách-nhà-cung-cấp-phân-trang--tìm-kiếm)
   - 3.2. [Xem chi tiết 1 Nhà cung cấp theo ID](#32-xem-chi-tiết-1-nhà-cung-cấp-theo-id)
   - 3.3. [Xem danh sách Sản phẩm do Nhà cung cấp phân phối (N-N)](#33-xem-danh-sách-sản-phẩm-do-nhà-cung-cấp-phân-phối-n-n)
   - 3.4. [Tạo mới Nhà cung cấp](#34-tạo-mới-nhà-cung-cấp)
   - 3.5. [Cập nhật thông tin Nhà cung cấp](#35-cập-nhật-thông-tin-nhà-cung-cấp)
   - 3.6. [Xóa mềm Nhà cung cấp (Soft Delete)](#36-xóa-mềm-nhà-cung-cấp-soft-delete)
   - 3.7. [Khôi phục Nhà cung cấp đã xóa mềm](#37-khôi-phục-nhà-cung-cấp-đã-xóa-mềm)
   - 3.8. [Tạo / Cập nhật Liên kết Sản phẩm - NCC (ProductSupplier)](#38-tạo--cập-nhật-liên-kết-sản-phẩm---ncc-productsupplier)
   - 3.9. [Hủy Liên kết Sản phẩm khỏi NCC](#39-hủy-liên-kết-sản-phẩm-khỏi-ncc)
4. [Mã Lỗi HTTP & Xử lý Ngoại lệ](#4-mã-lỗi-http--xử-lý-ngoại-lệ)

---

## 1. TỔNG QUAN API

- **Base URL**: `/api/v1/suppliers`
- **Định dạng dữ liệu**: `application/json`
- **Chuẩn xác thực**: JWT Bearer Token gửi trong Header `Authorization: Bearer <token>`.
- **Phân quyền truy cập**:
  - `Admin`, `Manager`, `Staff`: Quyền đọc/ghi toàn bộ API.
  - `AI Agent`: Chỉ có quyền `GET` (Read-Only).

---

## 2. CẤU TRÚC PHẢN HỒI CHUẨN (APIRESULT RESPONSE WRAPPER)

Toàn bộ API trả về kết quả bọc trong đối tượng `ApiResult<T>`:

```json
{
  "isSuccess": true,
  "message": "Thành công",
  "data": { ... },
  "errors": null
}
```

---

## 3. DANH SÁCH API ENDPOINTS

### 3.1. Lấy danh sách Nhà cung cấp (Phân trang & Tìm kiếm)
- **Endpoint**: `GET /api/v1/suppliers`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`, `AI Agent`
- **Query Parameters**:
  - `search` (string, optional): Từ khóa tìm kiếm theo tên NCC, email, số điện thoại.
  - `status` (byte, optional): `1` = Active, `2` = Inactive.
  - `page` (int, default = 1): Trang hiện tại.
  - `pageSize` (int, default = 10): Số bản ghi/trang.
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách nhà cung cấp thành công.",
  "data": {
    "items": [
      {
        "supplierId": 1,
        "supplierName": "Công ty TNHH NGK Coca-Cola Việt Nam",
        "contactPerson": "Nguyễn Văn A",
        "phoneNumber": "02838291111",
        "email": "contact@cocacola.com.vn",
        "address": "Xa lộ Hà Nội, P. Linh Trung, TP. Thủ Đức, TP.HCM",
        "status": 1,
        "statusName": "Đang hợp tác",
        "productCount": 15,
        "createdAt": "2026-09-01T08:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "errors": null
}
```

---

### 3.2. Xem chi tiết 1 Nhà cung cấp theo ID
- **Endpoint**: `GET /api/v1/suppliers/{id}`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`, `AI Agent`
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Lấy thông tin nhà cung cấp thành công.",
  "data": {
    "supplierId": 1,
    "supplierName": "Công ty TNHH NGK Coca-Cola Việt Nam",
    "contactPerson": "Nguyễn Văn A",
    "phoneNumber": "02838291111",
    "email": "contact@cocacola.com.vn",
    "address": "Xa lộ Hà Nội, P. Linh Trung, TP. Thủ Đức, TP.HCM",
    "status": 1,
    "statusName": "Đang hợp tác",
    "productCount": 15,
    "createdAt": "2026-09-01T08:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

---

### 3.3. Xem danh sách Sản phẩm do Nhà cung cấp phân phối (N-N)
- **Endpoint**: `GET /api/v1/suppliers/{id}/products`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`, `AI Agent`
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách sản phẩm do nhà cung cấp phân phối thành công.",
  "data": [
    {
      "productId": 101,
      "productName": "Nước ngọt Coca-Cola Lon 330ml",
      "barcode": "8935001800012",
      "purchasePrice": 7500.00,
      "supplierProductCode": "KO-330",
      "leadTime": 2,
      "minimumOrderQuantity": 50,
      "rating": 4.80,
      "isDefault": true
    }
  ],
  "errors": null
}
```

---

### 3.4. Tạo mới Nhà cung cấp
- **Endpoint**: `POST /api/v1/suppliers`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Request Body**:
```json
{
  "supplierName": "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
  "contactPerson": "Trần Thị B",
  "phoneNumber": "02854155555",
  "email": "vinamilk@vinamilk.com.vn",
  "address": "Số 10 Tân Trào, Quận 7, TP.HCM"
}
```
- **Response Success (`201 Created`)**:
```json
{
  "isSuccess": true,
  "message": "Tạo mới nhà cung cấp thành công.",
  "data": {
    "supplierId": 2,
    "supplierName": "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
    "contactPerson": "Trần Thị B",
    "phoneNumber": "02854155555",
    "email": "vinamilk@vinamilk.com.vn",
    "address": "Số 10 Tân Trào, Quận 7, TP.HCM",
    "status": 1,
    "statusName": "Đang hợp tác",
    "createdAt": "2026-09-09T20:30:00Z"
  },
  "errors": null
}
```

---

### 3.5. Cập nhật thông tin Nhà cung cấp
- **Endpoint**: `PUT /api/v1/suppliers/{id}`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Request Body**:
```json
{
  "supplierName": "Công ty CP Sữa Việt Nam - Vinamilk",
  "contactPerson": "Trần Thị B (Phụ trách Kinh doanh)",
  "phoneNumber": "02854155555",
  "email": "contact@vinamilk.com.vn",
  "address": "Số 10 Tân Trào, Quận 7, TP.HCM",
  "status": 1
}
```
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Cập nhật nhà cung cấp thành công.",
  "data": { ... },
  "errors": null
}
```

---

### 3.6. Xóa mềm Nhà cung cấp (Soft Delete)
- **Endpoint**: `DELETE /api/v1/suppliers/{id}`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Đã chuyển trạng thái nhà cung cấp sang Tạm ngừng hợp tác thành công.",
  "data": true,
  "errors": null
}
```

---

### 3.7. Khôi phục Nhà cung cấp đã xóa mềm
- **Endpoint**: `POST /api/v1/suppliers/{id}/restore`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Khôi phục trạng thái hợp tác nhà cung cấp thành công.",
  "data": { ... },
  "errors": null
}
```

---

### 3.8. Tạo / Cập nhật Liên kết Sản phẩm - NCC (ProductSupplier)
- **Endpoint**: `POST /api/v1/suppliers/products/link`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Request Body**:
```json
{
  "productId": 101,
  "supplierId": 1,
  "purchasePrice": 7500.00,
  "supplierProductCode": "KO-330",
  "leadTime": 2,
  "minimumOrderQuantity": 50,
  "rating": 4.80,
  "isDefault": true
}
```
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Tạo liên kết cung ứng sản phẩm thành công.",
  "data": true,
  "errors": null
}
```

---

### 3.9. Hủy Liên kết Sản phẩm khỏi NCC
- **Endpoint**: `DELETE /api/v1/suppliers/products/unlink`
- **Quyền truy cập**: `Admin`, `Manager`, `Staff`
- **Query Parameters**: `productId=101&supplierId=1`
- **Response Success (`200 OK`)**:
```json
{
  "isSuccess": true,
  "message": "Đã hủy liên kết sản phẩm khỏi nhà cung cấp thành công.",
  "data": true,
  "errors": null
}
```

---

## 4. MÃ LỖI HTTP & XỬ LÝ NGOẠI LỆ

| Mã HTTP Status Code | Ý nghĩa & Trường hợp xuất hiện |
| :--- | :--- |
| **`200 OK`** | Thao tác thành công. |
| **`201 Created`** | Tạo mới bản ghi thành công. |
| **`400 Bad Request`** | Dữ liệu không hợp lệ (Trùng tên NCC, giá âm, sai định dạng email/sđt). |
| **`401 Unauthorized`** | Chưa đăng nhập hoặc Token JWT hết hạn. |
| **`403 Forbidden`** | Không có quyền truy cập (AI Agent gọi API ghi dữ liệu hoặc Customer gọi API Supplier). |
| **`404 Not Found`** | Không tìm thấy Nhà cung cấp hoặc Sản phẩm yêu cầu. |
| **`500 Internal Server Error`** | Lỗi hệ thống backend chưa lường trước. |
