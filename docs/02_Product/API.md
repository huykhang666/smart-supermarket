# THIẾT KẾ DANH SÁCH REST API PHÂN HỆ SẢN PHẨM (PRODUCT API SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh sách REST API Endpoints](#21-tổng-quan-danh-sách-rest-api-endpoints)
   - 2.2. [Chi tiết API 1: Lấy Danh sách Sản phẩm Phân trang (`GET /api/products`)](#22-chi-tiết-api-1-lấy-danh-sách-sản-phẩm-phân-trang-get-apiproducts)
   - 2.3. [Chi tiết API 2: Lấy Chi tiết Sản phẩm theo ID (`GET /api/products/{id}`)](#23-chi-tiết-api-2-lấy-chi-tiết-sản-phẩm-theo-id-get-apiproductsid)
   - 2.4. [Chi tiết API 3: Tra cứu Sản phẩm nhanh theo Mã vạch POS (`GET /api/products/barcode/{barcode}`)](#24-chi-tiết-api-3-tra-cứu-sản-phẩm-nhanh-theo-mã-vạch-pos-get-apiproductsbarcodebarcode)
   - 2.5. [Chi tiết API 4: Tạo mới Sản phẩm (`POST /api/products`)](#25-chi-tiết-api-4-tạo-mới-sản-phẩm-post-apiproducts)
   - 2.6. [Chi tiết API 5: Cập nhật Thông tin Sản phẩm (`PUT /api/products/{id}`)](#26-chi-tiết-api-5-cập-nhật-thông-tin-sản-phẩm-put-apiproductsid)
   - 2.7. [Chi tiết API 6: Khóa/Ngừng kinh doanh Sản phẩm (`DELETE /api/products/{id}`)](#27-chi-tiết-api-6-khóangừng-kinh-doanh-sản-phẩm-delete-apiproductsid)
   - 2.8. [Chi tiết API 7: Tải lên Hình ảnh Sản phẩm (`POST /api/products/upload-image`)](#28-chi-tiết-api-7-tải-lên-hình-ảnh-sản-phẩm-post-apiproductsupload-image)
   - 2.9. [Chi tiết API 8: Tự động Sinh Mã vạch GS1 EAN-13 (`POST /api/products/generate-barcode`)](#29-chi-tiết-api-8-tự-động-sinh-mã-vạch-gs1-ean-13-post-apiproductsgenerate-barcode)
   - 2.10. [Chi tiết API 9: Cập nhật & Xem Biên lợi nhuận Giá (`PUT /api/products/{id}/price`)](#210-chi-tiết-api-9-cập-nhật--xem-biên-lợi-nhuận-giá-put-apiproductsidprice)
   - 2.11. [Chi tiết API 10: Quản lý Danh mục Sản phẩm (`/api/category`)](#211-chi-tiết-api-10-quản-lý-danh-mục-sản-phẩm-apicategory)
   - 2.12. [Chi tiết API 11: Quản lý Nhà cung cấp (`/api/supplier`)](#212-chi-tiết-api-11-quản-lý-nhà-cung-cấp-apisupplier)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **API.md** định nghĩa chi tiết Hợp đồng Giao tiếp RESTful API (API Contract) cho phân hệ **02_Product (Quản lý Sản phẩm)** thuộc hệ thống **Smart SuperMarket**. Tài liệu quy định cụ thể URL, HTTP Method, Header xác thực JWT Bearer, tham số Query, cấu trúc JSON Request/Response bọc trong vỏ chuẩn `ApiResult<T>` và các mã lỗi HTTP tương ứng.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh sách REST API Endpoints

| HTTP Method | Endpoint URL | Vai trò được phép gọi | Mục đích nghiệp vụ |
| :---: | :--- | :---: | :--- |
| `GET` | `/api/products` | Public / Customer / Staff / Admin | Lấy danh sách sản phẩm (Hỗ trợ lọc, tìm kiếm, phân trang) |
| `GET` | `/api/products/{id}` | Public / Customer / Staff / Admin | Lấy chi tiết thông tin 1 sản phẩm theo `ProductId` |
| `GET` | `/api/products/barcode/{barcode}` | Staff / Admin / Public | Tra cứu tức thì sản phẩm khi quét mã vạch Barcode tại POS |
| `POST` | `/api/products` | Admin / Manager | Tạo mới bản ghi sản phẩm vào CSDL |
| `PUT` | `/api/products/{id}` | Admin / Manager | Cập nhật thông tin chi tiết sản phẩm |
| `DELETE`| `/api/products/{id}` | Admin / Manager | Đổi trạng thái sản phẩm sang `Ngừng kinh doanh` (Soft Delete) |
| `POST` | `/api/products/upload-image` | Admin / Manager | Upload file hình ảnh sản phẩm lên Server |
| `POST` | `/api/products/generate-barcode` | Admin / Manager / Staff | Sinh mã vạch nội bộ GS1 EAN-13 tự động |
| `PUT` | `/api/products/{id}/price` | Admin / Manager | Cập nhật giá bán niêm yết và giá vốn |
| `GET` | `/api/products/{id}/price-history` | Admin / Manager | Xem thông tin biên lợi nhuận và cờ cảnh báo bán lỗ |
| `GET/POST/PUT/DELETE` | `/api/category` | Public / Admin / Manager | Quản lý danh mục sản phẩm (CRUD) |
| `GET/POST/PUT/DELETE` | `/api/supplier` | Public / Admin / Manager | Quản lý nhà cung cấp chính (CRUD) |

---

### 2.2. Chi tiết API 1: Lấy Danh sách Sản phẩm Phân trang (`GET /api/products`)

- **Xác thực**: Public / Bearer Token (Tùy chọn).
- **Query Parameters**:
  - `search` (string, optional): Từ khóa tìm kiếm theo tên sản phẩm hoặc mã vạch.
  - `categoryId` (int, optional): Lọc theo ID danh mục.
  - `supplierId` (int, optional): Lọc theo ID nhà cung cấp chính.
  - `status` (byte, optional): `1` = Đang bán, `2` = Ngừng kinh doanh. Mặc định `1`.
  - `minPrice` (decimal, optional): Giá bán nhỏ nhất.
  - `maxPrice` (decimal, optional): Giá bán lớn nhất.
  - `page` (int, default = 1): Trang hiện tại.
  - `pageSize` (int, default = 10): Số lượng bản ghi trên 1 trang.
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy danh sách sản phẩm thành công.",
  "data": {
    "items": [
      {
        "productId": 1,
        "productName": "Nước ngọt Coca-Cola Lon 330ml",
        "barcode": "8935001800012",
        "categoryId": 1,
        "categoryName": "Nước giải khát",
        "supplierId": 1,
        "supplierName": "Công ty TNHH Coca-Cola Việt Nam",
        "price": 10000.00,
        "costPrice": 7500.00,
        "imageUrl": "/images/products/coca_330ml.jpg",
        "unit": "lon",
        "status": "Active",
        "createdAt": "2026-09-01T08:00:00Z"
      }
    ],
    "totalCount": 45,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5
  },
  "errors": null
}
```

---

### 2.3. Chi tiết API 2: Lấy Chi tiết Sản phẩm theo ID (`GET /api/products/{id}`)

- **Xác thực**: Public / Authenticated.
- **Path Parameter**: `id` (int, required) — ID sản phẩm.
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Lấy thông tin sản phẩm thành công.",
  "data": {
    "productId": 1,
    "productName": "Nước ngọt Coca-Cola Lon 330ml",
    "barcode": "8935001800012",
    "categoryId": 1,
    "categoryName": "Nước giải khát",
    "supplierId": 1,
    "supplierName": "Công ty TNHH Coca-Cola Việt Nam",
    "price": 10000.00,
    "costPrice": 7500.00,
    "imageUrl": "/images/products/coca_330ml.jpg",
    "unit": "lon",
    "status": "Active",
    "createdAt": "2026-09-01T08:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

---

### 2.4. Chi tiết API 3: Tra cứu Sản phẩm nhanh theo Mã vạch POS (`GET /api/products/barcode/{barcode}`)

- **Xác thực**: Public / Authenticated (Dành cho WinForms POS quét mã vạch).
- **Path Parameter**: `barcode` (string, required) — Chuỗi mã vạch vừa quét được.
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Tra cứu mã vạch thành công.",
  "data": {
    "productId": 1,
    "productName": "Nước ngọt Coca-Cola Lon 330ml",
    "barcode": "8935001800012",
    "price": 10000.00,
    "unit": "lon",
    "status": "Active",
    "categoryName": "Nước giải khát"
  },
  "errors": null
}
```

---

### 2.5. Chi tiết API 4: Tạo mới Sản phẩm (`POST /api/products`)

- **Xác thực**: Header `Authorization: Bearer <JWT>` (Yêu cầu Role `Admin` hoặc `Manager`).
- **Request Body (JSON)**:
```json
{
  "productName": "Sữa tươi tiệt trùng Vinamilk Có đường 1L",
  "barcode": "8934673123456",
  "categoryId": 2,
  "supplierId": 2,
  "price": 36000.00,
  "costPrice": 29000.00,
  "imageUrl": "/images/products/vinamilk_1l.jpg",
  "unit": "hộp"
}
```
- **Response Thành công (201 Created)**:
```json
{
  "isSuccess": true,
  "message": "Tạo mới sản phẩm thành công.",
  "data": {
    "productId": 2,
    "productName": "Sữa tươi tiệt trùng Vinamilk Có đường 1L",
    "barcode": "8934673123456",
    "categoryId": 2,
    "supplierId": 2,
    "price": 36000.00,
    "costPrice": 29000.00,
    "unit": "hộp",
    "status": "Active",
    "createdAt": "2026-09-09T08:30:00Z"
  },
  "errors": null
}
```

---

### 2.6. Chi tiết API 5: Cập nhật Thông tin Sản phẩm (`PUT /api/products/{id}`)

- **Xác thực**: Header `Authorization: Bearer <JWT>` (Yêu cầu Role `Admin` hoặc `Manager`).
- **Path Parameter**: `id` (int, required).
- **Response Thành công (200 OK)**: Trả về đối tượng `ProductDto` đã cập nhật.

---

### 2.7. Chi tiết API 6: Khóa/Ngừng kinh doanh Sản phẩm (`DELETE /api/products/{id}`)

- **Xác thực**: Header `Authorization: Bearer <JWT>` (Yêu cầu Role `Admin` hoặc `Manager`).
- **Path Parameter**: `id` (int, required).
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Đã chuyển trạng thái sản phẩm sang Ngừng kinh doanh thành công.",
  "data": true,
  "errors": null
}
```

---

### 2.8. Chi tiết API 7: Tải lên Hình ảnh Sản phẩm (`POST /api/products/upload-image`)

- **Xác thực**: Header `Authorization: Bearer <JWT>` (Yêu cầu Role `Admin` hoặc `Manager`).
- **Content-Type**: `multipart/form-data`.
- **Form Data**: `file` (Binary File image), `productId` (int).
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Tải lên hình ảnh thành công.",
  "data": {
    "imageUrl": "/images/products/prod_1_a1b2c3d4.jpg"
  },
  "errors": null
}
```

---

### 2.9. Chi tiết API 8: Tự động Sinh Mã vạch GS1 EAN-13 (`POST /api/products/generate-barcode`)

- **Xác thực**: Authenticated / Public.
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Sinh mã vạch EAN-13 thành công.",
  "data": "2000010100009",
  "errors": null
}
```

---

### 2.10. Chi tiết API 9: Cập nhật & Xem Biên lợi nhuận Giá (`PUT /api/products/{id}/price`)

- **Xác thực**: Header `Authorization: Bearer <JWT>` (Yêu cầu Role `Admin` hoặc `Manager`).
- **Response Thành công (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Cập nhật giá sản phẩm thành công.",
  "data": {
    "productId": 1,
    "productName": "Nước ngọt Coca-Cola Lon 330ml",
    "price": 10000.00,
    "costPrice": 7500.00,
    "grossProfit": 2500.00,
    "profitMarginPercentage": 25.0,
    "isNegativeMarginWarning": false
  },
  "errors": null
}
```

---

### 2.11. Chi tiết API 10: Quản lý Danh mục Sản phẩm (`/api/category`)

- `GET /api/category`: Lấy tất cả hoặc tìm kiếm phân trang.
- `GET /api/category/{id}`: Chi tiết danh mục.
- `POST /api/category`: Tạo mới danh mục.
- `PUT /api/category/{id}`: Cập nhật danh mục.
- `DELETE /api/category/{id}`: Xóa danh mục.

---

### 2.12. Chi tiết API 11: Quản lý Nhà cung cấp (`/api/supplier`)

- `GET /api/supplier`: Lấy tất cả hoặc tìm kiếm phân trang.
- `GET /api/supplier/{id}`: Chi tiết nhà cung cấp.
- `POST /api/supplier`: Tạo mới nhà cung cấp.
- `PUT /api/supplier/{id}`: Cập nhật nhà cung cấp.
- `DELETE /api/supplier/{id}`: Xóa nhà cung cấp.

---

## 3. GHI CHÚ
- Khi gọi các API tạo mới hoặc cập nhật sản phẩm, Client phải đảm bảo giá trị `price >= 0` và `barcode` không rỗng.
- Tất cả các API phản hồi lỗi validation đều trả về mã HTTP `400 Bad Request` hoặc `404 Not Found` bọc trong `ApiResult<T>`.

---

## 4. KẾT LUẬN

Tài liệu `API.md` đã cập nhật đầy đủ toàn bộ Hợp đồng REST API của phân hệ Sản phẩm, bao gồm cả Product, Category, Supplier, Barcode Service và Price Margin features.
