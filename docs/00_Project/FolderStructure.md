# QUY HOẠCH CẤU TRÚC THƯ MỤC (FOLDER STRUCTURE) SMART SUPERMARKET

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Cấu trúc Monorepo Tổng thể Dự án](#21-cấu-trúc-monorepo-tổng-thể-dự-án)
   - 2.2. [Chi tiết Thư mục Backend (`Backend/`)](#22-chi-tiết-thư-mục-backend-backend)
   - 2.3. [Chi tiết Thư mục Desktop (`Desktop/`)](#23-chi-tiết-thư-mục-desktop-desktop)
   - 2.4. [Chi tiết Thư mục Frontend (`Frontend/`)](#24-chi-tiết-thư-mục-frontend-frontend)
   - 2.5. [Chi tiết Thư mục Tài liệu & Hạ tầng (`docs/`, `docker/`, `.github/`)](#25-chi-tiết-thư-mục-tài-liệu--hạ-tầng-docs-docker-github)
   - 2.6. [Quy tắc Đặt tên Thư mục và File](#26-quy-tắc-đặt-tên-thư-mục-và-file)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

Tài liệu **FolderStructure.md** mô tả cấu trúc quy hoạch thư mục cho toàn bộ dự án **Smart SuperMarket**. Dự án được tổ chức theo mô hình **Monorepo**, gom tất cả các mã nguồn của Backend API, Desktop WinForms App, Web Client React TS, tài liệu kỹ thuật và hạ tầng Docker vào trong một repository duy nhất.

Tài liệu này giúp các lập trình viên hiểu rõ vị trí của từng tệp tin mã nguồn, đảm bảo tuân thủ nguyên tắc mô-đun hóa và dễ dàng bảo trì.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Cấu trúc Monorepo Tổng thể Dự án

```text
Smart-SuperMarket/
├── Backend/                 # ASP.NET Core Web API (.NET 8)
├── Desktop/                 # C# WinForms App (Admin, Staff POS)
├── Frontend/                # React + TypeScript + Vite (Customer Web)
├── docs/                    # Thư mục Tài liệu kỹ thuật 13 phân hệ
├── docker/                  # Cấu hình Docker & Docker Compose
├── .github/                 # Cấu hình GitHub Actions CI/CD Workflows
├── .gitignore               # Cấu hình bỏ qua các file rác Git
└── SmartSupermarket.slnx    # Solution File duy nhất của C# Visual Studio
```

---

### 2.2. Chi tiết Thư mục Backend (`Backend/`)

Backend tuân thủ kiến trúc **Feature-Folder / Vertical Slice Clean Architecture**:

```text
Backend/
├── Common/                          # Tiện ích dùng chung hệ thống
│   ├── Barcode/
│   │   └── BarcodeGenerator.cs     # Hàm tự sinh chuỗi Barcode EAN-13 / QR
│   ├── Results/
│   │   └── ApiResult.cs            # Class bọc kết quả trả về API chuẩn (Standard Wrapper)
│   └── Services/
│       ├── IDateTimeProvider.cs    # Interface Provider ngày giờ cho DI
│       └── SystemDateTimeProvider.cs
│
├── Domain/                          # Cốt lõi Nghiệp vụ (Core Domain)
│   ├── Entities/                   # Mỗi Entity 1 file riêng đại diện các bảng CSDL
│   │   ├── User.cs                 # Tài khoản người dùng & Tích điểm
│   │   ├── Product.cs              # Sản phẩm & Mã vạch Barcode
│   │   ├── Category.cs             # Danh mục sản phẩm
│   │   ├── Supplier.cs             # Nhà cung cấp
│   │   ├── Order.cs                # Hóa đơn bán hàng
│   │   └── OrderDetail.cs          # Chi tiết hóa đơn
│   └── Enums/                      # Các Enum hằng số hệ thống
│       ├── UserRole.cs             # Admin, Staff, Customer
│       ├── UserStatus.cs           # Active, Locked
│       ├── PaymentMethod.cs        # Cash, QRCode, CreditCard
│       └── OrderStatus.cs          # Completed, Cancelled
│
├── Features/                        # ĐÓNG GÓI THEO TÍNH NĂNG (Feature Folders)
│   ├── Auth/                       # Feature Đăng nhập & Xác thực
│   │   ├── Controllers/AuthController.cs
│   │   ├── DTOs/AuthDTOs.cs
│   │   └── Services/AuthService.cs
│   ├── Products/                   # Feature Quản lý & Quét mã vạch Sản phẩm
│   │   ├── Controllers/ProductsController.cs
│   │   ├── DTOs/ProductDTOs.cs
│   │   └── Services/ProductService.cs
│   ├── Orders/                     # Feature Bán hàng POS & Hóa đơn
│   ├── Inventory/                  # Feature Tồn kho theo Chi nhánh & Nhập HSD
│   ├── Promotions/                 # Feature Chương trình Khuyến mãi
│   ├── Customers/                  # Feature Hồ sơ Khách hàng & Tích điểm
│   ├── AI/                         # Feature AI Dự báo & Sinh Báo cáo
│   └── Admin/                      # Feature Dashboard Quản trị
│
├── Infrastructure/                  # Tầng Giao tiếp Hạ tầng (Persistence & Security)
│   ├── DependencyInjection.cs      # Extension Method AddInfrastructure()
│   ├── External/
│   │   └── AI/                     # Dịch vụ gọi AI bên ngoài
│   │       └── GeminiClient.cs
│   ├── Options/                    # Class ánh xạ appsettings.json
│   │   └── JwtOptions.cs
│   ├── Persistence/                # Cơ sở dữ liệu SQL Server / Postgres (EF Core)
│   │   ├── Configurations/
│   │   │   └── ProductConfiguration.cs
│   │   └── AppDbContext.cs
│   └── Security/                   # Bảo mật hệ thống (JWT & BCrypt)
│       ├── JwtService.cs
│       └── PasswordHasher.cs
│
├── Program.cs                      # File khởi chạy gốc tinh gọn
├── appsettings.json                # Cấu hình ConnectionString, JWT Key, Gemini API Key
└── SmartSupermarket.Backend.csproj
```

---

### 2.3. Chi tiết Thư mục Desktop (`Desktop/`)

Ứng dụng C# WinForms dành cho Nhân viên bán hàng POS và Admin:

```text
Desktop/
├── Forms/                          # Chứa các Giao diện WinForms (.cs & .Designer.cs)
│   ├── Admin/                      # Các Form Quản lý Sản phẩm, Nhân viên, Kho
│   ├── Auth/                       # Form Đăng nhập hệ thống (LoginForm)
│   └── POS/                        # Form Bán hàng Quét mã vạch (PosForm)
├── Models/                         # Data Models phục vụ hiển thị UI WinForms
│   └── DesktopModels.cs
├── Services/                       # Giao tiếp với Backend API
│   └── ApiClient.cs                # Client gọi HttpClient bất đồng bộ
├── Utils/                          # Helper tiện ích trên Desktop
│   └── BarcodeScannerHelper.cs     # Xử lý sự kiện máy quét mã vạch USB
├── Program.cs                      # File main khởi tạo WinForms App
└── SmartSupermarket.Desktop.csproj
```

---

### 2.4. Chi tiết Thư mục Frontend (`Frontend/`)

Ứng dụng Web dành cho Khách hàng mua sắm trực tuyến (React + TypeScript + Vite):

```text
Frontend/
├── public/                         # Chứa Favicon và các tài nguyên tĩnh công khai
├── src/
│   ├── app/                        # Cấu hình App Providers & Routes
│   ├── assets/                     # Hình ảnh, Icon, Logo
│   ├── components/common/          # UI Reusable (Navbar.tsx, Footer.tsx, Button...)
│   ├── config/                     # File cấu hình (api.config.ts)
│   ├── features/                   # Chia theo nhóm chức năng Web (products, cart, vouchers)
│   ├── layouts/                    # MainLayout.tsx, AuthLayout.tsx
│   ├── pages/                      # HomePage.tsx, CartPage.tsx, CheckoutPage.tsx
│   └── shared/                     # Utilities & API Client dùng chung
│       ├── api/axiosInstance.ts
│       ├── types/index.ts
│       └── utils/formatCurrency.ts
├── package.json
└── vite.config.ts
```

---

### 2.5. Chi tiết Thư mục Tài liệu & Hạ tầng (`docs/`, `docker/`, `.github/`)

```text
Smart-SuperMarket/
├── docs/                           # Thư mục Báo cáo & Tài liệu Kỹ thuật 13 phân hệ
│   ├── 00_Project/                 # Bộ tài liệu kiến trúc & quy chuẩn cốt lõi
│   ├── 01_Authentication/          # Tài liệu Phân hệ Xác thực & Phân quyền
│   ├── 02_Category/                # Tài liệu Phân hệ Danh mục
│   ├── 03_Supplier/                # Tài liệu Phân hệ Nhà cung cấp
│   ├── 04_Product/                 # Tài liệu Phân hệ Sản phẩm & Barcode
│   ├── 05_Inventory/               # Tài liệu Phân hệ Kho hàng
│   ├── 06_Import/                  # Tài liệu Phân hệ Nhập kho & FEFO HSD
│   ├── 07_Order/                   # Tài liệu Phân hệ Bán hàng POS & Hóa đơn
│   ├── 08_Promotion/               # Tài liệu Phân hệ Khuyến mãi
│   ├── 09_Customer/                # Tài liệu Phân hệ Khách hàng & Tích điểm
│   ├── 10_AI/                      # Tài liệu Phân hệ AI Integration
│   ├── 11_Deployment/              # Tài liệu Huớng dẫn Đóng gói & Deploy
│   └── 12_Report/                  # Quyển Báo cáo Đồ án Word/PDF & Slide
│
├── docker/                         # Hạ tầng Docker Container
│   └── docker-compose.yml          # Cấu hình container PostgreSQL / SQL Server
│
└── .github/
    └── workflows/                  # Tự động hóa CI/CD
        └── ci-cd.yml               # Workflow kiểm thử & build tự động
```

---

### 2.6. Quy tắc Đặt tên Thư mục và File

1. **C# Projects (`Backend/`, `Desktop/`)**:
   - Thư mục: Sử dụng **PascalCase** (Ví dụ: `Features/Products`, `Infrastructure/Security`).
   - Class & File C#: Sử dụng **PascalCase** (Ví dụ: `ProductService.cs`, `AuthController.cs`).
   - Interface: Bắt đầu bằng chữ `I` + **PascalCase** (Ví dụ: `IDateTimeProvider.cs`).
2. **React TypeScript (`Frontend/`)**:
   - Component & Page: Sử dụng **PascalCase** (Ví dụ: `HomePage.tsx`, `Navbar.tsx`).
   - Helper & Config: Sử dụng **camelCase** (Ví dụ: `formatCurrency.ts`, `api.config.ts`).
3. **Tài liệu Markdown (`docs/`)**:
   - Tên thư mục: Đánh số thứ tự 2 chữ số + **PascalCase** (Ví dụ: `00_Project`, `01_Authentication`).
   - Tên file Markdown: Sử dụng **PascalCase.md** (Ví dụ: `ProjectOverview.md`, `Architecture.md`).

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Các file ảnh chụp màn hình demo UI của WinForms sẽ lưu trực tiếp vào từng thư mục `docs/XX_Module/` hay gom chung vào `docs/12_Report/Screenshots/`? | Ảnh hưởng việc tổ chức tài liệu lưu trữ ảnh. | Đề xuất: Mỗi thư mục module giữ ảnh demo riêng của module đó; thư mục `12_Report` chỉ tổng hợp lại khi làm báo cáo tổng kết. |

---

## 4. GHI CHÚ
- Khi phát triển một Feature mới trong `Backend/Features/`, chỉ khởi tạo các thư mục con (`Controllers/`, `DTOs/`, `Services/`) khi bắt tay vào code feature đó để giữ cho cây Git luôn sạch sẽ.

---

## 5. KẾT LUẬN

Tài liệu `FolderStructure.md` đã chuẩn hóa chi tiết cây thư mục Monorepo cho toàn bộ hệ thống Smart SuperMarket. Việc tuân thủ cấu trúc thư mục này giúp nhóm 5 người dễ dàng phân chia công việc, tránh xung đột code khi làm việc nhóm và tạo sự chuyên nghiệp cho dự án.
