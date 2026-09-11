# 🎨 HƯỚNG DẪN THIẾT KẾ LẠI GIAO DIỆN — Smart SuperMarket ERP & POS (WinForms)

> File này dùng để đưa cho AI Agent (Cursor / Copilot / Claude Code / Windsurf...) đọc và **tự động chỉnh sửa code WinForms hiện có** theo phong cách POS siêu thị hiện đại (kiểu Bách Hóa Xanh / Circle K / WinMart). Agent nên đọc **toàn bộ file** trước khi sửa, làm theo đúng thứ tự Phase, không tự ý đổi kiến trúc code, chỉ đổi phần UI/UX.

---

## 0. Bối cảnh dự án

- Loại app: **Windows Forms (.NET / C#)**, dùng cho hệ thống ERP & POS quản lý siêu thị (sản phẩm, danh mục, nhà cung cấp, nhập kho, tồn kho, bán hàng POS, đơn hàng, khách hàng loyalty, khuyến mãi, AI Assistant, báo cáo, nhân viên/ca trực, cấu hình hệ thống).
- Vấn đề hiện tại: giao diện WinForms mặc định — nền trắng đơn điệu, viền control xám cứng, font hệ thống (Segoe UI mặc định không tinh chỉnh), bảng dữ liệu (DataGridView) kẻ ô dày, nút bấm phẳng không có điểm nhấn, không phân cấp thị giác rõ ràng giữa các khối thông tin.
- Mục tiêu: giữ nguyên toàn bộ **logic nghiệp vụ, tên biến, tên hàm, kết nối DB, luồng xử lý** — chỉ nâng cấp **lớp giao diện (UI layer)** để trông chuyên nghiệp, hiện đại, dễ dùng cho nhân viên thu ngân/quản lý.

**Nguyên tắc bất di bất dịch cho Agent:**
1. Không xoá/đổi tên control đang được reference trong code-behind (`.cs`) nếu chưa kiểm tra kỹ — chỉ đổi property (Font, BackColor, ForeColor, BorderStyle, Size, Padding, Anchor/Dock...) hoặc thay thế bằng control tương đương của thư viện UI mới.
2. Sửa từng Form một, build thử sau mỗi Form, không sửa hàng loạt rồi mới build.
3. Giữ tiếng Việt có dấu trong toàn bộ nhãn/tiêu đề như hiện tại.
4. Ưu tiên giải pháp không phá vỡ databinding hiện có (DataGridView, ComboBox, BindingSource...).

---

## 1. Bảng màu (Color Palette) — phong cách "Siêu thị xanh"

Lấy cảm hứng từ nhận diện các chuỗi bán lẻ Việt Nam (xanh lá tươi = tin cậy/tươi mới, cam/vàng = khuyến mãi, đỏ = cảnh báo).

| Vai trò | Tên biến gợi ý | Mã màu HEX | Dùng cho |
|---|---|---|---|
| Primary (thương hiệu) | `PrimaryGreen` | `#0D9C4A` | Sidebar nền, nút chính, header, logo |
| Primary Dark | `PrimaryGreenDark` | `#087A38` | Hover/pressed của nút chính, viền nhấn |
| Primary Light | `PrimaryGreenLight` | `#E6F7EC` | Nền card thống kê, hover row trong bảng |
| Accent cam (khuyến mãi/CTA) | `AccentOrange` | `#FF8A00` | Nút "Thêm", badge khuyến mãi, nút thanh toán phụ |
| Accent xanh dương (thông tin) | `InfoBlue` | `#2D7FF9` | Link, icon thông tin, biểu đồ dòng thứ 2 |
| Success | `SuccessGreen` | `#16A34A` | Trạng thái "Đang bán", "Hoạt động", "Completed" |
| Warning | `WarningAmber` | `#F59E0B` | "Sắp hết hạn", "Cận date", "Preparing" |
| Danger | `DangerRed` | `#DC2626` | "Cảnh báo đỏ", "Hết hàng", "Ngừng bán", "Tạm dừng" |
| Nền chính | `BackgroundGray` | `#F4F6F8` | Nền toàn Form (thay cho trắng thuần) |
| Nền Card/Panel | `SurfaceWhite` | `#FFFFFF` | Panel, Card, DataGridView |
| Viền nhẹ | `BorderLight` | `#E5E7EB` | Viền panel, đường kẻ bảng |
| Chữ chính | `TextPrimary` | `#1F2937` | Tiêu đề, nội dung chính |
| Chữ phụ | `TextSecondary` | `#6B7280` | Ghi chú, label phụ, placeholder |
| Sidebar chữ thường | `SidebarText` | `#D1FAE5` (trên nền xanh đậm) | Menu item chưa chọn |
| Sidebar chữ active | `SidebarTextActive` | `#FFFFFF` trên nền `#087A38` hoặc `#FF8A00` (thanh chỉ báo bên trái) | Menu item đang chọn |

> Có thể đổi `PrimaryGreen` sang xanh dương đậm `#0B5FFF` + cam nếu muốn phong cách "Circle K" thay vì "Bách Hóa Xanh" — giữ nguyên cấu trúc bảng, chỉ đổi mã màu.

---

## 2. Typography (Font chữ)

- Font chính: **"Inter"** hoặc **"Be Vietnam Pro"** (hỗ trợ tiếng Việt tốt, hiện đại) — nếu không nhúng được font ngoài, dùng **"Segoe UI Semibold/Regular"** có sẵn trên Windows làm phương án dự phòng.
- Phân cấp cỡ chữ:

| Cấp | Font/Size/Style | Dùng cho |
|---|---|---|
| H1 | 20px, Bold | Tiêu đề trang (VD: "QUẢN LÝ SẢN PHẨM") |
| H2 | 15px, Semibold | Tiêu đề khối/card |
| Body | 11–12px, Regular | Nội dung bảng, form nhập liệu |
| Caption | 10px, Regular, màu `TextSecondary` | Ghi chú, subtitle dưới số liệu |
| Số liệu lớn (KPI) | 24–28px, Bold | Các ô "Doanh thu hôm nay", "Tổng đơn hàng"... |

**Cách áp dụng trong code:**
```csharp
private readonly Font FontH1 = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
private readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
private readonly Font FontKpi = new Font("Segoe UI", 20F, FontStyle.Bold);
```
Nếu dùng thư viện UI mới (mục 3) thì hầu hết đã có theme font riêng — set 1 lần ở `Program.cs` cho toàn app.

---

## 3. Thư viện UI đề xuất cho WinForms (chọn 1)

| Thư viện | Ưu điểm | Ghi chú cài đặt |
|---|---|---|
| **Guna.UI2.WinForms** ⭐ khuyến nghị | Miễn phí, nhẹ, có sẵn: Guna2Panel bo góc, Guna2Button gradient, Guna2TextBox có icon/placeholder, Guna2DataGridView đẹp, Guna2SidePanel làm sidebar menu rất hợp POS | `Install-Package Guna.UI2.WinForms` (NuGet) |
| **SunnyUI** | Bộ control rất đầy đủ, có sẵn theme sáng/tối, UICard, UIStatisticSlider hợp làm Dashboard KPI | `Install-Package SunnyUI` |
| **MaterialSkin.2** | Phong cách Material Design phẳng, đơn giản, nhẹ | `Install-Package MaterialSkin.2` |
| **DevExpress WinForms** (trả phí) | Chuyên nghiệp nhất, DataGrid mạnh, nếu công ty có license thì chọn cái này | Theo license công ty |

> Trong hướng dẫn dưới đây mình dùng **Guna.UI2.WinForms** làm ví dụ chính vì free, phổ biến, dễ AI-agent áp dụng tự động (đổi control cũ sang Guna2 tương ứng: `Button` → `Guna2Button`, `Panel` → `Guna2Panel`, `TextBox` → `Guna2TextBox`, `DataGridView` → `Guna2DataGridView`, `ComboBox` → `Guna2ComboBox`).

### Cách Agent nên thực hiện thay thế control:
1. Cài package qua NuGet vào project (`.csproj` thêm `PackageReference`).
2. Trong mỗi Form Designer (`.Designer.cs`), thay khai báo control cũ bằng control Guna2 tương ứng, giữ nguyên **tên biến (field name)** để không phải sửa code xử lý sự kiện phía `.cs`.
3. Set các property chuẩn hoá (xem mục 5 bên dưới) cho từng loại control.
4. Build lại, kiểm tra sự kiện (Click, TextChanged, SelectedIndexChanged...) vẫn hoạt động bình thường.

---

## 4. Cấu trúc layout chuẩn (áp dụng cho MỌI Form)

```
┌─────────────────────────────────────────────────────────────┐
│  TOP BAR (Height 60px, màu PrimaryGreen)                     │
│  [Logo/Icon] Smart SuperMarket | ENTERPRISE ERP   [🔔][User] │
├───────────────┬─────────────────────────────────────────────┤
│               │  BREADCRUMB / TIÊU ĐỀ TRANG (H1)             │
│  SIDEBAR      │  ─────────────────────────────────────────  │
│  (240px,      │                                              │
│  nền          │  NỘI DUNG TRANG                              │
│  #087A38      │  (Card/Panel bo góc 12px, shadow nhẹ,        │
│  hoặc         │   padding 16-24px, spacing giữa card 16px)   │
│  #1F2937      │                                              │
│  tối màu)     │                                              │
│               │                                              │
│  [Menu items  │                                              │
│  có icon +    │                                              │
│  text, item   │                                              │
│  active có    │                                              │
│  thanh cam    │                                              │
│  bên trái]    │                                              │
└───────────────┴─────────────────────────────────────────────┘
```

**Chi tiết Sidebar:**
- Rộng cố định 240–260px, có thể thu gọn (collapse) còn icon-only 64px khi bấm nút toggle (tuỳ chọn nâng cao).
- Mỗi menu item cao 44–48px, icon bên trái (dùng `Guna2Panel` + `PictureBox` icon SVG/PNG, hoặc font-icon).
- Item đang active: nền sáng hơn nền sidebar (`#0D9C4A` trên nền `#087A38`) + thanh dọc màu cam 4px bên trái + chữ trắng đậm.
- Item không active: chữ `#D1FAE5`, hover đổi nền nhẹ.

**Chi tiết Top bar:**
- Bên trái: icon giỏ hàng + "Smart SuperMarket | ENTERPRISE ERP POS CONTROL CENTER".
- Bên phải: trạng thái server (chấm tròn xanh "Ready (Active)" bo góc pill), icon chuông thông báo có badge số đỏ, avatar/tên người dùng.

---

## 5. Chuẩn hoá từng loại control

### 5.1 Card thống kê (KPI Card) — dùng cho Dashboard
- `Guna2Panel`, `BorderRadius = 12`, `FillColor = SurfaceWhite`, có `Guna2ShadowForm` hoặc custom shadow nhẹ.
- Bên trong: icon tròn màu theo ngữ cảnh (xanh/cam/đỏ) ở góc trên trái, số liệu lớn (`FontKpi`) ở giữa, label mô tả nhỏ phía trên, dòng phụ (%, xu hướng) phía dưới màu Success/Danger kèm icon mũi tên ▲▼.
- 4 card ví dụ trong Dashboard: "Doanh thu hôm nay" (xanh), "Tổng đơn hàng POS" (xanh dương), "Khách hàng mới VIP" (cam), "Cảnh báo đỏ HSD" (đỏ, có icon ⚠).

### 5.2 Bảng dữ liệu (DataGridView → Guna2DataGridView)
- Header: nền `PrimaryGreenDark`, chữ trắng, Bold, không viền dày.
- Hàng dữ liệu: nền trắng xen kẽ `#F9FAFB` (zebra stripe) để dễ đọc.
- Chiều cao hàng: 40–44px (thoáng hơn mặc định 22px).
- Bỏ đường viền ô lưới đậm mặc định, chỉ để đường kẻ ngang mảnh `BorderLight`.
- Cột trạng thái (VD: "Đang bán", "Ngừng bán", "Hết hàng", "Xanh/Cam/Đỏ") → hiển thị dạng **Badge/Pill bo tròn** có màu nền nhạt + chữ đậm cùng tông (Success/Warning/Danger) thay vì chữ thường:
  - "Đang bán" / "Hoạt động" / "Active" → nền `#DCFCE7`, chữ `#16A34A`
  - "Ngừng bán" / "Tạm dừng" → nền `#FEE2E2`, chữ `#DC2626`
  - "Sắp hết hạn" / "Cận date" / "Preparing" → nền `#FEF3C7`, chữ `#F59E0B`
- Con trỏ hover đổi màu nền hàng nhẹ `PrimaryGreenLight`.
- Hàng chọn (selected row): nền `PrimaryGreenLight`, không dùng xanh dương mặc định của Windows.

### 5.3 Nút bấm (Button → Guna2Button)
- Nút chính (Lưu, Tạo, Thanh toán): `FillColor = PrimaryGreen`, chữ trắng Bold, `BorderRadius = 8`, có icon trái (Guna2 hỗ trợ `ImageAlign`), hiệu ứng hover đổi sang `PrimaryGreenDark`.
- Nút phụ/outline (Huỷ, Đóng): nền trắng, viền `BorderLight`, chữ `TextPrimary`.
- Nút nguy hiểm (Xoá): `FillColor = DangerRed` nhạt hoặc outline đỏ.
- Nút "Thanh toán" trong màn POS: to hơn hẳn (Height ≥ 50px), màu cam `AccentOrange` hoặc xanh đậm, chữ lớn, đặt cố định góc dưới phải.

### 5.4 Ô nhập liệu (TextBox/ComboBox → Guna2TextBox/Guna2ComboBox)
- `BorderRadius = 8`, `BorderColor = BorderLight`, khi Focus đổi `FocusedColor = PrimaryGreen`.
- Có `PlaceholderText` rõ ràng (VD: "Tìm theo Tên sản phẩm hoặc Barcode...").
- Ô tìm kiếm có icon kính lúp bên trái (`Guna2TextBox.IconLeft`).
- Chiều cao chuẩn 36–40px, đủ thoáng để bấm bằng tay trên màn hình cảm ứng (thu ngân hay dùng touch-screen POS).

### 5.5 Màn hình Bán hàng POS (quan trọng nhất — ưu tiên làm đẹp)
- Chia 2 cột: **Trái** (70%) = ô quét mã vạch to + bảng giỏ hàng; **Phải** (30%) = panel tổng kết đơn hàng cố định (sticky), gồm: Tạm tính, Giảm giá, VAT, **Tổng thanh toán** (chữ rất lớn, màu nổi bật), các nút phương thức thanh toán (Tiền mặt/Thẻ/QR) dạng nút lớn có icon, nút "THANH TOÁN" to màu nổi bật nhất màn hình.
- Ô quét mã vạch: to, nổi bật, tự động focus, font số lớn dễ nhìn dưới ánh đèn siêu thị.
- Dòng "TỔNG THANH TOÁN" hiện tại đang bị cắt/tràn ra ngoài Form (thấy trong ảnh) → cần đảm bảo panel tổng kết **luôn đủ rộng, không bị cắt chữ**, dùng `Dock = Right` hoặc `Anchor` đúng cách, không hardcode toạ độ.

### 5.6 Biểu đồ (Dashboard, Báo cáo)
- Nếu đang dùng `Chart` control mặc định của WinForms → tuỳ biến: bỏ nền ô lưới đậm, dùng màu line theo `PrimaryGreen`/`InfoBlue`, thêm vùng tô gradient nhẹ dưới đường (area fill opacity 15-20%) như trong ảnh Dashboard hiện tại (đã khá ổn, chỉ cần đồng bộ màu theo palette mới và làm mảnh nét lưới nền).
- Có thể nâng cấp bằng thư viện `LiveCharts2` hoặc `ScottPlot` nếu muốn đẹp hơn nữa (tuỳ độ phức tạp cho phép của agent).

### 5.7 Menu Sidebar — icon
- Thay icon chữ/emoji hiện tại bằng icon set nhất quán (gợi ý: **Font Awesome** hoặc **Material Icons** dạng PNG/SVG 20x20px, cùng style outline hoặc cùng style filled — không trộn lẫn 2 style).
- Icon + label cùng màu, căn giữa theo chiều dọc trong item 44px.

---

## 6. Áp dụng riêng cho từng màn hình đã có (theo ảnh chụp)

| Màn hình | Việc cần làm cụ thể |
|---|---|
| **Dashboard Tổng Quan** | Chuẩn hoá 4 KPI card theo mục 5.1; đồng bộ màu biểu đồ theo palette; khối "AI giải trình" đổi thành card có icon 🤖 nổi bật, nền `PrimaryGreenLight`, viền trái 4px màu xanh đậm để nổi bật là insight AI. |
| **Sản phẩm (Products)** | Bảng theo mục 5.2; cột "Trạng Thái" và cột "%" lợi nhuận dùng badge màu; thanh filter phía trên gom vào 1 hàng có khoảng cách đều, nút "Thêm Sản Phẩm" nổi bật màu xanh, nút "Quét" có icon barcode. |
| **Danh Mục (Category)** | Cây danh mục bên trái style lại giống TreeView hiện đại (icon folder, indent rõ, hover highlight xanh nhạt); form chi tiết bên phải bọc trong Card bo góc; 3 nút dưới cùng phân biệt rõ Lưu (xanh)/Thêm (xanh lá đậm hoặc outline)/Xoá (đỏ). |
| **Nhà Cung Cấp** | Bảng + cột "Đánh Giá (Rating)" hiển thị sao ⭐ màu vàng cam thay vì text; cột Trạng thái dùng badge. |
| **Nhập Hàng Kho** | Card chọn "Nhà cung cấp" + "Mã HD Nhập" gọn trong 1 hàng phía trên; bảng danh sách hàng nhập rõ ràng, cột "Hạn Sử" cận hạn tô màu cảnh báo; nút "Tạo Đơn Nhập Hàng" nổi bật góc phải. |
| **Quản Lý Tồn Kho** | Cột "Trạng Thái" (Xanh/Cam/Đỏ) hiện đang là text màu — chuyển hẳn sang **badge màu** tương ứng Success/Warning/Danger cho dễ quét mắt; filter kho + tìm kiếm gộp 1 hàng gọn. |
| **Bán Hàng POS** | Ưu tiên cao nhất — làm theo mục 5.5 chi tiết ở trên; sửa lỗi tràn UI của khối "TỔNG THANH TOÁN". |
| **Quản Lý Đơn Hàng** | Cột "Tiến Trình Đơn Hàng (Timeline)" hiện là text ("Completed", "Shipping"...) → đổi thành badge màu + icon nhỏ (✔️ hoàn thành, 🚚 đang giao, 📦 đang đóng, 🕐 chờ xác nhận). |
| **Khách Hàng Loyalty** | 4 ô thống kê đầu trang style như KPI card (mục 5.1); cột "Hạng Thành Viên" (Silver/Gold/VIP) hiển thị badge màu tương ứng (Silver = xám bạc, Gold = vàng, VIP = tím/đỏ). |
| **Chương Trình Khuyến Mãi** | Bảng voucher: cột "Trạng Thái" ("Đang diễn ra"/"Sắp diễn ra") dùng badge xanh/cam; có thể thêm each voucher dạng "thẻ vé" (ticket-style card) thay vì chỉ bảng thuần, nếu agent có thời gian nâng cấp thêm. |
| **Trợ Lý AI Assistant** | Giao diện dạng chat: khối trả lời của AI bọc trong bubble bo góc nền `PrimaryGreenLight`, icon robot 🤖 bên trái mỗi câu trả lời; ô nhập câu hỏi phía dưới to, có icon gửi (➤) bên phải, luôn dính đáy màn hình (Dock Bottom). |
| **Báo Cáo Enterprise** | Đồng bộ màu 2 đường biểu đồ (Doanh thu / Lợi nhuận gộp) theo `PrimaryGreen` và `AccentOrange`; thêm card tổng số tuần/tháng phía trên biểu đồ nếu có dữ liệu. |
| **Nhân Viên & Ca Trực** | Bảng nhân viên: cột "Trạng Thái" (Active) dùng badge xanh; avatar tròn nhỏ trước tên nhân viên nếu có ảnh; tab "Danh Sách/Ca Trực/Chấm Công" style lại thành tab pill (bo tròn, tab active nền xanh chữ trắng) thay vì tab Windows mặc định. |
| **Cấu Hình System** | Style tab con giống trên; nội dung cấu hình trình bày dạng form 2 cột (label trái, giá trị/input phải) trong Card, không để chữ nổi trần trên nền trắng như hiện tại. |

---

## 7. Thứ tự thực hiện đề xuất cho Agent (Roadmap)

```
Phase 0 — Chuẩn bị
  [ ] Cài NuGet package Guna.UI2.WinForms (hoặc SunnyUI) vào project
  [ ] Tạo file constant màu sắc dùng chung: /Common/AppTheme.cs (chứa các mã màu ở mục 1)
  [ ] Set font mặc định toàn app trong Program.cs / MainForm load

Phase 1 — Khung sườn chung
  [ ] Chuẩn hoá lại MainForm: Top bar + Sidebar theo mục 4
  [ ] Style lại toàn bộ menu item sidebar (icon, active state, hover)

Phase 2 — Form quan trọng nhất trước
  [ ] Bán Hàng POS (mục 5.5) — ưu tiên số 1, fix lỗi tràn giao diện
  [ ] Dashboard Tổng Quan (mục 5.1)

Phase 3 — Các Form quản lý dữ liệu (áp dụng chung 1 pattern)
  [ ] Sản Phẩm, Danh Mục, Nhà Cung Cấp, Nhập Hàng Kho, Tồn Kho,
      Đơn Hàng, Khách Hàng Loyalty, Khuyến Mãi, Nhân Viên
  → Áp dụng đồng loạt: DataGridView theo mục 5.2, Button theo 5.3,
    TextBox/ComboBox theo 5.4

Phase 4 — Các Form phụ
  [ ] Trợ Lý AI Assistant (dạng chat)
  [ ] Báo Cáo Enterprise (biểu đồ)
  [ ] Cấu Hình System

Phase 5 — Kiểm thử
  [ ] Build lại toàn bộ solution, chạy thử từng màn hình
  [ ] Kiểm tra không có control nào bị lệch/tràn khi resize cửa sổ
  [ ] Kiểm tra tất cả sự kiện Click/DataBinding cũ vẫn hoạt động đúng
```

---

## 8. Ghi chú thêm cho Agent

- Nếu project dùng WinForms .NET Framework cũ (không phải .NET 6+), vẫn cài được `Guna.UI2.WinForms` bình thường qua NuGet, không cần nâng cấp Framework.
- Không bắt buộc phải đổi 100% control cùng lúc — có thể làm dần theo Roadmap ở mục 7, mỗi lần 1 Form, commit git riêng để dễ rollback nếu lỗi.
- Luôn giữ bản backup `.Designer.cs` gốc trước khi agent tự động sinh lại code designer, tránh mất control ẩn không thấy trên giao diện.
- Có thể inject file `AppTheme.cs` này vào các Form qua 1 base class chung (`BaseForm : Form`) để áp style tự động (BackColor, Font mặc định) cho mọi Form mới tạo sau này, giảm lặp code.

```csharp
// Ví dụ /Common/AppTheme.cs
public static class AppTheme
{
    public static readonly Color PrimaryGreen = ColorTranslator.FromHtml("#0D9C4A");
    public static readonly Color PrimaryGreenDark = ColorTranslator.FromHtml("#087A38");
    public static readonly Color PrimaryGreenLight = ColorTranslator.FromHtml("#E6F7EC");
    public static readonly Color AccentOrange = ColorTranslator.FromHtml("#FF8A00");
    public static readonly Color InfoBlue = ColorTranslator.FromHtml("#2D7FF9");
    public static readonly Color SuccessGreen = ColorTranslator.FromHtml("#16A34A");
    public static readonly Color WarningAmber = ColorTranslator.FromHtml("#F59E0B");
    public static readonly Color DangerRed = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color BackgroundGray = ColorTranslator.FromHtml("#F4F6F8");
    public static readonly Color SurfaceWhite = ColorTranslator.FromHtml("#FFFFFF");
    public static readonly Color BorderLight = ColorTranslator.FromHtml("#E5E7EB");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1F2937");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#6B7280");

    public static readonly Font FontH1 = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
    public static readonly Font FontH2 = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
    public static readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font FontKpi = new Font("Segoe UI", 20F, FontStyle.Bold);
}
```

---

**Tóm lại:** đưa nguyên file `.md` này cho agent, yêu cầu agent đọc mục 7 (Roadmap) và thực hiện tuần tự từng Phase, build + test sau mỗi Form, ưu tiên màn hình **Bán Hàng POS** và **Dashboard** trước vì đây là 2 màn hình khách hàng/nhân viên nhìn thấy nhiều nhất.
