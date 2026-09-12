# PHÂN HỆ 05: QUẢN LÝ TỒN KHO (INVENTORY MANAGEMENT)

> Mức độ ưu tiên: 🔴 Bắt buộc | Phụ thuộc: 02_Product, 03_Category, 04_Supplier, 01_Authentication

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Mục tiêu phân hệ 05_Inventory](#21-mục-tiêu-phân-hệ-05_inventory)
   - 2.2. [Danh mục tài liệu trong phân hệ](#22-danh-mục-tài-liệu-trong-phân-hệ)
   - 2.3. [Phạm vi chức năng](#23-phạm-vi-chức-năng)
   - 2.4. [Ma trận phân quyền Tồn kho](#24-ma-trận-phân-quyền-tồn-kho)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Phân hệ **05_Inventory** chịu trách nhiệm toàn bộ nghiệp vụ quản lý tồn kho trong hệ thống **Smart SuperMarket**, bao gồm theo dõi số lượng hàng hóa hiện có tại từng chi nhánh, ghi nhận mọi biến động tồn kho (nhập hàng, bán hàng, hủy hàng, điều chỉnh), cảnh báo khi tồn kho xuống dưới ngưỡng an toàn và quản lý hạn sử dụng (HSD) theo chuẩn **FEFO (First Expired, First Out)**.

Tài liệu này đóng vai trò là hướng dẫn tổng quan cho phân hệ 05, dựa trên thiết kế chuẩn hóa CSDL tại file `Database_Design_ERD.pdf` và các quy định kiến trúc cốt lõi tại `docs/00_Project/`.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Mục tiêu phân hệ 05_Inventory

- **Theo dõi tồn kho real-time**: Mọi giao dịch nhập/bán hàng đều phản ánh ngay lập tức vào số liệu tồn kho, không có độ trễ.
- **Chuẩn FEFO**: Khi xuất kho, hệ thống tự động ưu tiên lô hàng có hạn sử dụng sớm nhất, giảm thiểu tổn thất do hàng hóa hết hạn.
- **Cảnh báo chủ động**: Tự động phát hiện và thông báo khi sản phẩm sắp hết hàng hoặc sắp hết hạn sử dụng.
- **Lịch sử minh bạch**: Mọi biến động tồn kho đều được ghi nhận vào `StockHistory` — ai làm, lúc nào, vì sao.
- **Hỗ trợ AI**: Cung cấp dữ liệu tồn kho và HSD để module AI (10_AI) phân tích, đề xuất nhập hàng và khuyến mãi.
- **Đa chi nhánh**: Tồn kho được quản lý độc lập theo từng chi nhánh (`BranchId`), hỗ trợ mô hình chuỗi siêu thị.

---

### 2.2. Danh mục tài liệu trong phân hệ

Phân hệ `05_Inventory` bao gồm 8 tài liệu kỹ thuật:

| STT | Tài liệu | File | Nội dung chính |
| :---: | :--- | :--- | :--- |
| 1 | **Trang chỉ mục phân hệ** | `README.md` | Tổng quan, mục tiêu, phạm vi, ma trận phân quyền |
| 2 | **Cấu trúc CSDL Tồn kho** | `InventoryEntity.md` | Chi tiết bảng `Inventory`, `StockHistory`, `DiscountRule` theo ERD |
| 3 | **Quy tắc Nghiệp vụ Tồn kho** | `BusinessRules.md` | Tập hợp BR-INV-01 → BR-INV-XX: cập nhật, cảnh báo, FEFO, giảm giá HSD |
| 4 | **Danh sách REST API Tồn kho** | `API.md` | Endpoints: xem tồn kho, low-stock, expiring, điều chỉnh, lịch sử |
| 5 | **Thuật toán FEFO** | `FEFO.md` | Định nghĩa, lý do áp dụng, cách implement, ví dụ minh họa |
| 6 | **Sơ đồ Tuần tự Tồn kho** | `SequenceDiagram.md` | Mermaid sequence: nhập kho, bán hàng FEFO, cảnh báo tự động |
| 7 | **Luồng Biến động Tồn kho** | `StockFlow.md` | Tất cả trường hợp tăng/giảm tồn kho, bảng ChangeType, flowchart |
| 8 | **Danh sách Task Triển khai** | `Tasks.md` | Phân công Backend Dev và WinForms Dev theo thứ tự ưu tiên |

---

### 2.3. Phạm vi chức năng

#### Chức năng bắt buộc (MVP - v1)
- Xem danh sách tồn kho theo chi nhánh, với chỉ báo màu sắc (Xanh / Vàng / Đỏ) theo mức tồn kho.
- Tự động cập nhật tồn kho khi phiếu nhập được xác nhận (06_Import).
- Tự động trừ tồn kho khi đơn hàng hoàn tất (07_Order).
- Cảnh báo sản phẩm tồn kho thấp (`QuantityOnHand <= MinStockLevel`).
- Cảnh báo sản phẩm sắp hết hạn sử dụng (≤ 15 ngày).
- Ghi lịch sử biến động tồn kho (`StockHistory`) cho mọi thay đổi.
- Điều chỉnh tồn kho thủ công bởi Admin/Manager (kiểm kê).

#### Chức năng nâng cao (v2 - nếu còn thời gian)
- Dashboard tồn kho tổng hợp toàn chuỗi chi nhánh.
- AI đề xuất chuyển hàng giữa chi nhánh khi phân phối không đều.
- Báo cáo tồn kho xuất PDF/Excel.

---

### 2.4. Ma trận phân quyền Tồn kho

| Chức năng | Admin (1) | Manager (2) | Staff (3) | Customer (4) |
| :--- | :---: | :---: | :---: | :---: |
| Xem danh sách tồn kho | ✅ | ✅ | ✅ | ❌ |
| Xem chi tiết tồn kho sản phẩm | ✅ | ✅ | ✅ | ❌ |
| Xem danh sách sắp hết hàng | ✅ | ✅ | ✅ | ❌ |
| Xem danh sách sắp hết hạn | ✅ | ✅ | ✅ | ❌ |
| Xem lịch sử biến động tồn kho | ✅ | ✅ | ❌ | ❌ |
| Điều chỉnh tồn kho thủ công | ✅ | ✅ | ❌ | ❌ |
| Cấu hình ngưỡng MinStockLevel | ✅ | ✅ | ❌ | ❌ |
| Cấu hình DiscountRule HSD | ✅ | ❌ | ❌ | ❌ |

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | `MinStockLevel` nên là giá trị mặc định cố định cho toàn hệ thống hay cấu hình riêng theo từng sản phẩm? | Ảnh hưởng schema bảng `Inventory` và logic cảnh báo. | Đề xuất: Giữ `MinStockLevel` ở cấp bảng `Inventory` (theo sản phẩm × chi nhánh), mặc định = 10. Admin có thể chỉnh từng sản phẩm. |
| 2 | Khi tồn kho âm (do lỗi hệ thống), hệ thống xử lý thế nào? | Ảnh hưởng ràng buộc `CHECK(QuantityOnHand >= 0)`. | Đề xuất: Không cho phép tồn kho âm — ném exception tại tầng Service trước khi ghi DB. |

---

## 4. GHI CHÚ
- Tồn kho được quản lý theo cặp `(ProductId, BranchId)` — mỗi sản phẩm tại mỗi chi nhánh có đúng 1 bản ghi `Inventory`.
- Mọi thao tác cập nhật tồn kho (nhập hàng, bán hàng, điều chỉnh) phải thực hiện trong **Transaction** để đảm bảo tính nhất quán dữ liệu.
- Tham chiếu thiết kế CSDL: file `Database_Design_ERD.pdf` (Version 2.0).

---

## 5. KẾT LUẬN

Tài liệu `README.md` định hình toàn bộ phạm vi công việc cho phân hệ `05_Inventory`. Đây là module nền tảng kết nối trực tiếp với nghiệp vụ nhập hàng (06_Import) và bán hàng (07_Order), đồng thời cung cấp dữ liệu đầu vào quan trọng cho module AI (10_AI) phân tích và ra quyết định kinh doanh.
