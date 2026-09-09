# THIẾT KẾ & CHUẨN HÓA ĐƠN VỊ TÍNH SẢN PHẨM (PRODUCT UNIT SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Mục đích & Vai trò của Đơn vị tính (`Unit`)](#21-mục-đích--vai-trò-của-đơn-vị-tính-unit)
   - 2.2. [Danh mục Đơn vị tính Chuẩn hóa (Standard Units List)](#22-danh-mục-đơn-vị-tính-chuẩn-hóa-standard-units-list)
   - 2.3. [Ràng buộc & Validation Dữ liệu Trường `Unit`](#23-ràng-buộc--validation-dữ-liệu-trường-unit)
   - 2.4. [Hiển thị Đơn vị tính trên Giao diện & Hóa đơn PDF](#24-hiển-thị-đơn-vị-tính-trên-giao-diện--hóa-đơn-pdf)
   - 2.5. [Liên kết Đơn vị tính với Quản lý Kho & Bán hàng POS](#25-liên-kết-đơn-vị-tính-với-quản-lý-kho--bán-hàng-pos)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **ProductUnit.md** quy định các tiêu chuẩn kỹ thuật và quy tắc nghiệp vụ về Đơn vị tính (`Unit`) cho sản phẩm thuộc hệ thống **Smart SuperMarket**. Đơn vị tính là thuộc tính định lượng bắt buộc giúp định danh chính xác quy cách đóng gói sản phẩm khi hiển thị giá niêm yết, bán hàng tại quầy POS, lập phiếu nhập kho và in hóa đơn thanh toán cho khách hàng.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Mục đích & Vai trò của Đơn vị tính (`Unit`)

- **Bắt buộc trong CSDL**: Trường `Unit` thuộc kiểu `NVARCHAR(20)`, ràng buộc `NOT NULL` trong bảng `Product` (`Database_Design_ERD.pdf`).
- **Rõ ràng minh bạch giá**: Giúp khách hàng hiểu rõ giá niêm yết áp dụng cho 1 đơn vị cụ thể nào (VD: *10.000 VNĐ / lon* vs *60.000 VNĐ / lốc*).
- **Đồng bộ toàn hệ thống**: Được sử dụng thống nhất trên WinForms POS, React Web Client, Báo cáo tồn kho và Hóa đơn PDF (`QuestPDF`).

---

### 2.2. Danh mục Đơn vị tính Chuẩn hóa (Standard Units List)

Hệ thống khuyến nghị và chuẩn hóa các đơn vị tính phổ biến trong ngành bán lẻ siêu thị:

| Nhóm ngành hàng | Danh mục Đơn vị tính Chuẩn (`Unit`) | Ví dụ sản phẩm |
| :--- | :--- | :--- |
| **Nước giải khát, Bia, Rượu** | `chai`, `lon`, `lốc`, `thùng` | Nước ngọt Coca-Cola (lon), Bia Heineken (thùng) |
| **Sữa & Sản phẩm từ sữa** | `hộp`, `bịch`, `vỉ`, `hũ` | Sữa tươi Vinamilk 1L (hộp), Sữa chua Vinamilk (hũ) |
| **Bánh kẹo, Thực phẩm khô** | `gói`, `hộp`, `túi`, `hũ`, `bar` | Bánh OREO (hộp), Mì Hảo Hảo (gói) |
| **Thực phẩm tươi sống, Rau củ** | `kg`, `gram`, `khay`, `túi`, `bó` | Thịt heo băm (khay), Rau cải ngọt (bó), Táo Mỹ (kg) |
| **Hóa mỹ phẩm, Gia dụng** | `chai`, `tuýp`, `bình`, `cuộn`, `bộ` | Dầu gội Sunsilk (chai), Kem đánh răng (tuýp) |

---

### 2.3. Ràng buộc & Validation Dữ liệu Trường `Unit`

1. **Ràng buộc độ dài & Ký tự**:
   - Độ dài tối đa: **20 ký tự**.
   - Không được để trống (`String.IsNullOrWhiteSpace`).
   - Tự động cắt khoảng trắng thừa ở 2 đầu (`Trim()`) và chuyển về chữ thường tiếng Việt chuẩn.
2. **Validation DTO trong C# Backend**:
   - Attribute: `[Required(ErrorMessage = "Đơn vị tính không được để trống.")]`
   - Attribute: `[StringLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]`

---

### 2.4. Hiển thị Đơn vị tính trên Giao diện & Hóa đơn PDF

1. **Trên màn hình bán hàng WinForms POS (`PosForm`)**:
   - Cột Đơn vị tính được hiển thị rõ ràng trên DataGridView giỏ hàng cạnh cột Số lượng (`Quantity`).
2. **Trên React Web Client**:
   - Hiển thị nhãn giá theo chuẩn: `{price.toLocaleString('vi-VN')} VNĐ / {unit}` (VD: `36.000 VNĐ / hộp`).
3. **Trên Hóa đơn PDF in cho Khách hàng (`QuestPDF`)**:
   - Bảng chi tiết hóa đơn gồm các cột: `Tên SP` | `ĐVT` | `SL` | `Đơn giá` | `Thành tiền`.
   - *Ví dụ*: `Coca-Cola 330ml | lon | 3 | 10.000 | 30.000`.

---

### 2.5. Liên kết Đơn vị tính với Quản lý Kho & Bán hàng POS

- Số lượng tồn kho trong bảng `Inventory.QuantityInStock` được tính toán trực tiếp theo `Unit` của sản phẩm đó.
- *Ví dụ*: Nếu sản phẩm có `Unit = "lon"` và `QuantityInStock = 120`, có nghĩa là chi nhánh đang còn tồn kho 120 lon.

---

## 3. GHI CHÚ
- Khi khởi tạo sản phẩm mới trên Form WinForms Admin, cung cấp `ComboBox` có sẵn danh sách các Đơn vị tính chuẩn hóa để thu ngân/quản lý chọn nhanh, đồng thời cho phép nhập tự do nếu có đơn vị tính mới.

---

## 4. KẾT LUẬN

Tài liệu `ProductUnit.md` đã chuẩn hóa toàn bộ các quy định liên quan đến Đơn vị tính sản phẩm. Sự chuẩn hóa này đảm bảo tính chính xác cho các hoạt động quản lý kho, bán hàng và in hóa đơn thanh toán trên toàn hệ thống Smart SuperMarket.
