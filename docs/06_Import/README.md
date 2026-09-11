# PHÂN HỆ 06: QUẢN LÝ NHẬP HÀNG (IMPORT MANAGEMENT)

> Mức độ ưu tiên: 🔴 Bắt buộc | Phụ thuộc: 02_Product, 04_Supplier, 05_Inventory, 01_Authentication

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Mục tiêu phân hệ 06_Import](#21-mục-tiêu-phân-hệ-06_import)
   - 2.2. [Danh mục tài liệu trong phân hệ](#22-danh-mục-tài-liệu-trong-phân-hệ)
   - 2.3. [Phạm vi chức năng](#23-phạm-vi-chức-năng)
   - 2.4. [Ma trận phân quyền Nhập hàng](#24-ma-trận-phân-quyền-nhập-hàng)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Phân hệ **06_Import** chịu trách nhiệm toàn bộ quy trình nhập hàng hóa vào kho siêu thị trong hệ thống **Smart SuperMarket**, bao gồm tạo phiếu nhập hàng từ nhà cung cấp, nhập chi tiết sản phẩm (số lượng, giá vốn, hạn sử dụng) và xác nhận phiếu để tự động cập nhật tồn kho.

Phân hệ này là đầu vào của toàn bộ chuỗi cung ứng: nhà cung cấp → phiếu nhập → kho hàng → bán lẻ.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Mục tiêu phân hệ 06_Import

- **Quản lý đầu vào chuỗi cung ứng**: Ghi nhận chính xác từng lô hàng nhập vào, bao gồm nhà cung cấp, ngày nhập, giá vốn và hạn sử dụng.
- **Kiểm soát trạng thái phiếu nhập**: Phiếu nhập trải qua vòng đời `Draft → Confirmed / Cancelled` — không cho phép sửa sau khi đã xác nhận.
- **Tự động cập nhật tồn kho**: Khi phiếu nhập được xác nhận, hệ thống tự động cộng tồn kho và ghi `StockHistory` trong cùng một Transaction.
- **Hỗ trợ FEFO**: Lưu trữ `ExpiryDate` của từng lô hàng trong `ImportDetail` — đây là nguồn dữ liệu HSD duy nhất để hệ thống thực hiện FEFO khi bán hàng.
- **Kiểm soát giá vốn**: Ghi nhận giá vốn (`CostPrice`) từng lô nhập để tính toán lợi nhuận chính xác.

---

### 2.2. Danh mục tài liệu trong phân hệ

Phân hệ `06_Import` bao gồm 7 tài liệu kỹ thuật:

| STT | Tài liệu | File | Nội dung chính |
| :---: | :--- | :--- | :--- |
| 1 | **Trang chỉ mục phân hệ** | `README.md` | Tổng quan, mục tiêu, phạm vi, ma trận phân quyền |
| 2 | **Cấu trúc CSDL Phiếu nhập** | `ImportReceipt.md` | Chi tiết bảng `ImportReceipt`, vòng đời trạng thái, mã phiếu |
| 3 | **Cấu trúc CSDL Chi tiết nhập** | `ImportDetail.md` | Chi tiết bảng `ImportDetail`, tính SubTotal, lưu trữ HSD |
| 4 | **Quy tắc Nghiệp vụ Nhập hàng** | `BusinessRules.md` | BR-IMP-01 → BR-IMP-XX: vòng đời phiếu, validate HSD, phân quyền |
| 5 | **Danh sách REST API Nhập hàng** | `API.md` | Endpoints: CRUD phiếu nhập, confirm, cancel, thêm chi tiết |
| 6 | **Sơ đồ Tuần tự Nhập hàng** | `SequenceDiagram.md` | Mermaid sequence: tạo Draft, Confirm → cập nhật kho, hủy phiếu |
| 7 | **Danh sách Task Triển khai** | `Tasks.md` | Phân công Backend Dev và WinForms Dev theo thứ tự ưu tiên |

---

### 2.3. Phạm vi chức năng

#### Chức năng bắt buộc (MVP - v1)
- Tạo phiếu nhập hàng mới ở trạng thái `Draft` (chọn nhà cung cấp, chi nhánh).
- Thêm sản phẩm vào phiếu nhập (số lượng, giá vốn, hạn sử dụng).
- Xem danh sách phiếu nhập với filter theo ngày, nhà cung cấp, trạng thái.
- Xem chi tiết phiếu nhập kèm danh sách sản phẩm.
- Xác nhận phiếu nhập (`Confirmed`) → Tự động cập nhật tồn kho và ghi StockHistory.
- Hủy phiếu nhập `Draft` chưa xác nhận.

#### Ràng buộc quan trọng
- **Phiếu đã `Confirmed` KHÔNG được sửa hoặc hủy** — đây là dữ liệu kế toán bất biến.
- **HSD bắt buộc** với sản phẩm thực phẩm — validate HSD > ngày nhập + 7 ngày.
- **Chỉ Admin và Manager** được tạo và xác nhận phiếu nhập.

---

### 2.4. Ma trận phân quyền Nhập hàng

| Chức năng | Admin (1) | Manager (2) | Staff (3) | Customer (4) |
| :--- | :---: | :---: | :---: | :---: |
| Xem danh sách phiếu nhập | ✅ | ✅ | ❌ | ❌ |
| Xem chi tiết phiếu nhập | ✅ | ✅ | ❌ | ❌ |
| Tạo phiếu nhập mới (Draft) | ✅ | ✅ | ❌ | ❌ |
| Thêm sản phẩm vào phiếu | ✅ | ✅ | ❌ | ❌ |
| Xác nhận phiếu (Confirm) | ✅ | ✅ | ❌ | ❌ |
| Hủy phiếu Draft (Cancel) | ✅ | ✅ | ❌ | ❌ |
| Xem lịch sử nhập hàng | ✅ | ✅ | ❌ | ❌ |

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Có cần tính năng "Sửa phiếu Draft" trước khi Confirm không? | Ảnh hưởng API và UX WinForms. | Đề xuất: Có — cho phép thêm/xóa dòng `ImportDetail` khi phiếu còn `Draft`. Sau khi `Confirmed` thì lock hoàn toàn. |
| 2 | Sản phẩm không có HSD (đồ gia dụng) có bắt buộc nhập `ExpiryDate` trong `ImportDetail` không? | Ảnh hưởng validation. | Đề xuất: `ExpiryDate` là `NULL` cho sản phẩm không có HSD — Backend kiểm tra theo `ProductCategory` để quyết định có bắt buộc không. |

---

## 4. GHI CHÚ
- Phân hệ 06_Import là **upstream** trực tiếp của 05_Inventory — phiếu nhập chỉ có ý nghĩa sau khi được Confirm và tồn kho được cập nhật.
- Mã phiếu nhập tự sinh theo định dạng `IMP-YYYYMMDD-XXXX` (ví dụ: `IMP-20260911-0001`) — đảm bảo duy nhất và dễ tra cứu.
- Tham chiếu thiết kế CSDL: file `Database_Design_ERD.pdf` (Version 2.0).

---

## 5. KẾT LUẬN

Tài liệu `README.md` định hình toàn bộ phạm vi công việc cho phân hệ `06_Import`. Đây là module quản lý nguồn cung hàng hóa của chuỗi siêu thị, kết nối trực tiếp với nhà cung cấp (04_Supplier) và tự động kích hoạt cập nhật tồn kho (05_Inventory) mỗi khi phiếu nhập được xác nhận.
