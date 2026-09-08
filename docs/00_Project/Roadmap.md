# LỘ TRÌNH PHÁT TRIỂN & PHÂN CÔNG TASK (ROADMAP) SMART SUPERMARKET

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Lộ trình 13 Giai đoạn Phát triển (13-Stage Roadmap)](#21-lộ-trình-13-giai-đoạn-phát-triển-13-stage-roadmap)
   - 2.2. [Bảng Phân công Task Chi tiết cho Nhóm 5 Người](#22-bảng-phân-công-task-chi-tiết-cho-nhóm-5-người)
   - 2.3. [Kế hoạch Thực hiện theo Tuần (Milestone Schedule)](#23-kế-hoạch-thực-hiện-theo-tuần-milestone-schedule)
   - 2.4. [Tiêu chí Nghiệm thu Hoàn thành Giai đoạn (Acceptance Criteria)](#24-tiêu-chí-nghiệm-thu-hoàn-thành-giai-đoạn-acceptance-criteria)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **Roadmap.md** xác lập lộ trình phát triển tổng thể và bảng phân công công việc chi tiết cho dự án **Smart SuperMarket**. Lộ trình được chi tiết hóa qua **13 giai đoạn tương ứng với 13 thư mục tài liệu** (từ `00_Project` đến `12_Report`), đảm bảo sự phối hợp nhịp nhàng giữa 5 thành viên trong nhóm và đúng tiến độ báo cáo môn học Lập trình Windows.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Lộ trình 13 Giai đoạn Phát triển (13-Stage Roadmap)

Lộ trình phát triển hệ thống được thực hiện tuần tự qua 13 mốc chính:

```text
00_Project ──► 01_Auth ──► 02_Category ──► 03_Supplier ──► 04_Product ──► 05_Inventory
                                                                              │
12_Report ◄── 11_Deploy ◄── 10_AI ◄── 09_Customer ◄── 08_Promotion ◄── 06_Import ◄── 07_Order
```

| Mốc | Thư mục Module | Nội dung trọng tâm phát triển |
| :---: | :--- | :--- |
| **00** | `00_Project/` | Biên soạn bộ tài liệu kiến trúc, quy chuẩn code, tech stack và quy hoạch Monorepo. |
| **01** | `01_Authentication/` | Xây dựng CSDL `User`, API Đăng nhập/JWT/BCrypt và Form WinForms `LoginForm`. |
| **02** | `02_Category/` | Xây dựng CSDL `Category`, API CRUD và Form WinForms Quản lý Danh mục. |
| **03** | `03_Supplier/` | Xây dựng CSDL `Supplier`, API CRUD và Form WinForms Quản lý Nhà cung cấp. |
| **04** | `04_Product/` | Xây dựng CSDL `Product`, sinh Barcode EAN-13, API Tra cứu Barcode & Form `ProductForm`. |
| **05** | `05_Inventory/` | Xây dựng CSDL `Inventory`, API Tồn kho chi nhánh & Cảnh báo tồn thấp. |
| **06** | `06_Import/` | CSDL `ImportReceipt`/`ImportDetail`, API Nhập kho quản lý HSD FEFO & Form `ImportForm`. |
| **07** | `07_Order/` | **Trọng tâm POS**: CSDL `Order`/`OrderDetail`, Form POS Quét Barcode & In hóa đơn PDF. |
| **08** | `08_Promotion/` | CSDL `Promotion`/`OrderPromotion`, API Khuyến mãi & Quy tắc giảm giá HSD. |
| **09** | `09_Customer/` | CSDL `Voucher`/`PointHistory`, Web Customer React TS Tích điểm & Đổi Voucher. |
| **10** | `10_AI/` | Tích hợp Google Gemini API: AI Dự báo nhập hàng, Gợi ý HSD, Sinh báo cáo tự động. |
| **11** | `11_Deployment/` | Đóng gói WinForms Self-Contained (.exe), Docker Compose PostgreSQL, Nginx. |
| **12** | `12_Report/` | Hoàn thiện Báo cáo đồ án Word/PDF, Slide thuyết trình và Video Demo tổng duyệt. |

---

### 2.2. Bảng Phân công Task Chi tiết cho Nhóm 5 Người

Hệ thống phân chia công việc độc lập, không dẫm chân nhau giữa 5 thành viên:

| Thành viên | Vai trò đảm nhận | Nhiệm vụ chi tiết được giao |
| :--- | :--- | :--- |
| **Thành viên 1** | **Team Leader / Core Backend Lead** | - Dựng `AppDbContext` EF Core & Migrations SQL Server/Postgres.<br>- Viết Module Authentication & JWT Token Generator (`Features/Auth`).<br>- Viết `ApiResult<T>`, Global Exception Middleware.<br>- Quản lý Repository Git, code review & merge code. |
| **Thành viên 2** | **Backend & AI Developer** | - Viết API CRUD cho Product, Category, Supplier, Inventory.<br>- Tích hợp **Google Gemini API** (`Infrastructure/External/AI/`).<br>- Viết API AI Dự báo nhập hàng, AI Gợi ý giảm giá HSD, AI Sinh báo cáo.<br>- Viết API Dashboard Thống kê cho Admin. |
| **Thành viên 3** | **WinForms Dev 1 (POS & Barcode Lead)** | - Thiết kế Form `LoginForm` và Màn hình chính Bán hàng `PosForm`.<br>- Tích hợp **Máy quét mã vạch USB / Camera Webcam (`ZXing.Net`)**.<br>- Xử lý giỏ hàng POS, tính tổng tiền, tích điểm, áp mã Voucher.<br>- In & Xuất hóa đơn bán hàng PDF (`QuestPDF`). |
| **Thành viên 4** | **WinForms Dev 2 (Admin & Inventory Lead)** | - Thiết kế Form Quản lý Sản phẩm, Nhập kho (`ImportForm`), Chuyển kho.<br>- Dựng Dashboard Biểu đồ Thống kê doanh thu (`LiveCharts2`).<br>- Làm màn hình Cảnh báo tồn kho thấp (hiển thị màu đỏ) và Cảnh báo HSD.<br>- Màn hình xem bài Báo cáo AI sinh ra. |
| **Thành viên 5** | **Web Customer & DevOps / Docs Lead** | - Dựng **Web Khách hàng (React + TypeScript + Vite)** tra cứu SP, tích điểm.<br>- Viết `Dockerfile` và `docker-compose.yml` cho dự án.<br>- Tổng hợp Slide thuyết trình, vẽ sơ đồ Use Case & ERD.<br>- Biên soạn quyển Báo cáo Đồ án môn học Word/PDF. |

---

### 2.3. Kế hoạch Thực hiện theo Tuần (Milestone Schedule)

- **Tuần 1: Khởi tạo Kiến trúc & Master Data**:
  - TV1: Dựng CSDL 20 bảng & API Auth JWT.
  - TV2: Viết API Product, Category, Supplier.
  - TV3 & TV4: Thiết kế khung Form WinForms POS & Admin.
  - TV5: Dựng khung Web React TS & Viết tài liệu `00_Project/`.
- **Tuần 2: Nghiệp vụ Bán hàng POS & Kho hàng**:
  - TV1 & TV3: Ghép API Đơn hàng với WinForms POS + Tích hợp máy quét mã vạch.
  - TV2: Viết API Nhập kho (HSD) & Tồn kho.
  - TV4: Dựng Form Nhập kho & Biểu đồ `LiveCharts2`.
  - TV5: Dựng trang web Khách hàng xem sản phẩm.
- **Tuần 3: Tích hợp AI, Thanh toán & Tích điểm**:
  - TV2: Tích hợp Google Gemini API (Dự báo nhập, Sinh báo cáo).
  - TV3: Hoàn thiện In hóa đơn PDF (`QuestPDF`).
  - TV4: Màn hình hiển thị Báo cáo AI.
  - TV5: Tích hợp Đổi Voucher trên Web + Đóng gói Docker.
- **Tuần 4: Tổng duyệt, Test & Nộp Báo cáo**:
  - Cả nhóm: Chạy thử nghiệm End-to-End, fix bug.
  - TV5: Hoàn thiện Báo cáo đồ án Word/PDF + Slide thuyết trình.

---

### 2.4. Tiêu chí Nghiệm thu Hoàn thành Giai đoạn (Acceptance Criteria)

Một giai đoạn được coi là hoàn thành khi đáp ứng 3 điều kiện:
1. **Mã nguồn**: Biên dịch `dotnet build` và `npm run build` thành công 0 lỗi.
2. **Tài liệu**: Có file tài liệu tương ứng nằm trong thư mục `docs/XX_Module/`.
3. **Kiểm thử**: Các tính năng chạy mượt mà không làm đơ đơ (freeze) giao diện WinForms.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Việc họp nhóm trao đổi tiến độ sẽ thực hiện hàng ngày (Daily Standup) hay họp cố định 2 lần/tuần? | Ảnh hưởng việc theo dõi tiến độ công việc. | Đề xuất: Họp nhanh 15 phút qua Discord/Google Meet vào 21h00 các ngày Thứ 3, Thứ 5 và Chủ Nhật. |

---

## 4. GHI CHÚ
- Khi có sự thay đổi người phụ trách Task, Leader phải cập nhật lại tài liệu `Roadmap.md` và thông báo công khai cho cả nhóm.

---

## 5. KẾT LUẬN

Tài liệu `Roadmap.md` đã quy hoạch lộ trình 13 giai đoạn và bảng phân công công việc minh bạch cho nhóm 5 người. Sự phân chia rõ ràng trách nhiệm giúp cả nhóm chủ động thực hiện công việc, đảm bảo đúng tiến độ và đưa đồ án Smart SuperMarket đạt kết quả cao nhất.
