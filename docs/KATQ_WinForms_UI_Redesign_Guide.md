# 🎨 KATQ Smart Workstation — WinForms UI Redesign Guide
> Design System: **Microsoft Fluent 2** | Target: ERP/POS Enterprise Desktop App
> Version: 1.0 | Ngày: 2026-09-14

---

## PHẦN 0 — DESIGN TOKENS (chuẩn dùng cho cả 3 hướng)

Mọi code/thiết kế dưới đây đều dựa trên bộ token này. **Agent phải tuân thủ tuyệt đối**, không hardcode màu lạc.

### 0.1. Bảng màu (Fluent 2 palette)

| Token | Hex | Dùng cho |
|---|---|---|
| `--primary` | `#0F6CBD` | Nút primary, link, accent |
| `--primary-hover` | `#115EA3` | Hover nút primary |
| `--primary-pressed` | `#0C3B5E` | Pressed |
| `--primary-subtle` | `#EBF3FC` | Nền icon KPI card, selected row nhẹ |
| `--success` | `#107C10` | Trạng thái OK, tồn kho đủ |
| `--success-subtle` | `#DFF6DD` | Badge success |
| `--warning` | `#F7630C` | Cảnh báo HSD, sắp hết hàng |
| `--warning-subtle` | `#FFF4E5` | Badge warning |
| `--danger` | `#C42B1C` | Xóa, hủy đơn, lỗi |
| `--danger-subtle` | `#FDE7E9` | Badge danger |
| `--sidebar-bg` | `#1B1F23` | Nền sidebar (đen xanh, không dùng xanh đậm #003366) |
| `--sidebar-active` | `#0F6CBD` + bar 3px `#5EB4F2` | Item menu đang chọn |
| `--sidebar-text` | `#D6D6D6` | Text menu |
| `--bg-page` | `#F5F5F5` | Nền vùng nội dung |
| `--surface` | `#FFFFFF` | Card, panel |
| `--border` | `#E0E0E0` | Viền card, input |
| `--text-primary` | `#242424` | Text chính |
| `--text-secondary` | `#616161` | Label phụ, caption |
| `--text-disabled` | `#BDBDBD` | Disabled |

**Quy tắc vàng:** Xanh đậm `#0F6CBD` chỉ xuất hiện ở nút primary + accent — KHÔNG tô nền header trang, KHÔNG tô header bảng xanh đậm (đổi sang nền xám `#F3F2F1`).

### 0.2. Typography

| Vai trò | Font | Size | Weight | Color |
|---|---|---|---|---|
| H1 — tiêu đề trang | Segoe UI | 20pt | SemiBold | text-primary |
| H2 — tiêu đề section/card | Segoe UI | 14pt | SemiBold | text-primary |
| H3 — tiêu đề bảng/cột | Segoe UI | 11pt | SemiBold | text-primary |
| Body / ô nhập liệu | Segoe UI | 10pt | Regular | text-primary |
| Label form | Segoe UI | 10pt | Regular | text-secondary |
| Số liệu KPI | Segoe UI | 24pt | Bold | text-primary |
| Text bảng (cell) | Segoe UI | 10pt | Regular | text-primary |

Font family đăng ký:
```csharp
// Program.cs hoặc App start
Application.DefaultFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
```

### 0.3. Spacing & Shape

| Token | Giá trị | Dùng cho |
|---|---|---|
| Space-1 | 4px | Gap icon-text |
| Space-2 | 8px | Padding trong nhỏ, gap control |
| Space-3 | 12px | Padding input, gap nhóm |
| Space-4 | 16px | Padding card, margin section |
| Space-6 | 24px | Padding trang, khoảng cách section lớn |
| Radius-S | 4px | Button, input, badge |
| Radius-M | 8px | Card, panel |
| Control height | 32px | Button, TextBox, ComboBox chuẩn |
| Row height bảng | 36px | DataGridView |
| Card shadow | offset Y=2, blur 8, alpha 15% | Card nổi |

---

# HƯỚNG A — TỰ VIẾT UI (Custom Paint, không thư viện)

✅ Ưu: không phụ thuộc third-party, không bị antivirus flag, full control, dễ maintain bán phần mềm.
❌ Nhược: tốn công viết hơn.

### A.1. Theme.cs — trung tâm toàn bộ màu sắc

```csharp
using System.Drawing;

namespace KatqSmart.UI.Themes
{
    /// <summary>
    /// Fluent 2 Design Tokens. Toàn bộ app CHỈ lấy màu từ đây.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Primary        = Color.FromArgb(0x0F, 0x6C, 0xBD);
        public static readonly Color PrimaryHover   = Color.FromArgb(0x11, 0x5E, 0xA3);
        public static readonly Color PrimaryPressed = Color.FromArgb(0x0C, 0x3B, 0x5E);
        public static readonly Color PrimarySubtle  = Color.FromArgb(0xEB, 0xF3, 0xFC);

        public static readonly Color Success        = Color.FromArgb(0x10, 0x7C, 0x10);
        public static readonly Color SuccessSubtle  = Color.FromArgb(0xDF, 0xF6, 0xDD);
        public static readonly Color Warning        = Color.FromArgb(0xF7, 0x63, 0x0C);
        public static readonly Color WarningSubtle  = Color.FromArgb(0xFF, 0xF4, 0xE5);
        public static readonly Color Danger         = Color.FromArgb(0xC4, 0x2B, 0x1C);
        public static readonly Color DangerSubtle   = Color.FromArgb(0xFD, 0xE7, 0xE9);

        public static readonly Color SidebarBg      = Color.FromArgb(0x1B, 0x1F, 0x23);
        public static readonly Color SidebarHover   = Color.FromArgb(0x2D, 0x33, 0x38);
        public static readonly Color SidebarActive  = Color.FromArgb(0x0F, 0x6C, 0xBD);
        public static readonly Color SidebarAccent  = Color.FromArgb(0x5E, 0xB4, 0xF2);
        public static readonly Color SidebarText    = Color.FromArgb(0xD6, 0xD6, 0xD6);

        public static readonly Color BgPage         = Color.FromArgb(0xF5, 0xF5, 0xF5);
        public static readonly Color Surface        = Color.White;
        public static readonly Color Border         = Color.FromArgb(0xE0, 0xE0, 0xE0);
        public static readonly Color GridHeaderBg   = Color.FromArgb(0xF3, 0xF2, 0xF1);

        public static readonly Color TextPrimary    = Color.FromArgb(0x24, 0x24, 0x24);
        public static readonly Color TextSecondary  = Color.FromArgb(0x61, 0x61, 0x61);
        public static readonly Color TextDisabled   = Color.FromArgb(0xBD, 0xBD, 0xBD);

        public static class Fonts
        {
            public static readonly Font H1       = new Font("Segoe UI", 20F, FontStyle.Bold);
            public static readonly Font H2       = new Font("Segoe UI", 14F, FontStyle.Bold);
            public static readonly Font H3       = new Font("Segoe UI", 11F, FontStyle.Bold);
            public static readonly Font Body     = new Font("Segoe UI", 10F);
            public static readonly Font Label    = new Font("Segoe UI", 10F);
            public static readonly Font KpiValue = new Font("Segoe UI", 24F, FontStyle.Bold);
            public static readonly Font KpiLabel = new Font("Segoe UI", 9.5F);
        }
    }
}
```

### A.2. RoundedPanel.cs — Card bo góc + shadow

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KatqSmart.UI.Controls
{
    public class RoundedPanel : Panel
    {
        [System.ComponentModel.DefaultValue(8)]
        public int Radius { get; set; } = 8;

        public RoundedPanel()
        {
            BackColor = Themes.Theme.Surface;
            Padding = new Padding(16);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Shadow nhẹ (vẽ trước)
            using (var shadowPath = GetRoundPath(new Rectangle(0, 2, Width - 1, Height - 3), Radius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(25, 0, 0, 0)))
                e.Graphics.FillPath(shadowBrush, shadowPath);

            // Nền card
            using (var path = GetRoundPath(new Rectangle(0, 0, Width - 3, Height - 3), Radius))
            using (var brush = new SolidBrush(BackColor))
                e.Graphics.FillPath(brush, path);

            // Viền
            using (var path = GetRoundPath(new Rectangle(0, 0, Width - 4, Height - 4), Radius))
            using (var pen = new Pen(Themes.Theme.Border))
                e.Graphics.DrawPath(pen, path);

            base.OnPaint(e);
        }

        private static GraphicsPath GetRoundPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
```

### A.3. ModernButton.cs — Nút bo góc, hover/pressed, màu theo loại

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KatqSmart.UI.Controls
{
    public enum BtnKind { Primary, Secondary, Danger, Ghost }

    public class ModernButton : Button
    {
        public BtnKind Kind { get; set; } = BtnKind.Secondary;
        private bool _hover;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(120, 32);          // chuẩn Fluent height
            Font = Themes.Theme.Fonts.Body;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(System.EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private Color BaseColor => Kind switch
        {
            BtnKind.Primary   => _hover ? Themes.Theme.PrimaryHover : Themes.Theme.Primary,
            BtnKind.Danger    => _hover ? Color.FromArgb(0xA4, 0x26, 0x1C) : Themes.Theme.Danger,
            BtnKind.Secondary => _hover ? Color.FromArgb(0xFA, 0xFA, 0xFA) : Color.White,
            _                 => Color.Transparent
        };

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundRect(rect, 4))
            using (var brush = new SolidBrush(Enabled ? BaseColor : Themes.Theme.TextDisabled))
                g.FillPath(brush, path);

            // Viền nút secondary/ghost
            if (Kind is BtnKind.Secondary or BtnKind.Ghost)
                using (var path = RoundRect(rect, 4))
                using (var pen = new Pen(Themes.Theme.Border))
                    g.DrawPath(pen, path);

            // Text căn giữa
            TextRenderer.DrawText(g, Text, Font, rect,
                Kind == BtnKind.Ghost ? Themes.Theme.Primary : Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
```

### A.4. GridStyler.cs — "Fluent hóa" DataGridView có sẵn

```csharp
using System.Windows.Forms;

namespace KatqSmart.UI.Controls
{
    public static class GridStyler
    {
        public static void Apply(DataGridView grid)
        {
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Themes.Theme.Surface;
            grid.EnableHeadersVisualStyles = false;           // QUAN TRỌNG: cho phép custom header

            // Header: nền xám nhạt, chữ đậm — KHÔNG tô xanh đậm
            grid.ColumnHeadersDefaultCellStyle.BackColor = Themes.Theme.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Themes.Theme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = Themes.Theme.Fonts.H3;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Themes.Theme.GridHeaderBg;
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            // Row
            grid.RowTemplate.Height = 36;
            grid.DefaultCellStyle.Font = Themes.Theme.Fonts.Body;
            grid.DefaultCellStyle.ForeColor = Themes.Theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Themes.Theme.PrimarySubtle;
            grid.DefaultCellStyle.SelectionForeColor = Themes.Theme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFA, 0xFA);

            // Gridline mảnh màu nhạt
            grid.GridColor = Themes.Theme.Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        }
    }
}
```

### A.5. SidebarMenu.cs — menu dọc tối màu có accent bar

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace KatqSmart.UI.Controls
{
    public class SidebarMenu : FlowLayoutPanel
    {
        public event Action<string>? ItemClicked;
        private SidebarItem? _active;

        public SidebarMenu()
        {
            FlowDirection = FlowDirection.TopDown;
            WrapContents = false;
            AutoScroll = true;
            BackColor = Themes.Theme.SidebarBg;
            Width = 220;
            Padding = new Padding(0, 8, 0, 8);
        }

        public SidebarItem AddItem(string id, string text, Image? icon = null)
        {
            var item = new SidebarItem(id, text, icon) { Width = this.Width - 16, Margin = new Padding(8, 2, 8, 2) };
            item.Click += (s, e) => SetActive(item);
            Controls.Add(item);
            return item;
        }

        private void SetActive(SidebarItem item)
        {
            if (_active != null) _active.IsActive = false;
            _active = item;
            item.IsActive = true;
            ItemClicked?.Invoke(item.ItemId);
        }
    }

    public class SidebarItem : Control
    {
        public string ItemId { get; }
        public bool IsActive { get; set; }

        private readonly Image? _icon;
        private bool _hover;

        public SidebarItem(string id, string text, Image? icon)
        {
            ItemId = id; _icon = icon;
            Text = text; Height = 40;
            Font = new Font("Segoe UI", 10F);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bg = IsActive ? Themes.Theme.SidebarActive
                  : _hover    ? Themes.Theme.SidebarHover
                  : Themes.Theme.SidebarBg;
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, ClientRectangle);

            // Accent bar 3px bên trái khi active
            if (IsActive)
                using (var brush = new SolidBrush(Themes.Theme.SidebarAccent))
                    g.FillRectangle(brush, new Rectangle(0, 8, 3, Height - 16));

            var textColor = IsActive ? Color.White : Themes.Theme.SidebarText;
            var iconSize = 16;
            var iconRect = new Rectangle(16, (Height - iconSize) / 2, iconSize, iconSize);
            if (_icon != null) g.DrawImage(_icon, iconRect);

            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(44, 0, Width - 48, Height), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
```

### A.6. Màn hình mẫu hoàn chỉnh — FormBaoCao.cs (layout khung sườn)

```csharp
using System.Drawing;
using System.Windows.Forms;
using KatqSmart.UI.Controls;
using KatqSmart.UI.Themes;

namespace KatqSmart.UI.Screens
{
    /// <summary>
    /// Khung sườn mọi màn hình nội dung: Header trang + vùng card nội dung.
    /// Màn hình khác chỉ kế thừa và đổ phần body.
    /// </summary>
    public class ScreenBase : UserControl
    {
        protected readonly Label LblTitle = new();
        protected readonly Panel Body = new() { Dock = DockStyle.Fill, BackColor = Theme.BgPage, Padding = new Padding(24) };

        public ScreenBase(string title)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgPage;

            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 64, BackColor = Theme.BgPage,
                Padding = new Padding(24, 16, 24, 8)
            };
            LblTitle.Text = title;
            LblTitle.Font = Theme.Fonts.H1;
            LblTitle.ForeColor = Theme.TextPrimary;
            LblTitle.AutoSize = true;
            header.Controls.Add(LblTitle);

            Controls.Add(Body);
            Controls.Add(header);
        }
    }

    public class ScreenBaoCao : ScreenBase
    {
        public ScreenBaoCao() : base("Báo cáo phân tích doanh thu")
        {
            // KPI cards — hàng trên
            var kpiRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 108, BackColor = Theme.BgPage,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            kpiRow.Controls.Add(MakeKpiCard("Doanh thu hôm nay", "0 đ", Theme.PrimarySubtle, Theme.Primary));
            kpiRow.Controls.Add(MakeKpiCard("Tổng đơn hàng POS", "0 đơn", Theme.SuccessSubtle, Theme.Success));
            kpiRow.Controls.Add(MakeKpiCard("Khách hàng loyalty", "0 thành viên", Theme.PrimarySubtle, Theme.Primary));
            kpiRow.Controls.Add(MakeKpiCard("Cảnh báo kho HSD", "0 mặt hàng", Theme.WarningSubtle, Theme.Warning));

            // Card biểu đồ — dưới
            var chartCard = new RoundedPanel
            {
                Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0)
            };
            var lblChart = new Label
            {
                Text = "Biểu đồ doanh thu thực tế (7 ngày gần nhất)",
                Font = Theme.Fonts.H2, ForeColor = Theme.TextPrimary, AutoSize = true, Dock = DockStyle.Top
            };
            var chartHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(0, 12, 0, 0) };
            chartHost.Controls.Add(new FormsPlotPlaceholder()); // thay bằng ScottPlot/Chart control
            chartCard.Controls.Add(chartHost);
            chartCard.Controls.Add(lblChart);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(kpiRow, 0, 0);
            layout.Controls.Add(chartCard, 0, 1);

            Body.Controls.Add(layout);
        }

        private static RoundedPanel MakeKpiCard(string label, string value, Color iconBg, Color accent)
        {
            var card = new RoundedPanel { Size = new Size(280, 92), Margin = new Padding(0, 0, 16, 0) };

            var icon = new Panel
            {
                Size = new Size(40, 40), Location = new Point(16, 26), BackColor = iconBg
            };
            // vẽ icon 16px màu accent vào panel này (override Paint của bạn)

            var lblValue = new Label
            {
                Text = value, Font = Theme.Fonts.KpiValue, ForeColor = Theme.TextPrimary,
                AutoSize = true, Location = new Point(68, 18)
            };
            var lblLabel = new Label
            {
                Text = label, Font = Theme.Fonts.KpiLabel, ForeColor = Theme.TextSecondary,
                AutoSize = true, Location = new Point(68, 56)
            };

            card.Controls.Add(icon);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblLabel);
            return card;
        }

        private class FormsPlotPlaceholder : Control { } // thay bằng ScottPlot.FormsPlot
    }
}
```

### A.7. Màn hình Login mẫu

```csharp
public class FormLogin : Form
{
    public FormLogin()
    {
        // Form nền gradient nhẹ
        var bg = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgPage };

        var card = new RoundedPanel
        {
            Size = new Size(400, 460),
            Anchor = AnchorStyles.None
        };

        var lblTitle = new Label
        {
            Text = "Đăng nhập hệ thống", Font = Theme.Fonts.H2,
            ForeColor = Theme.TextPrimary, AutoSize = true,
            Location = new Point(24, 24)
        };

        var txtUser = MakeInput("Tên đăng nhập", 24, 90);
        var txtPass = MakeInput("Mật khẩu", 24, 150, isPassword: true);

        var btnLogin = new ModernButton
        {
            Kind = BtnKind.Primary, Text = "Đăng nhập",
            Location = new Point(24, 220), Size = new Size(352, 36), Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

        var btnRegister = new ModernButton
        {
            Kind = BtnKind.Secondary, Text = "Đăng ký / Phân quyền mới",
            Location = new Point(24, 268), Size = new Size(352, 36)
        };

        card.Controls.AddRange(new Control[] { lblTitle, txtUser, txtPass, btnLogin, btnRegister });
        bg.Controls.Add(card);
        card.Location = new Point((bg.Width - card.Width) / 2, (bg.Height - card.Height) / 2);

        Controls.Add(bg);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 800);
        Text = "KATQ Smart Workstation";
    }

    private static TextBox MakeInput(string placeholder, int x, int y, bool isPassword = false)
    {
        var box = new TextBox
        {
            Location = new Point(x, y), Size = new Size(352, 32),
            BorderStyle = BorderStyle.FixedSingle, Font = Theme.Fonts.Body
        };
        if (isPassword) box.UseSystemPasswordChar = true;
        // Placeholder: dùng thư viện nhỏ CueBanner hoặc tự xử lý GotFocus/LostFocus
        return box;
    }
}
```

### A.8. Icon

Dùng **Fluent UI System Icons** (github.com/microsoft/fluentui-system-icons) — bộ icon chính thức của Microsoft, free, format SVG/PNG. Kích thước dùng trong app: 16px (menu/bảng), 20px (nút lớn), 32px (KPI card).

---

# HƯỚNG B — DÙNG THƯ VIỆN UI

## B.1. So sánh nhanh

| Thư viện | Giá | Style | Ghi chú |
|---|---|---|---|
| **ReaLTaiizor** | Free | Fluent/Material/Metro | ⭐ Khuyên dùng cho project của bạn |
| **Krypton Toolkit (Standard)** | Free | Office/Fluent enterprise | Mạnh về docking, ribbon, datagrid |
| MaterialSkin 2 | Free | Google Material | Nhanh apply, hơi "Android" |
| MetroFramework | Free | Metro UI | Cũ (2013), chỉ dùng khi cần nhẹ |
| Guna.UI2 | Paid (~$100+) | Modern gradient | Đẹp, cộng đồng lớn |
| Bunifu | Paid | Modern | Đẹp, giá cao |
| Syncfusion / DevExpress | Paid (community free < 1M doanh thu) | Enterprise chuẩn | Syncfusion có Community License miễn phí nếu doanh thu < $1M/năm — đáng cân nhắc |

⚠️ **Cảnh báo bảo mật:** ReaLTaiizor/MaterialSkin/Guna thường bị Windows Defender/AV false-positive do dùng hook vẽ. Trước khi phát hành: test trên máy khách, ký code-signing certificate nếu bán phần mềm.

## B.2. Cài đặt ReaLTaiizor

```bash
dotnet add package ReaLTaiizor
```

```csharp
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Crown;

// Form kế thừa MaterialForm (Material Design)
public class MainForm : MaterialForm
{
    public MainForm()
    {
        // Bảng màu tùy chỉnh khớp với Theme tokens
        SkinManager.ColorScheme = new MaterialColorScheme(
            Primary.Blue800, Primary.Blue900, Primary.Blue500,
            Accent.LightBlue200, TextShade.WHITE);

        var drawer = new MaterialDrawer();
        // ... menu items
    }
}
```

Hoặc control lẻ không cần đổi Form base:
```csharp
var button = new MaterialButton { Type = MaterialButton.MaterialButtonType.Contained };
var textBox = new MaterialTextBox2 { Hint = "Tên sản phẩm" };
var tabPage = new CrownTabControl(); // style Fluent tab giống màn Cấu Hình / Nhân Viên
```

## B.3. Cài đặt Krypton Toolkit

```bash
dotnet add package Krypton.Toolkit
```

```csharp
using Krypton.Toolkit;

// Program.cs — bật giao diện Krypton toàn cục
var palette = new KryptonCustomPaletteBase();
// Map từng màu trong Theme.cs sang palette (chi tiết dưới)

KryptonManager.DefaultPalette = palette;
Application.Run(new MainForm());
```

Ưu điểm lớn nhất của Krypton: **KryptonDataGridView** (sort header, styling dễ hơn DataGridView thuần) và **KryptonDocking** nếu sau này làm layout dockable kiểu Visual Studio.

**Khuyến nghị thực tế cho project bạn:** dùng **Hướng A cho khung sườn** (sidebar, card, button, grid) + **ReaLTaiizor chỉ lấy control lẻ** (tab, textbox có hint) nếu muốn nhanh. Không nên chồng 2 thư viện skin cùng lúc.

## B.4. Biểu đồ (Báo cáo / Dashboard)

- **ScottPlot 5** (free, MIT): `dotnet add package ScottPlot.WinForms` — vẽ nhanh, đẹp, tài liệu tốt. ⭐ Khuyên dùng.
- **LiveCharts2** (free): đẹp, animation mượt, hỗ trợ real-time tốt cho dashboard.
- Tránh MS Chart cũ (visual lỗi thờicổ).

---

# HƯỚNG C — THIẾT KẾ CHI TIẾT TỪNG MÀN HÌNH

Quy tắc chung áp dụng mọi màn: header trang chỉ có **tiêu đề 20pt đen** (bỏ nền xanh đậm), nội dung trong card trắng bo 8px, bảng header nền xám `#F3F2F1`, nút primary xanh chỉ 1–2 cái/màn hình.

### C.1. Đăng nhập (image.png)
- Bỏ form xanh đậm toàn màn → nền `#F5F5F5`, card trắng 400px căn giữa, logo KATQ trong card.
- Input cao 36px, bo 4px, viền `#E0E0E0`, focus viền xanh `#0F6CBD`.
- Nút "Đăng nhập" primary full-width; "Đăng ký" secondary ghost.
- Dòng trạng thái server: text 9pt màu success/danger có dot tròn 8px (Fluent status badge).

### C.2. Dashboard Tổng (image1)
- KPI cards: icon trong vòng tròn 40px nền subtle, số 24pt Bold, label 9.5pt xám, viền trái màu 4px mỗi card theo semantic color (xanh/cam/đỏ/xanh lá). **Bỏ hiệu ứng "v" răng cưa** ở góc card.
- Biểu đồ đặt trong card trắng riêng, tiêu đề 14pt.
- Khối "AI giải trình": card nền `#EBF3FC` (primary subtle), text 10pt, icon robot, không viền xanh đậm.

### C.3. Sản Phẩm (image2)
- Thanh công cụ: ô tìm kiếm cao 32px bo 4px (có icon kính lúp 16px bên trái) + dropdown + nút Primary "Thêm sản phẩm" + nút Secondary "Quét".
- Bảng: header nền xám, row 36px, zebra striping, cột Trạng Thái dùng badge bo tròn (pill) nền SuccessSubtle/WarningSubtle.
- **Sửa lỗi hiện tại:** tiêu đề cột bị cắt → tăng ColumnHeadersHeight lên 38 và dùng wrap hoặc AutoSizeColumnsMode.Fill.

### C.4. Danh Mục (image3)
- Chia 2 cột 1:2 bằng TableLayoutPanel: trái cây danh mục trong card, phải chi tiết trong card.
- Cây danh mục: item cao 32px, icon folder 16px, selected nền PrimarySubtle + chữ xanh đậm.
- Form chi tiết: label trên, input dưới (stacked), khoảng cách 12px; nút Lưu Primary, Thêm Secondary, Xóa Danger (luôn có confirm dialog).

### C.5. Nhà Cung Cấp (image4)
- Giống layout Sản Phẩm: toolbar + bảng. Nút Import đặt Secondary kèm icon.
- Thêm cột "Đánh giá" render ★ bằng cell painting màu cam `#F7630C`.

### C.6. Nhập Hàng Kho (image5)
- Card chứa header phiếu: "Nhà cung cấp" dropdown + "Mã HĐ nhập" label read-only (nền xám) + nút "Tạo đơn" Primary góc phải.
- Bảng chi tiết như GridStyler; cột Hạn Sử Dụng: ngày < 30 ngày tới → chữ Warning, < 7 ngày → chữ Danger đậm.
- Footer card: dòng Tổng giá trị (thuế, chiết khấu) căn phải, font Bold 12pt.

### C.7. Quản Lý Tồn Kho (image6)
- 2 filter trên cùng hàng (dropdown kho + ô tìm kiếm).
- Cột "Trạng Thái Cảnh Báo": badge pill — "Đủ hàng" (SuccessSubtle), "Sắp hết" (WarningSubtle), "Hết hàng" (DangerSubtle).
- Thanh progress tồn kho trong cell (vẽ Bar custom trong CellPainting: % tồn = tồn / định mức).

### C.8. Bán Hàng POS (image7)
- Layout 2 cột: trái 60% giỏ hàng, phải 40% bảng tính tiền trong card tối nhẹ hoặc card trắng viền.
- Ô quét barcode: cao 40px, font 14pt, focus mặc định khi mở màn hình.
- Tổng tiền: 28pt Bold; tiền khách đưa / tiền thừa 14pt.
- Nút HOÀN THÀNH: Primary, cao 48px full-width cột phải (chỉ nút này to).
- Hình thức thanh toán: segmented control (tiền mặt / thẻ / QR) bo 4px, selected nền PrimarySubtle.

### C.9. Quản Lý Đơn Hàng (image8)
- Toolbar: Tải / Xem chi tiết (Secondary) / Hủy đơn (Danger outline).
- Bảng timeline: cột Trạng Thái badge theo trạng thái (Chờ xử lý=Warning, Hoàn thành=Success, Đã hủy=Disabled).
- Khi "Không có dữ liệu": hiện empty state giữa bảng — icon 48px xám + text 11pt "Chưa có đơn hàng nào" (bỏ text đỏ).

### C.10. Khách Hàng Loyalty (image9)
- 4 KPI cards đã đẹp về cấu trúc — đổi sang style card mới (icon tròn subtle), số liệu 24pt.
- Bảng: thêm column "Hạng thành viên" badge màu (VIP=warning vàng, Gold=amber, Member=xám).
- Nút "Thêm khách hàng" Primary góc phải header.

### C.11. Chương Trình Khuyến Mãi (image10)
- Bảng + badge "Đang diễn ra" (SuccessSubtle pill) / "Hết hạn" (Disabled).
- Loại giảm: icon % hoặc ₫ cùng text, căn giữa cell.

### C.12. Trợ Lý AI (image11)
- Chat-style: tin nhắn AI trong card PrimarySubtle bo 8px căn trái, tin người dùng căn phải.
- Input câu hỏi cao 40px bo 4px, nút gửi icon primary.
- Bullet AI Forecast/Recommendation: icon 16px + tiêu đề đậm + nội dung 10pt.

### C.13. Báo Cáo Enterprise (image12)
- Biểu đồ ScottPlot/LiveCharts trong card trắng, legend bên phải, line Primary + line Warning, gridline `#E0E0E0` mảnh.
- Thêm bộ lọc thờigian (Tuần/Tháng/Quý) segmented control phía trên phải biểu đồ.

### C.14. Nhân Viên Ca Trực (image13)
- Tabs (Danh sách / Ca trực / Chấm công): tab strip dưới header, selected có underline 2px Primary (Fluent style) thay vì tab cổ điển.
- Nút "Tạo tài khoản nhân viên" Primary.

### C.15. Cấu Hình System (image14)
- Info rows (Tên hệ thống, VAT, Chi nhánh, Endpoint): label 10pt xám + value 11pt đậm, icon 16px màu Primary.
- Trạng thái server: dot tròn xanh lá nhấp nháy + text Success.
- Tabs: Cấu hình / Audit Logs / Sao lưu — underline style như C.14.

---

# PHẦN D — PROMPT CHO AI AGENT (copy-paste)

```
Bạn là senior C# WinForms developer + UI designer theo Microsoft Fluent 2.
Dự án: KATQ Smart Workstation — ERP/POS siêu thị (WinForms .NET, C#).

NGUYÊN TẮC BẮT BUỘC:
1. Mọi màu sắc lấy từ class Theme (namespace KatqSmart.UI.Themes), CẤM hardcode hex.
2. Font chuẩn Segoe UI: H1=20 Bold, H2=14 Bold, body=10 Regular.
3. Mọi màn hình kế thừa ScreenBase (header tiêu đề + Body panel).
4. DataGridView phải gọi GridStyler.Apply().
5. Nút dùng ModernButton với đúng Kind (Primary/Secondary/Danger/Ghost).
6. Card dùng RoundedPanel (bo 8px, padding 16).
7. Bảng màu: Primary #0F6CBD, Success #107C10, Warning #F7630C, Danger #C42B1C,
   Sidebar #1B1F23, BgPage #F5F5F5, Border #E0E0E0.
8. Trạng thái dùng badge pill (bo tròn, nền *Subtle, chữ màu đậm tương ứng).
9. Khoảng cách theo hệ 4px: 4/8/12/16/24. Chiều cao control chuẩn 32px.
10. Không dùng màu xanh đậm làm nền header trang hoặc header bảng.

NHIỆM VỤ: [mô tả màn hình cần làm — ví dụ: "viết lại màn hình Quản lý tồn kho
theo mô tả C.7, có filter kho, tìm kiếm, bảng batch với badge cảnh báo HSD
và progress bar tồn kho trong cell"]

OUTPUT: file .cs hoàn chỉnh, compile được, kèm comment tiếng Việt.
```

---

# PHẦN E — CHECKLIST MIGRATION

- [ ] Tạo project `KatqSmart.UI` chứa Themes/Controls/Screens
- [ ] Chuyển toàn bộ màu hardcode về `Theme.cs` (Ctrl+F tìm `Color.FromArgb`, `#003366`, `Color.Blue`…)
- [ ] Thay toàn bộ Button → ModernButton (Kind đúng vai trò)
- [ ] Gọi `GridStyler.Apply()` cho mọi DataGridView
- [ ] Bỏ nền xanh đậm header trang + header bảng
- [ ] Fix lỗi cột bảng bị cắt chữ (AutoSize + header height 38)
- [ ] Thay biểu đồ cũ bằng ScottPlot
- [ ] Empty states cho mọi bảng (icon + text, không text đỏ)
- [ ] Confirm dialog trước mọi hành động Xóa/Hủy
- [ ] Test DPI scale 100%/125%/150% (PerMonitorV2 trong app.manifest)
- [ ] Nếu dùng thư viện: kiểm tra false-positive AV trước khi release

## app.manifest (DPI awareness — bắt buộc để UI không vỡ)

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
  </windowsSettings>
</application>
```

---

*Hết tài liệu. Cập nhật khi chốt được font/brand màu cuối cùng.*
