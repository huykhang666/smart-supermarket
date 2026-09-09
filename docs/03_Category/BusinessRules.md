# QUY TẮC NGHIỆP VỤ QUẢN LÝ DANH MỤC (CATEGORY BUSINESS RULES SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Danh mục Quy tắc Nghiệp vụ](#21-tổng-quan-danh-mục-quy-tắc-nghiệp-vụ)
   - 2.2. [BR-CAT-01: Tên Danh mục Duy nhất (Category Name Uniqueness)](#22-br-cat-01-tên-danh-mục-duy-nhất-category-name-uniqueness)
   - 2.3. [BR-CAT-02: Giới hạn Mô tả (Description Constraint)](#23-br-cat-02-giới-hạn-mô-tả-description-constraint)
   - 2.4. [BR-CAT-03: Ràng buộc An toàn khi Xóa (Soft Delete & Dependency Constraint)](#24-br-cat-03-ràng-buộc-an-toàn-khi-xóa-soft-delete--dependency-constraint)
   - 2.5. [BR-CAT-04: Quy tắc Chuyển Sản phẩm giữa các Danh mục (Move Category Rule)](#25-br-cat-04-quy-tắc-chuyển-sản-phẩm-giữa-các-danh-mục-move-category-rule)
   - 2.6. [BR-CAT-05: Sắp xếp & Chuẩn hóa Tree/Dropdown (Tree & Dropdown Formatting)](#26-br-cat-05-sắp-xếp--chuẩn-hóa-treedropdown-tree--dropdown-formatting)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **BusinessRules.md** định nghĩa tập hợp toàn bộ các Quy tắc Nghiệp vụ (Business Rules) áp dụng cho phân hệ **Quản lý Danh mục (03_Category)** trong hệ thống **Smart SuperMarket**. Các quy tắc này bắt buộc phải được thực thi tại tầng `CategoryService.cs`.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Danh mục Quy tắc Nghiệp vụ

| Mã quy tắc | Tên quy tắc nghiệp vụ | Phạm vi áp dụng | Mức độ nghiêm ngặt |
| :---: | :--- | :--- | :---: |
| **BR-CAT-01** | Tên Danh mục Duy nhất (Category Name Uniqueness) | Backend Service / CSDL | **Bắt buộc (Critical)** |
| **BR-CAT-02** | Giới hạn Độ dài Mô tả (Description Constraint) | Backend Service / DTO | **Bắt buộc (High)** |
| **BR-CAT-03** | Ràng buộc An toàn khi Xóa (Soft Delete Constraint) | Backend Service / DB FK | **Bắt buộc (Critical)** |
| **BR-CAT-04** | Quy tắc Chuyển Sản phẩm Danh mục (Move Category) | Backend Service | **Bắt buộc (High)** |
| **BR-CAT-05** | Chuẩn hóa Tree Query & Dropdown Selector | Backend Service / DTO | **Khuyên dùng (Medium)** |

---

### 2.2. BR-CAT-01: Tên Danh mục Duy nhất (Category Name Uniqueness)
- Tên danh mục (`CategoryName`) không được để trống (Not Null / Not Empty).
- Độ dài tối đa **100 ký tự**.
- Tên danh mục là **duy nhất trên toàn hệ thống** (Không phân biệt chữ hoa/chữ thường và tự động cắt bỏ khoảng trắng thừa ở hai đầu).

---

### 2.3. BR-CAT-02: Giới hạn Mô tả (Description Constraint)
- Mô tả danh mục (`Description`) là tùy chọn (cho phép Null).
- Độ dài tối đa **255 ký tự**.

---

### 2.4. BR-CAT-03: Ràng buộc An toàn khi Xóa (Soft Delete & Dependency Constraint)
- Nếu danh mục đang chứa sản phẩm (`ProductCount > 0`), hệ thống **ngăn chặn hành động xóa** và trả về thông báo lỗi rõ ràng.
- Để xóa danh mục đang có sản phẩm, người dùng phải thực hiện chuyển toàn bộ sản phẩm sang danh mục khác (`MoveCategoryProductsAsync`) trước.

---

### 2.5. BR-CAT-04: Quy tắc Chuyển Sản phẩm giữa các Danh mục (Move Category Rule)
- Cho phép chuyển tất cả sản phẩm từ Danh mục nguồn (`SourceCategoryId`) sang Danh mục đích (`TargetCategoryId`).
- `SourceCategoryId` và `TargetCategoryId` phải tồn tại trong CSDL và **phải khác nhau**.

---

### 2.6. BR-CAT-05: Sắp xếp & Chuẩn hóa Tree/Dropdown (Tree & Dropdown Formatting)
- Danh sách Tree Query và Dropdown luôn được sắp xếp theo bảng chữ cái A-Z của `CategoryName`.
- API Dropdown chỉ trả về các trường tối giản (`CategoryId`, `CategoryName`) để tối ưu dung lượng truyền tải.

---

## 3. GHI CHÚ
- Toàn bộ các business rule trên phải được kiểm soát tại `CategoryService.cs` trước khi gọi Repository.

---

## 4. KẾT LUẬN
Tài liệu `BusinessRules.md` thiết lập chuẩn mực nghiệp vụ cho phân hệ Danh mục, đảm bảo tính toàn vẹn dữ liệu và an toàn khi vận hành hệ thống Smart SuperMarket.
