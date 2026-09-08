# TÀI LIỆU DỰ ÁN SMART SUPERMARKET - THƯ MỤC 00_PROJECT

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Tổng quan thư mục 00_Project](#21-tổng-quan-thư-mục-00_project)
   - 2.2. [Danh mục tài liệu kỹ thuật cốt lõi](#22-danh-mục-tài-liệu-kỹ-thuật-cốt-lõi)
   - 2.3. [Hướng dẫn sử dụng tài liệu theo vai trò](#23-hướng-dẫn-sử-dụng-tài-liệu-theo-vai-trò)
   - 2.4. [Quy định bảo trì và cập nhật tài liệu](#24-quy-định-bảo-trì-và-cập-nhật-tài-liệu)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Thư mục `docs/00_Project/` đóng vai trò là **Nguồn sự thật duy nhất (Single Source of Truth - SSOT)** cho toàn bộ dự án **Smart SuperMarket**. 

Tài liệu tại đây thiết lập nền tảng về tầm nhìn sản phẩm, kiến trúc hệ thống, danh mục công nghệ, cấu trúc thư mục, quy chuẩn lập trình và lộ trình phát triển. Tất cả các phân hệ tiếp theo (từ `01_Authentication` đến `12_Report`) và mọi thành viên trong dự án (Software Architect, Backend Developer, WinForms Desktop Developer, Web Frontend Developer, Tester, DevOps Engineer) đều phải tuân thủ nghiêm ngặt các nguyên tắc và định hướng được quy định trong thư mục này.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Tổng quan thư mục 00_Project
Thư mục `00_Project/` lưu trữ các tài liệu cấp cao (High-Level Documentation). Các tài liệu này được thiết kế để đảm bảo:
- **Tính nhất quán**: Mọi quyết định kỹ thuật và thiết kế giao diện đều đồng bộ giữa 3 phân hệ: Backend API, Desktop App (WinForms) và Web App (React TS).
- **Tính kế thừa**: Một lập trình viên mới khi gia nhập dự án có thể đọc bộ tài liệu này để hiểu toàn bộ bức tranh tổng thể và bắt tay vào phát triển mà không làm phá vỡ kiến trúc sẵn có.
- **Tính minh bạch**: Mọi yêu cầu chưa rõ ràng hoặc đang chờ quyết định đều được ghi nhận công khai tại mục *Open Questions*.

---

### 2.2. Danh mục tài liệu kỹ thuật cốt lõi

Dưới đây là danh sách 7 tài liệu kỹ thuật thành phần thuộc thư mục `docs/00_Project/`:

| STT | Tên tài liệu | File đính kèm | Mô tả mục đích sử dụng |
| :---: | :--- | :--- | :--- |
| 1 | **Trang chỉ mục tổng quan** | `README.md` | Giới thiệu bức tranh tổng thể, mục lục chỉ mục và hướng dẫn khai thác toàn bộ thư mục `docs/00_Project/`. |
| 2 | **Tổng quan dự án** | `ProjectOverview.md` | Định nghĩa bài toán nghiệp vụ siêu thị bán lẻ, mục tiêu hệ thống, danh mục chức năng cho 3 vai trò (Admin, Staff, Customer), ứng dụng AI và bảng đối chiếu tiêu chí Rubric đánh giá. |
| 3 | **Kiến trúc hệ thống** | `Architecture.md` | Mô tả chi tiết kiến trúc 3 tầng độc lập (3-Tier Architecture), sơ đồ luồng dữ liệu Mermaid, mô hình Clean Architecture (Feature-Folder), luồng xác thực JWT/BCrypt và tích hợp AI. |
| 4 | **Ma trận công nghệ** | `TechStack.md` | Liệt kê toàn bộ công nghệ, thư viện, framework (C# .NET 8, WinForms, React TS, SQL Server, EF Core, Google Gemini, ZXing.Net, QuestPDF, LiveCharts2, Docker) và lý do lựa chọn. |
| 5 | **Cấu trúc thư mục** | `FolderStructure.md` | Mô tả sơ đồ cây thư mục Monorepo toàn bộ hệ thống (`Backend/`, `Desktop/`, `Frontend/`, `docs/`, `docker/`, `.github/`) và nguyên tắc sắp xếp theo tính năng. |
| 6 | **Quy chuẩn lập trình** | `CodingConvention.md` | Quy định chuẩn đặt tên code C#/TypeScript, chuẩn định dạng RESTful API Response (`ApiResult<T>`), xử lý ngoại lệ và quy tắc Git Commit. |
| 7 | **Lộ trình & Phân công** | `Roadmap.md` | Quy hoạch 13 giai đoạn phát triển tài liệu/phần mềm và bảng phân công công việc cụ thể cho nhóm 5 người. |

---

### 2.3. Hướng dẫn sử dụng tài liệu theo vai trò

Để khai thác tài liệu hiệu quả nhất, các thành viên cần tiếp cận theo định hướng sau:

* **Software Architect & Team Leader**:
  - Tham chiếu `Architecture.md` và `FolderStructure.md` để kiểm soát cấu trúc mã nguồn.
  - Theo dõi `Roadmap.md` để phân công công việc và đánh giá tiến độ.
* **Backend Developer (.NET 8 API)**:
  - Đọc kỹ `Architecture.md` (tầng Infrastructure & Features) và `CodingConvention.md`.
  - Tham chiếu `ProjectOverview.md` để nắm rõ nghiệp vụ từng endpoint API cần viết.
* **Desktop Developer (WinForms Admin/Staff)**:
  - Đọc `ProjectOverview.md` (chức năng Staff POS & Admin) và `TechStack.md` (thư viện ZXing.Net, LiveCharts2, QuestPDF).
  - Đọc `CodingConvention.md` để gọi REST API bất đồng bộ (`HttpClient async/await`).
* **Web Frontend Developer (React TS Customer)**:
  - Đọc `ProjectOverview.md` (chức năng Customer) và `CodingConvention.md` (định dạng `ApiResult<T>`).
* **Tester & Technical Writer**:
  - Đọc `ProjectOverview.md` để lập Test Cases cho từng vai trò người dùng.
  - Sử dụng bộ tài liệu này làm căn cứ viết Báo cáo đồ án môn học.

---

### 2.4. Quy định bảo trì và cập nhật tài liệu

1. **Nguyên tắc cập nhật**:
   - Khi có sự thay đổi về kiến trúc, công nghệ hoặc nghiệp vụ, tài liệu tương ứng tại `docs/00_Project/` phải được cập nhật **trước hoặc song song** với quá trình viết code.
   - Không được phép thay đổi mã nguồn làm lệch khỏi tài liệu mà chưa có sự thảo luận và đồng thuận của nhóm.
2. **Quy định Commit tài liệu**:
   - Các commit cập nhật tài liệu phải sử dụng tiền tố `docs:` theo chuẩn Conventional Commits (Ví dụ: `docs: update Architecture.md for JWT flow`).

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Ghi chú / Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Quy trình xác thực OTP cho số điện thoại của Khách hàng trên Web sẽ sử dụng dịch vụ thật (như Twilio/Firebase) hay giả lập (Mock OTP trong môi trường Dev)? | Ảnh hưởng tới chi phí và cấu hình trong `TechStack.md` & `Architecture.md`. | Đề xuất dùng Mock OTP cố định (ví dụ: `666666`) ở phiên bản v1 để tập trung làm mượt luồng nghiệp vụ. |
| 2 | Việc kết nối CSDL SQL Server trên môi trường Production sẽ chạy qua Docker Container hay kết nối instance SQL Server Express cài trực tiếp trên OS máy chủ? | Ảnh hưởng file `docker-compose.yml` trong `11_Deployment/`. | Đề xuất chuẩn bị sẵn cấu hình Docker Compose cho SQL Server / PostgreSQL. |

---

## 4. GHI CHÚ
- Thư mục `docs/00_Project/` nằm tại đường dẫn gốc của repository: `Smart-SuperMarket/docs/00_Project/`.
- Mọi sơ đồ trong tài liệu được vẽ bằng ngôn ngữ **Mermaid**. Để hiển thị tốt nhất, nên sử dụng các công cụ có hỗ trợ Mermaid như VS Code (Extension: *Markdown Preview Mermaid Support*), GitHub Web UI, hoặc Obsidian.

---

## 5. KẾT LUẬN

Tài liệu `README.md` này mở đầu cho chuỗi tài liệu kỹ thuật của hệ thống **Smart SuperMarket**. Bằng việc thiết lập rõ ràng danh mục tài liệu, đối tượng sử dụng và quy trình bảo trì, thư mục `00_Project/` đảm bảo dự án được triển khai đúng hướng, chất lượng và đáp ứng đầy đủ các tiêu chuẩn kỹ thuật cũng như yêu cầu môn học.
