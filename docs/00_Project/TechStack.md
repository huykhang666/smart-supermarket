# MA TRẬN CÔNG NGHỆ (TECH STACK) SMART SUPERMARKET

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Bảng tổng hợp Ma trận Công nghệ](#21-bảng-tổng-hợp-ma-trận-công-nghệ)
   - 2.2. [Chi tiết Tầng Backend (.NET 8 Web API)](#22-chi-tiết-tầng-backend-net-8-web-api)
   - 2.3. [Chi tiết Tầng Desktop (C# WinForms)](#23-chi-tiết-tầng-desktop-c-winforms)
   - 2.4. [Chi tiết Tầng Web Client (React + TypeScript)](#24-chi-tiết-tầng-web-client-react--typescript)
   - 2.5. [Cơ sở Dữ liệu & ORM Layer](#25-cơ-sở-dữ-liệu--orm-layer)
   - 2.6. [AI & Dịch vụ Bên ngoài (External Integrations)](#26-ai--dịch-vụ-bên-ngoài-external-integrations)
   - 2.7. [Hạ tầng DevOps, Containerization & CI/CD](#27-hạ-tầng-devops-containerization--cicd)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **TechStack.md** định nghĩa ma trận công nghệ, các công cụ, thư viện, framework và môi trường thực thi được lựa chọn để xây dựng hệ thống **Smart SuperMarket**. Tài liệu cung cấp lý do lựa chọn kỹ thuật, phiên bản cụ thể và vai trò của từng thành phần trong bức tranh tổng thể dự án.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Bảng tổng hợp Ma trận Công nghệ

| Tầng / Thành phần | Công nghệ / Thư viện | Phiên bản | Vai trò & Lý do lựa chọn |
| :--- | :--- | :---: | :--- |
| **Backend Framework** | ASP.NET Core Web API | .NET 8 / 9 | Hiệu năng cao, cross-platform, hỗ trợ async native, chuẩn doanh nghiệp. |
| **Desktop Framework** | C# WinForms App | .NET 8 / 9 | Đáp ứng 100% yêu cầu môn Lập trình Windows, giao diện trực quan, kết nối ngoại vi tốt. |
| **Web Frontend** | React + TypeScript + Vite | React 18, TS 5 | Giao diện Web Khách hàng mượt mà, type-safety cao, tốc độ build siêu nhanh với Vite. |
| **Database Server** | SQL Server / PostgreSQL | SQL 2022 / PG 16 | CSDL quan hệ tin cậy, lưu trữ 20 bảng nghiệp vụ, chạy nhẹ nhàng trong Docker Container. |
| **ORM Framework** | Entity Framework Core | EF Core 8/9 | Thao tác CSDL Code First, hỗ trợ LINQ, Migration tự động, phòng chống SQL Injection. |
| **Authentication** | JWT Bearer Token | `System.IdentityModel.Tokens.Jwt` | Xác thực không lưu trạng thái (Stateless), dùng chung cho cả WinForms và Web App. |
| **Password Hashing** | BCrypt.Net-Next | v4.0.3 | Mã hóa mật khẩu an toàn với Salt tự động, chống tấn công Brute-force/Rainbow table. |
| **Barcode / QR Code** | ZXing.Net / AForge.NET | v0.16.9 | Nhận diện và tự động đọc mã vạch Barcode/QR Code qua webcam/máy quét USB. |
| **Thống kê Biểu đồ** | LiveCharts2 | v2.0.0-beta | Vẽ biểu đồ tương tác Doanh thu (Cột, Tròn, Đường) đẹp mắt trên Form WinForms. |
| **Xuất Báo cáo PDF** | QuestPDF / iTextSharp | v2023.12 | Định dạng và xuất hóa đơn bán hàng POS ra file PDF chuyên nghiệp. |
| **Trí tuệ Nhân tạo** | Google Gemini API | REST API | Dự báo số lượng nhập hàng, gợi ý giảm giá cận hạn FEFO và tự sinh báo cáo văn bản. |
| **Containerization** | Docker & Docker Compose | v24.0+ | Đóng gói môi trường CSDL và Web Server chạy đồng bộ trên mọi máy tính. |
| **CI/CD Pipeline** | GitHub Actions | Workflows v3 | Tự động hóa kiểm thử mã nguồn và build dự án mỗi khi push code. |

---

### 2.2. Chi tiết Tầng Backend (.NET 8 Web API)

- **Runtime**: .NET 8 LTS (Long Term Support) cung cấp hiệu năng vượt trội và quản lý bộ nhớ tối ưu.
- **RESTful API Engine**: ASP.NET Core Controllers hỗ trợ `[ApiController]`, routing linh hoạt, tự động validate DTOs.
- **Dependency Injection (DI)**: Sử dụng IoC Container có sẵn của .NET 8 (`IServiceCollection`) để đăng ký Scope cho Services, Repositories và Clients.
- **Serialization**: `System.Text.Json` giúp đóng/mở gói dữ liệu JSON tốc độ cao.
- **Bảo mật**: `Microsoft.AspNetCore.Authentication.JwtBearer` xử lý xác thực Token header.

---

### 2.3. Chi tiết Tầng Desktop (C# WinForms)

- **UI Engine**: Windows Forms (.NET 8) tương thích cao với Windows 10/11, hỗ trợ thiết kế giao diện dạng kéo-thả trực quan.
- **Quét Mã vạch (POS)**:
  - Máy quét cầm tay USB: Hoạt động qua giả lập bàn phím (USB HID Keyboard Mode).
  - Webcam laptop: Sử dụng `ZXing.Net` kết hợp `AForge.NET` xử lý khung hình camera theo thời gian thực.
- **Biểu đồ Dashboard**: `LiveCharts.WinForms` (LiveCharts2) hỗ trợ hiển thị dữ liệu doanh thu dạng cột và tròn với hiệu ứng mượt mà.
- **Xuất file PDF**: `QuestPDF` thiết kế bố cục hóa đơn bán hàng POS (có Logo, Mã hóa đơn, Chi tiết mặt hàng, Mã QR).
- **HTTP Client**: Sử dụng `HttpClientFactory` để gửi request bất đồng bộ (`async/await`) sang Backend API.

---

### 2.4. Chi tiết Tầng Web Client (React + TypeScript)

- **Build Tool**: **Vite** cung cấp môi trường phát triển siêu nhanh (Hot Module Replacement - HMR).
- **Ngôn ngữ**: **TypeScript** đảm bảo type-checking chặt chẽ cho toàn bộ DTOs kết nối từ Backend.
- **State & Data Fetching**: **TanStack Query (React Query v5)** quản lý caching dữ liệu, tự động refetch và xử lý trạng thái Loading/Error.
- **UI Framework**: **TailwindCSS** hỗ trợ dựng giao diện Responsive hiện đại, tối ưu cho mobile và laptop.

---

### 2.5. Cơ sở Dữ liệu & ORM Layer

- **Database Engine**: SQL Server 2022 Express hoặc PostgreSQL 16 Alpine Docker Container.
- **ORM (Entity Framework Core)**:
  - Tiếp cận theo mô hình **Code First**: Định nghĩa 20 Entity Classes trong C# $\rightarrow$ Tự động sinh Migration script.
  - Sử dụng **Fluent API** trong `Configurations/` để thiết lập các ràng buộc Foreign Key, UNIQUE Index và Check Constraints.
  - Tích hợp **Repository Pattern** đảm bảo khả năng viết Unit Test và dễ dàng chuyển đổi CSDL nếu cần.

---

### 2.6. AI & Dịch vụ Bên ngoài (External Integrations)

1. **Google Gemini API**:
   - Sử dụng mô hình `gemini-1.5-flash` cho tốc độ phản hồi nhanh và chi phí tối ưu.
   - Kết nối qua `HttpClient` với Header `x-goog-api-key`.
2. **Cổng Thanh toán Sandbox**:
   - Tích hợp VNPay / MoMo Sandbox API để tạo mã VietQR động cho đơn hàng tại quầy POS.

---

### 2.7. Hạ tầng DevOps, Containerization & CI/CD

- **Docker Compose**: File `docker-compose.yml` quản lý dịch vụ PostgreSQL CSDL và Nginx Reverse Proxy.
- **GitHub Actions**: File `.github/workflows/ci-cd.yml` tự động chạy `dotnet restore` và `dotnet build` để kiểm tra lỗi cú pháp mỗi khi có Pull Request mới.

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Thư viện vẽ biểu đồ `LiveCharts2` trên WinForms có cần cài thêm bản trả phí không? | Ảnh hưởng bản quyền và ngân sách đồ án. | Sử dụng bản mã nguồn mở miễn phí `LiveCharts2 (LiveChartsCore.SkiaSharpView.WinForms)` hoàn toàn đáp ứng đủ nhu cầu đồ án. |
| 2 | Thư viện in hóa đơn `QuestPDF` ở các phiên bản mới có yêu cầu License Commercial không? | Ảnh hưởng việc xuất file PDF. | Sử dụng phiên bản QuestPDF Community License (miễn phí cho mục đích phi thương mại/học tập). |

---

## 4. GHI CHÚ
- Tất cả các gói NuGet và npm dependencies đều phải được khóa phiên bản trong `SmartSupermarket.Backend.csproj` và `package-lock.json` để tránh lỗi xung đột khi thành viên trong nhóm `dotnet restore` hoặc `npm install`.

---

## 5. KẾT LUẬN

Tài liệu `TechStack.md` đã làm rõ toàn bộ ma trận công nghệ được áp dụng cho dự án Smart SuperMarket. Việc lựa chọn công nghệ dựa trên sự kết hợp hài hòa giữa yêu cầu đồ án môn học WinForms, tính hiện đại của .NET 8/React TS và khả năng ứng dụng AI thực tế.
