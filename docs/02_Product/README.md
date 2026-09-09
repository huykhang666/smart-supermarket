# BỘ TÀI LIỆU QUẢN LÝ SẢN PHẨM (02_PRODUCT MODULE SPECIFICATION)

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan Phân hệ Quản lý Sản phẩm (02_Product)](#21-tổng-quan-phân-hệ-quản-lý-sản-phẩm-02_product)
   - 2.2. [Cấu trúc Danh mục Tài liệu Kỹ thuật](#22-cấu-trúc-danh-mục-tài-liệu-kỹ-thuật)
   - 2.3. [Mối liên kết với các Phân hệ khác](#23-mối-liên-kết-với-các-phân-hệ-khác)
   - 2.4. [Quy định Đóng góp & Cập nhật Tài liệu](#24-quy-định-đóng-góp--cập-nhật-tài-liệu)
3. [Ghi chú](#3-ghi-chú)
4. [Kết luận](#4-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **README.md** là trang định hướng và mục lục tổng quan cho toàn bộ phân hệ **02_Product (Quản lý Sản phẩm)** thuộc hệ thống **Smart SuperMarket**. Phân hệ này đóng vai trò trung tâm lưu trữ thông tin hàng hóa, mã vạch (Barcode/QR Code), giá bán niêm yết, giá vốn trung bình, đơn vị tính, danh mục phân loại và nhà cung cấp chính.

Tài liệu này cung cấp sơ đồ điều hướng các tài liệu thành phần trong thư mục `docs/02_Product/`, đảm bảo lập trình viên WinForms Desktop, React Web Client và Backend Web API có cùng nguồn thông tin tham chiếu chuẩn (Single Source of Truth).

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan Phân hệ Quản lý Sản phẩm (02_Product)

Phân hệ Sản phẩm chịu trách nhiệm:
- Quản lý vòng đời sản phẩm từ khi khởi tạo, đang kinh doanh cho đến khi ngừng kinh doanh (Soft Delete).
- Chuẩn hóa mã vạch Barcode/QR Code cho máy quét tại quầy thu ngân WinForms POS (`PosForm`) và máy quét kiểm kho.
- Kiểm soát chính xác giá bán niêm yết (`Price`) và giá vốn trung bình (`CostPrice`) nhằm phục vụ tính toán lợi nhuận kinh doanh.
- Quản lý hệ thống hình ảnh sản phẩm phục vụ hiển thị trên ứng dụng React Web Client và WinForms Admin Dashboard.
- Phân loại sản phẩm theo Danh mục (`Category`) và gán Nhà cung cấp chính (`Supplier`).

---

### 2.2. Cấu trúc Danh mục Tài liệu Kỹ thuật

Thư mục `docs/02_Product/` bao gồm 10 tài liệu kỹ thuật chi tiết:

| STT | Tên tài liệu | Nội dung mô tả chính |
| :---: | :--- | :--- |
| 1 | **[README.md](file:///d:/Laptrinhtrucquan/docs/02_Product/README.md)** | Trang tổng quan điều hướng toàn bộ tài liệu phân hệ Sản phẩm. |
| 2 | **[ProductEntity.md](file:///d:/Laptrinhtrucquan/docs/02_Product/ProductEntity.md)** | Định nghĩa chi tiết cấu trúc bảng `Product` chuẩn 3NF theo `Database_Design_ERD.pdf`. |
| 3 | **[BusinessRules.md](file:///d:/Laptrinhtrucquan/docs/02_Product/BusinessRules.md)** | Tập hợp toàn bộ các quy tắc nghiệp vụ quản lý sản phẩm, giá bán và trạng thái. |
| 4 | **[API.md](file:///d:/Laptrinhtrucquan/docs/02_Product/API.md)** | Hợp đồng giao tiếp REST API (URL, Query, Request/Response Body, HTTP Status). |
| 5 | **[Barcode.md](file:///d:/Laptrinhtrucquan/docs/02_Product/Barcode.md)** | Quy chuẩn mã vạch EAN-13/CODE-128 và cơ chế xử lý quét mã vạch trên WinForms POS. |
| 6 | **[ProductImage.md](file:///d:/Laptrinhtrucquan/docs/02_Product/ProductImage.md)** | Quy định định dạng, lưu trữ và xử lý hình ảnh sản phẩm trên Server Backend. |
| 7 | **[ProductPrice.md](file:///d:/Laptrinhtrucquan/docs/02_Product/ProductPrice.md)** | Phương pháp quản lý giá bán, giá vốn, tỷ lệ lợi nhuận và lịch sử biến động giá. |
| 8 | **[ProductUnit.md](file:///d:/Laptrinhtrucquan/docs/02_Product/ProductUnit.md)** | Chuẩn hóa danh mục đơn vị tính (chai, lon, hộp, kg, lốc, thùng...) và quy tắc hiển thị. |
| 9 | **[SequenceDiagram.md](file:///d:/Laptrinhtrucquan/docs/02_Product/SequenceDiagram.md)** | Sơ đồ tuần hoàn (Sequence Diagram) cho các luồng nghiệp vụ tìm kiếm, quét mã và quản lý. |
| 10 | **[Tasks.md](file:///d:/Laptrinhtrucquan/docs/02_Product/Tasks.md)** | Kế hoạch phân chia Task phát triển từng Phase theo kiến trúc Feature-Folder. |

---

### 2.3. Mối liên kết với các Phân hệ khác

Phân hệ `02_Product` giữ mối liên hệ mật thiết với các phân hệ khác trong hệ thống:
- **`03_Category`**: Liên kết 1-N (1 Danh mục chứa nhiều Sản phẩm) qua `CategoryId`.
- **`04_Supplier`**: Liên kết 1-N (1 Nhà cung cấp là đối tác chính của nhiều Sản phẩm) qua `SupplierId`.
- **`05_Inventory`**: Quản lý số lượng tồn kho theo từng chi nhánh qua bảng trung gian `Inventory` (`ProductId`, `BranchId`).
- **`06_Import`**: Ghi nhận chi tiết sản phẩm và hạn sử dụng FEFO khi nhập kho qua `ImportDetail`.
- **`07_Order`**: Lưu thông tin bán hàng tại quầy POS và đơn online qua `OrderDetail` (`ProductId`, `Quantity`, `UnitPrice`).
- **`08_Promotion`**: Áp dụng chương trình giảm giá riêng biệt cho từng sản phẩm qua `Promotion.ProductId`.
- **`10_AI`**: Phục vụ tính năng gợi ý sản phẩm và dự báo nhập hàng của Google Gemini AI.

---

### 2.4. Quy định Đóng góp & Cập nhật Tài liệu

1. **Tính nhất quán**: Mọi thay đổi về cấu hình field hoặc thuộc tính CSDL trong `ProductEntity.md` phải tuân thủ tuyệt đối sơ đồ `Database_Design_ERD.pdf`.
2. **Không tự ý sửa API Contract**: Các thay đổi về cấu trúc JSON Request/Response trong `API.md` phải được thống nhất giữa Backend Developer và Client Developer (WinForms/React).
3. **Quản lý phiên bản**: Mọi cập nhật tài liệu phải được ghi nhận commit theo chuẩn Conventional Commits (ví dụ: `docs(product): update barcode scanning sequence diagram`).

---

## 3. GHI CHÚ
- Lập trình viên mới tham gia dự án bắt buộc phải đọc kĩ `ProductEntity.md` và `BusinessRules.md` trước khi tiến hành code các class Entity hoặc DTOs.
- Các tài liệu trong thư mục này được viết hoàn toàn bằng định dạng Markdown, sử dụng tiếng Việt chuyên ngành kỹ thuật phần mềm.

---

## 4. KẾT LUẬN

Tài liệu `README.md` của phân hệ **02_Product** đã định hình xong khung cấu trúc và hệ thống tài liệu thành phần. Đây là kim chỉ nam cho công tác phát triển, kiểm thử và vận hành các tính năng liên quan đến sản phẩm trong dự án Smart SuperMarket.
