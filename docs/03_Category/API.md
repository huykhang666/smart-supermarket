# THIẾT KẾ DANH SÁCH REST API PHÂN HỆ DANH MỤC (CATEGORY API SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh sách REST API Endpoints](#21-tổng-quan-danh-sách-rest-api-endpoints)
   - 2.2. [Chi tiết API 1: Tạo mới Danh mục (`POST /api/category`)](#22-chi-tiết-api-1-tạo-mới-danh-mục-post-apicategory)
   - 2.3. [Chi tiết API 2: Cập nhật Danh mục (`PUT /api/category/{id}`)](#23-chi-tiết-api-2-cập-nhật-danh-mục-put-apicategoryid)
   - 2.4. [Chi tiết API 3: Xóa / Soft Delete Danh mục (`DELETE /api/category/{id}`)](#24-chi-tiết-api-3-xóa--soft-delete-danh-mục-delete-apicategoryid)
   - 2.5. [Chi tiết API 4: Điều chuyển Sản phẩm giữa Danh mục (`POST /api/category/move`)](#25-chi-tiết-api-4-điều-chuyển-sản-phẩm-giữa-danh-mục-post-apicategorymove)
   - 2.6. [Chi tiết API 5: Chi tiết Danh mục theo ID (`GET /api/category/{id}`)](#26-chi-tiết-api-5-chi-tiết-danh-mục-theo-id-get-apicategoryid)
   - 2.7. [Chi tiết API 6: Cấu trúc Cây Danh mục (`GET /api/category/tree`)](#27-chi-tiết-api-6-cấu-trúc-cây-danh-mục-get-apicategorytree)
   - 2.8. [Chi tiết API 7: Danh sách Danh mục Dropdown (`GET /api/category/dropdown`)](#28-chi-tiết-api-7-danh-sách-danh-mục-dropdown-get-apicategorydropdown)
   - 2.9. [Chi tiết API 8: Tìm kiếm Danh mục (`GET /api/category/search`)](#29-chi-tiết-api-8-tìm-kiếm-danh-mục-get-apicategorysearch)
   - 2.10. [Chi tiết API 9: Danh sách Sản phẩm thuộc Danh mục (`GET /api/category/{id}/products`)](#210-chi-tiết-api-9-danh-sách-sản-phẩm-thuộc-danh-mục-get-apicategoryidproducts)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **API.md** định nghĩa chi tiết Hợp đồng Giao tiếp RESTful API (API Contract) cho phân hệ **03_Category (Quản lý Danh mục)** thuộc hệ thống **Smart SuperMarket**.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh sách REST API Endpoints

| HTTP Method | Endpoint URL | Vai trò được phép gọi | Mục đích nghiệp vụ |
| :---: | :--- | :---: | :--- |
| `POST` | `/api/category` | Admin / Manager | Tạo mới danh mục sản phẩm (BR-CAT-01, BR-CAT-02) |
| `PUT` | `/api/category/{id}` | Admin / Manager | Cập nhật thông tin danh mục theo ID |
| `DELETE` | `/api/category/{id}` | Admin / Manager | Xóa / Soft Delete danh mục (BR-CAT-03) |
| `POST` | `/api/category/move` | Admin / Manager | Điều chuyển toàn bộ sản phẩm từ danh mục nguồn sang đích (BR-CAT-04) |
| `GET` | `/api/category/{id}` | Public / Authenticated | Lấy chi tiết danh mục theo `CategoryId` |
| `GET` | `/api/category/tree` | Public / Authenticated | Lấy danh sách cấu trúc cây danh mục kèm số lượng sản phẩm |
| `GET` | `/api/category/dropdown` | Public / Authenticated | Lấy danh sách danh mục tối giản cho ô chọn Dropdown (BR-CAT-05) |
| `GET` | `/api/category/search` | Public / Authenticated | Tìm kiếm danh mục theo từ khóa |
| `GET` | `/api/category/{id}/products` | Public / Authenticated | Lấy danh sách toàn bộ sản phẩm thuộc danh mục |

---

### 2.2. Chi tiết API 1: Tạo mới Danh mục (`POST /api/category`)
- **Header**: `Authorization: Bearer <JWT>`
- **Body**: `CreateCategoryRequest` (`categoryName`, `description`)
- **Response**: `201 Created` (`ApiResult<CategoryDto>`)

---

### 2.3. Chi tiết API 2: Cập nhật Danh mục (`PUT /api/category/{id}`)
- **Header**: `Authorization: Bearer <JWT>`
- **Body**: `UpdateCategoryRequest` (`categoryName`, `description`)
- **Response**: `200 OK` (`ApiResult<CategoryDto>`)

---

### 2.4. Chi tiết API 3: Xóa / Soft Delete Danh mục (`DELETE /api/category/{id}`)
- **Header**: `Authorization: Bearer <JWT>`
- **Response**: `200 OK` (`ApiResult<bool>`) hoặc `400 Bad Request` (Nếu danh mục đang chứa sản phẩm).

---

### 2.5. Chi tiết API 4: Điều chuyển Sản phẩm giữa Danh mục (`POST /api/category/move`)
- **Header**: `Authorization: Bearer <JWT>`
- **Body**: `MoveCategoryRequest` (`sourceCategoryId`, `targetCategoryId`)
- **Response**: `200 OK` (`ApiResult<bool>`)

---

### 2.6. Chi tiết API 5: Chi tiết Danh mục theo ID (`GET /api/category/{id}`)
- **Response**: `200 OK` (`ApiResult<CategoryDto>`) hoặc `404 Not Found`.

---

### 2.7. Chi tiết API 6: Cấu trúc Cây Danh mục (`GET /api/category/tree`)
- **Response**: `200 OK` (`ApiResult<IEnumerable<CategoryTreeDto>>`)

---

### 2.8. Chi tiết API 7: Danh sách Danh mục Dropdown (`GET /api/category/dropdown`)
- **Response**: `200 OK` (`ApiResult<IEnumerable<CategoryDropdownDto>>`)

---

### 2.9. Chi tiết API 8: Tìm kiếm Danh mục (`GET /api/category/search`)
- **Query Parameter**: `search` (string)
- **Response**: `200 OK` (`ApiResult<IEnumerable<CategoryDto>>`)

---

### 2.10. Chi tiết API 9: Danh sách Sản phẩm thuộc Danh mục (`GET /api/category/{id}/products`)
- **Response**: `200 OK` (`ApiResult<IEnumerable<ProductDto>>`)

---

## 3. GHI CHÚ
- Toàn bộ kết quả phản hồi đều bọc chuẩn trong `ApiResult<T>`.

---

## 4. KẾT LUẬN
Tài liệu `API.md` cho phân hệ 03_Category quy định đầy đủ 9 REST Endpoints chuẩn mực.
