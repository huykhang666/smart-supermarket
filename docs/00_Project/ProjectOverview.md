# TỔNG QUAN DỰ ÁN SMART SUPERMARKET

---

## MỤC LỤC
1. [Giới thiệu](#1-giới-thiệu)
2. [Nội dung chính](#2-nội-dung-chính)
   - 2.1. [Bối cảnh bài toán & Mục tiêu dự án](#21-bối-cảnh-bài-toán--mục-tiêu-dự-án)
   - 2.2. [Phân hệ chức năng theo vai trò người dùng](#22-phân-hệ-chức-năng-theo-vai-trò-người-dùng)
     - 2.2.1. [Vai trò Khách hàng (Customer - Web App)](#221-vai-trò-khách-hàng-customer---web-app)
     - 2.2.2. [Vai trò Nhân viên (Staff - WinForms Desktop)](#222-vai-trò-nhân-viên-staff---winforms-desktop)
     - 2.2.3. [Vai trò Quản trị viên (Admin - WinForms Desktop)](#223-vai-trò-quản-trị-viên-admin---winforms-desktop)
   - 2.3. [Các tính năng Trí tuệ Nhân tạo (AI Integration)](#23-các-tính-năng-trí-tuệ-nhân-tạo-ai-integration)
   - 2.4. [Bảng đánh giá mức độ đáp ứng Rubric Đồ án (Mục tiêu 10.0/10.0)](#24-bảng-đánh-giá-mức-độ-đáp-ứng-rubric-đồ-án-mục-tiêu-100100)
3. [Open Questions](#3-open-questions)
4. [Ghi chú](#4-ghi-chú)
5. [Kết luận](#5-kết-luận)

---

## 1. GIỚI THIỆU

**Smart SuperMarket** là một hệ thống quản lý siêu thị thông minh toàn diện, tích hợp công nghệ .NET hiện đại, kiến trúc API-first và trí tuệ nhân tạo (AI). Hệ thống mô phỏng quy trình vận hành thực tế của một chuỗi siêu thị bán lẻ (tương tự mô hình Bách Hóa Xanh hoặc WinMart), bao gồm việc phục vụ khách hàng trực tuyến qua Web App và vận hành bán hàng/quản lý kho chuyên nghiệp tại cửa hàng qua phần mềm Desktop WinForms.

Tài liệu này cung cấp cái nhìn tổng quan về mục tiêu, danh mục chức năng chi tiết cho từng vai trò người dùng, các tính năng AI hỗ trợ ra quyết định kinh doanh và bảng đối chiếu các tiêu chí Rubric nhằm đảm bảo dự án đạt điểm tối đa trong kỳ đánh giá đồ án môn học Lập trình Windows.

---

## 2. NỘI DUNG CHÍNH

### 2.1. Bối cảnh bài toán & Mục tiêu dự án

#### Bối cảnh nghiệp vụ
Trong ngành bán lẻ siêu thị hiện đại, các thách thức chính bao gồm:
- **Tốc độ thanh toán tại quầy**: Cần nhận diện sản phẩm qua mã vạch (Barcode/QR Code) tức thì để tránh ùn tắc vào giờ cao điểm.
- **Quản lý kho & Hạn sử dụng (FEFO - First Expired, First Out)**: Nguy cơ thất thoát do hàng hóa hết hạn nếu không được theo dõi và đẩy bán kịp thời.
- **Trải nghiệm khách hàng**: Cần kết nối giữa trải nghiệm mua sắm tại cửa hàng và đặt hàng trực tuyến, kết hợp chương trình chăm sóc khách hàng thân thiết (Tích điểm, Đổi Voucher).
- **Hỗ trợ ra quyết định**: Quản lý cần dữ liệu dự báo để nhập hàng vừa đủ và phân tích xu hướng doanh thu tự động thay vì thống kê thủ công.

#### Mục tiêu hệ thống (Phiên bản v1)
- Triển khai thành công cho **1 chi nhánh siêu thị chuẩn**.
- Xây dựng hệ thống 3 phân hệ hoàn chỉnh:
  1. **Backend REST API (.NET 8)**: Trung tâm xử lý toàn bộ nghiệp vụ, lưu trữ dữ liệu tập trung trên SQL Server.
  2. **WinForms Desktop Application**: Phục vụ Nhân viên (Staff) bán hàng POS quét mã vạch và Quản trị viên (Admin) quản lý kho, nhân sự, xem biểu đồ báo cáo.
  3. **React + TypeScript Web Application**: Phục vụ Khách hàng (Customer) mua sắm, tích điểm và theo dõi đơn hàng online.
- Ứng dụng **Google Gemini API** như một công cụ hỗ trợ ra quyết định kinh doanh thực tế.

---

### 2.2. Phân hệ chức năng theo vai trò người dùng

#### 2.2.1. Vai trò Khách hàng (Customer - Web App)
Khách hàng tương tác với hệ thống thông qua giao diện Web Responsive (React + TypeScript):
- **Đăng ký tài khoản bằng Số điện thoại (OTP)**: Đăng ký thành viên nhanh chóng thông qua xác thực số điện thoại và mã OTP.
- **Đăng nhập hệ thống**: Đăng nhập an toàn bằng Số điện thoại/Email và Mật khẩu.
- **Duyệt & Tìm kiếm sản phẩm**: Xem danh sách sản phẩm theo danh mục, tìm kiếm theo tên, xem chi tiết giá niêm yết và hình ảnh minh họa.
- **Đặt hàng trực tuyến**: Thêm sản phẩm vào giỏ hàng, chọn phương thức thanh toán và đặt hàng online.
- **Theo dõi đơn hàng**: Kiểm tra trạng thái tiến độ đơn hàng (Chờ chuẩn bị $\rightarrow$ Đang giao $\rightarrow$ Hoàn tất / Đã hủy).
- **Tích điểm thành viên (Loyalty Points)**: Tự động tích lũy điểm thưởng dựa trên giá trị hóa đơn đã thanh toán.
- **Đổi Voucher giảm giá**: Sử dụng điểm thưởng tích lũy để đổi lấy các mã giảm giá (Voucher) áp dụng cho các đơn hàng tiếp theo.

#### 2.2.2. Vai trò Nhân viên (Staff - WinForms Desktop)
Nhân viên thao tác tại cửa hàng trên ứng dụng WinForms Desktop (Tài khoản được Admin khởi tạo):
- **Đăng nhập tài khoản Nhân viên**: Đăng nhập theo tài khoản được cấp, hệ thống tự động phân quyền chỉ mở các chức năng nghiệp vụ của Staff.
- **Quét Barcode / QR Code bán hàng tại quầy (POS)**:
  - Sử dụng máy quét mã vạch chuyên dụng USB hoặc Webcam để quét mã sản phẩm.
  - Tự động tra cứu thông tin sản phẩm và nhảy giỏ hàng tức thì.
  - Tính tổng tiền, áp dụng Voucher/Khuyến mãi, tích điểm cho khách hàng và hoàn tất thanh toán.
  - In hóa đơn bán hàng trực tiếp hoặc xuất file PDF.
- **Chuẩn bị đơn hàng Online**: Tiếp nhận các đơn hàng online do Khách hàng đặt từ Web, tiến hành soạn hàng và cập nhật trạng thái đơn hàng.
- **Quản lý nhập kho (Import Inventory)**: Lap phiếu nhập hàng từ Nhà cung cấp, ghi nhận số lượng nhập, giá nhập và **Hạn sử dụng (ExpiryDate)** cho từng lô hàng theo nguyên tắc FEFO.

#### 2.2.3. Vai trò Quản trị viên (Admin - WinForms Desktop)
Quản trị viên có toàn quyền kiểm soát hệ thống trên ứng dụng WinForms Desktop:
- **Quản lý toàn bộ hệ thống**: Thêm, sửa, xóa, tìm kiếm nâng cao đối với Sản phẩm, Danh mục, Nhà cung cấp, Chi nhánh, Chương trình Khuyến mãi.
- **Quản lý Nhân viên**: Tạo tài khoản cho Nhân viên mới, cập nhật thông tin cá nhân (Ngày tháng năm sinh, SĐT, Email), phân công vai trò (Staff/Manager), quản lý trạng thái tài khoản (Hoạt động/Khóa).
- **Dashboard Thống kê & Báo cáo**:
  - Xem bảng điều khiển tổng quan: Doanh thu hôm nay, top sản phẩm bán chạy, cảnh báo sản phẩm tồn kho thấp, cảnh báo sản phẩm sắp hết hạn.
  - Xem biểu đồ doanh thu tương tác (LiveCharts2) theo ngày, tuần, tháng.
- **Quản lý Báo cáo AI (AI Reports)**: Kích hoạt các công cụ AI và xem các bài báo cáo kinh doanh được sinh tự động.

---

### 2.3. Các tính năng Trí tuệ Nhân tạo (AI Integration)

Dự án tích hợp **Google Gemini API** để thực hiện các bài toán hỗ trợ ra quyết định kinh doanh:

1. **Gợi ý sản phẩm (Product Recommendations)**: Phân tích thói quen mua sắm của Khách hàng để gợi ý các sản phẩm phù hợp khi xem chi tiết hoặc thanh toán.
2. **Gợi ý nguyên liệu nấu ăn (Cooking Ingredient Suggestions)**: Tính năng thông minh cho phép Khách hàng chọn một món ăn (ví dụ: *Lẩu thái*, *Bò kho*), AI sẽ tự động gợi ý danh sách các nguyên liệu cần mua sẵn có trong siêu thị và đưa vào giỏ hàng.
3. **Báo cáo doanh thu (Revenue Reports)**: AI phân tích nguyên nhân biến động doanh thu (ví dụ: doanh số tăng/giảm do nhóm hàng nào) và tự động viết báo cáo tổng kết dạng văn bản tự nhiên.
4. **Dự đoán nhập hàng (Import Demand Forecasting)**: Dựa trên số liệu bán hàng của 7–30 ngày gần nhất, AI đưa ra đề xuất số lượng cụ thể nên nhập kho cho tuần tới để tối ưu tồn kho.
5. **Phân tích sản phẩm bán chạy (Best-selling Product Analysis)**: AI thống kê xu hướng tiêu dùng, đánh giá các mặt hàng mang lại lợi nhuận cao nhất và đề xuất chiến lược bày bán.

---

### 2.4. Bảng đánh giá mức độ đáp ứng Rubric Đồ án (Mục tiêu 10.0/10.0)

Hệ thống được thiết kế để đáp ứng trọn vẹn Mức 3 (Tốt / Xuất sắc) trong Bảng Rubric chấm điểm Đồ án Lập trình Windows:

| Mã tiêu chí | Nội dung Rubric | Giải pháp thiết kế của Smart SuperMarket | Điểm tối đa |
| :---: | :--- | :--- | :---: |
| **A1** | Bố cục & Thẩm mỹ giao diện | Giao diện WinForms thiết kế chuyên nghiệp, màu sắc nhất quán, có Splash Screen và Loading Indicator async. | 0.5 |
| **A2** | Đa dạng Controls ($\ge 8$ loại) | Dùng > 8 loại control: TabControl, GroupBox, DataGridView, ComboBox, DateTimePicker, NumericUpDown, ProgressBar, RichTextBox, ToolStrip, StatusStrip. | 0.5 |
| **A3** | Validation & Báo lỗi thân thiện | Validate real-time bằng `ErrorProvider` (báo đỏ ô sai, không crash app), thông báo rõ ràng kèm Tooltip hướng dẫn. | 0.5 |
| **B1** | Hệ thống Đăng nhập / Đăng ký / Đăng xuất | Đăng nhập/Đăng xuất/Đăng ký/Quên mật khẩu, mật khẩu mã hóa BCrypt + Salt, khóa tài khoản nếu nhập sai nhiều lần. | 0.5 |
| **B2** | Phân quyền $\ge 2$ vai trò (Admin & Staff) | Phân quyền động từ DB. Menu/Button ẩn theo Role. Log hoạt động tài khoản chi tiết (Audit Trail). | 0.5 |
| **C1** | CRUD Nghiệp vụ phức tạp | CRUD đa bảng trong giao dịch Bán hàng POS (`Order`, `OrderDetail`, `Inventory`, `Payment`), có Confirm Dialog, Transaction an toàn. | 1.0 |
| **C2** | Tìm kiếm & Lọc dữ liệu nâng cao | Tìm kiếm real-time theo tên/barcode trên DataGridView, lọc đa tiêu chí, highlight kết quả và xuất Excel. | 0.5 |
| **C3** | EF Core / Async đúng cách | Dùng EF Core Code First + Repository Pattern, `async/await` toàn bộ thao tác DB để không đóng băng UI. | 0.25 |
| **D1** | Tác vụ nặng không đóng băng UI | Sử dụng `Task` + `Progress<T>` + `CancellationToken` cho tiến trình In PDF / Import Excel có ProgressBar mượt mà. | 0.5 |
| **E1** | Vẽ GDI+ tùy chỉnh | Vẽ GDI+ khung Thẻ thành viên (`Membership Card`) hoặc biểu đồ tùy chỉnh có hiệu ứng Gradient & Hover tương tác. | 0.5 |
| **E2** | Dashboard biểu đồ tương tác | Bảng điều khiển tích hợp biểu đồ `LiveCharts2` (cột, tròn) hiển thị doanh thu, top bán chạy, có tooltip và filter thời gian. | 0.5 |
| **F1** | Xuất báo cáo PDF / Excel | Xuất hóa đơn bán hàng và báo cáo ra file PDF (`QuestPDF`) có Header/Footer, Logo, tổng tiền và chức năng Preview. | 0.5 |
| **F2** | Import Excel & Mã vạch QR/Barcode | Import danh sách sản phẩm từ file Excel có validate; Nhận diện Barcode/QR tức thì qua Máy quét USB / Camera (`ZXing.Net`). | 0.5 |
| **F3** | OpenFileDialog, SaveFileDialog, Drag-Drop | Dùng đủ 3 loại Dialog, hỗ trợ Kéo-Thả (Drag & Drop) file Excel/Ảnh sản phẩm trực tiếp vào Form WinForms. | 0.5 |
| **G1** | REST API & HttpClient async | WinForms gọi API qua `HttpClient` bất đồng bộ (`async/await`), BaseAddress lấy từ `appsettings.json`, có retry policy và cache. | 0.25 |
| **G3** | Tích hợp Thanh toán Sandbox | Tích hợp cổng thanh toán Sandbox (VNPay / MoMo QR Code) khi thanh toán đơn hàng. | 0.25 |
| **G4** | Tích hợp Chatbot / AI (Gemini API) | Gọi Google Gemini API thực hiện AI Dự báo nhập hàng, Gợi ý giảm giá cận hạn và Sinh báo cáo văn bản tự động. | 0.5 |
| **G5** | Tích hợp Email thông báo | Gửi Email SMTP xác nhận hóa đơn thanh toán và reset mật khẩu bằng Template HTML đẹp. | 0.25 |
| **H1** | Layered Architecture / DI | Tách 3 tầng rõ ràng (UI / Business Logic / Data Access), ứng dụng Dependency Injection (DI) và Repository Pattern. | 0.25 |
| **H2** | Cấu hình & Bảo mật | Đọc cấu hình từ `appsettings.json`, xử lý lỗi ngoại lệ toàn cục (`Application.ThreadException`), mã hóa Connection String. | 0.25 |
| **H3** | Deployment / Publish | Publish phần mềm dạng Self-Contained (.exe chạy không cần cài .NET runtime), có tài liệu hướng dẫn README chi tiết. | 0.25 |
| **I1** | Localization đa ngôn ngữ | Đa ngôn ngữ Tiếng Việt + Tiếng Anh sử dụng file Resource `.resx`, cho phép chuyển đổi ngôn ngữ realtime trên Form. | 0.25 |
| **I2** | Tính năng sáng tạo nổi bật | Kết hợp bán hàng mã vạch POS + AI gợi ý giảm giá HSD (FEFO) + Web Customer đồng bộ điểm thưởng. | 0.25 |
| **TỔNG** | **ĐÁNH GIÁ TỔNG THỂ** | **ĐẠT ĐIỂM TỐI ĐA** | **10.0** |

---

## 3. OPEN QUESTIONS

| STT | Vấn đề chưa quyết định | Tác động | Hướng xử lý đề xuất |
| :---: | :--- | :--- | :--- |
| 1 | Cổng thanh toán Sandbox (VNPay/MoMo) sẽ được gọi trực tiếp từ WinForms POS hay đi qua .NET Web API? | Ảnh hưởng đến luồng Webhook/Callback thanh toán. | Đề xuất: WinForms gọi API Backend `POST /api/payments/create-qr`, Backend sinh mã QR VNPay/MoMo để WinForms hiển thị lên màn hình cho khách quét. |
| 2 | Việc chọn nguyên liệu nấu ăn (Cooking Ingredient Suggestions) sẽ hiển thị gợi ý danh mục nguyên liệu thô hay hiển thị chính xác Mã sản phẩm cụ thể trong siêu thị? | Ảnh hưởng đến logic prompt AI Gemini và việc tự động thêm vào giỏ hàng. | Đề xuất: AI trả về danh sách tên nguyên liệu $\rightarrow$ Backend tự tìm các `Product` tương ứng trong CSDL có tồn kho > 0 để gợi ý cho khách hàng. |

---

## 4. GHI CHÚ
- Các tính năng dành cho **Staff** và **Admin** được ưu tiên phát triển trọn vẹn trên ứng dụng **WinForms Desktop** để phục vụ báo cáo môn học Lập trình Windows.
- Phân hệ **Web App (React TS)** cho Customer được thiết kế như một kênh giao tiếp mở rộng, sử dụng chung tập REST API của Backend.

---

## 5. KẾT LUẬN

Tài liệu `ProjectOverview.md` đã làm rõ bối cảnh bài toán, mục tiêu hệ thống, danh mục chức năng chi tiết cho 3 vai trò (Customer, Staff, Admin), 5 bài toán ứng dụng AI và chứng minh khả năng đáp ứng trọn vẹn 10.0/10.0 điểm theo Rubric môn học. Tài liệu này là căn cứ nghiệp vụ chính xác để phát triển các phân hệ tài liệu tiếp theo.
