using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Desktop;

public static class AppTheme
{
    // --- Bảng màu KATQ Smart Sky Blue (Đồng bộ với giao diện Login) ---
    public static readonly Color PrimaryGreen = ColorTranslator.FromHtml("#0087E6");      // Primary KATQ Sky Blue
    public static readonly Color PrimaryGreenDark = ColorTranslator.FromHtml("#005FB4");  // Header / Hover Dark Sky Blue
    public static readonly Color PrimaryGreenLight = ColorTranslator.FromHtml("#E6F4FF"); // Light Sky Blue Hover/Card
    
    public static readonly Color AccentOrange = ColorTranslator.FromHtml("#FF8A00");      // Orange Accent / CTA Buttons
    public static readonly Color InfoBlue = ColorTranslator.FromHtml("#00A8CC");          // Teal Cyan KATQ Accent
    
    public static readonly Color SuccessGreen = ColorTranslator.FromHtml("#16A34A");      // Active / Completed Status
    public static readonly Color WarningAmber = ColorTranslator.FromHtml("#F59E0B");      // Expiring / Pending Status
    public static readonly Color DangerRed = ColorTranslator.FromHtml("#DC2626");         // Out of Stock / Inactive Status
    
    public static readonly Color BackgroundGray = ColorTranslator.FromHtml("#F4F6F8");    // Main Form Background
    public static readonly Color SurfaceWhite = ColorTranslator.FromHtml("#FFFFFF");       // Card / Panel / Grid Background
    public static readonly Color BorderLight = ColorTranslator.FromHtml("#E5E7EB");       // Border Lines
    
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1F2937");       // Main Dark Text
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#6B7280");     // Muted Gray Subtitle Text
    public static readonly Color SidebarText = ColorTranslator.FromHtml("#BAE6FF");       // Inactive Sidebar Text
    public static readonly Color SidebarTextActive = ColorTranslator.FromHtml("#FFFFFF"); // Active Sidebar Text

    // --- Typography (Font Chữ Phân Cấp - Phóng to 10% cho giao diện Admin) ---
    public static readonly Font FontTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FontH1 = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold);
    public static readonly Font FontH2 = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold);
    public static readonly Font FontBody = new Font("Segoe UI", 11.5F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new Font("Segoe UI", 11.5F, FontStyle.Bold);
    public static readonly Font FontKpi = new Font("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font FontCaption = new Font("Segoe UI", 10.5F, FontStyle.Regular);

    public static void ApplyGridStyle(DataGridView dgv)
    {
        dgv.Dock = DockStyle.Fill;
        dgv.BackgroundColor = SurfaceWhite;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = BorderLight;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.ReadOnly = true;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.RowTemplate.Height = 46;
        dgv.ColumnHeadersHeight = 48;
        dgv.EnableHeadersVisualStyles = false;
        dgv.Font = FontBody;

        // Header Styling (PrimaryGreenDark Header)
        dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryGreenDark;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // Rows Styling
        dgv.DefaultCellStyle.BackColor = SurfaceWhite;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = PrimaryGreenLight;
        dgv.DefaultCellStyle.SelectionForeColor = PrimaryGreenDark;
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // Alternating Rows (Zebra Striping)
        dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = PrimaryGreenLight;
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = PrimaryGreenDark;
    }

    public static void ApplyCardPanel(Panel panel, int radius = 12)
    {
        panel.BackColor = SurfaceWhite;
        panel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Subtle Drop Shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
            {
                using var shadowPath = GetRoundedPath(new Rectangle(2, 3, panel.Width - 5, panel.Height - 4), radius);
                e.Graphics.FillPath(shadowBrush, shadowPath);
            }

            // Draw Card Body & Border
            using (var cardBrush = new SolidBrush(SurfaceWhite))
            {
                using var cardPath = GetRoundedPath(new Rectangle(0, 0, panel.Width - 3, panel.Height - 3), radius);
                e.Graphics.FillPath(cardBrush, cardPath);
                using var borderPen = new Pen(BorderLight, 1);
                e.Graphics.DrawPath(borderPen, cardPath);
            }
        };
    }

    public static void ApplyPrimaryButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = PrimaryGreen;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryGreenDark;
        btn.MouseLeave += (s, e) => btn.BackColor = PrimaryGreen;
    }

    public static void ApplyAccentButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = AccentOrange;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;

        btn.MouseEnter += (s, e) => btn.BackColor = ColorTranslator.FromHtml("#E67C00");
        btn.MouseLeave += (s, e) => btn.BackColor = AccentOrange;
    }

    public static void ApplyOutlineButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = TextPrimary;
        btn.BackColor = SurfaceWhite;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderLight;
        btn.Cursor = Cursors.Hand;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryGreenLight;
        btn.MouseLeave += (s, e) => btn.BackColor = SurfaceWhite;
    }

    public static Panel CreateKpiCard(string title, string value, string note, Color accentColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceWhite,
            Margin = new Padding(8),
            Padding = new Padding(16)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Card Body
            using (var cardPath = GetRoundedPath(new Rectangle(0, 0, card.Width - 3, card.Height - 3), 12))
            {
                using (var cardBrush = new SolidBrush(SurfaceWhite))
                    e.Graphics.FillPath(cardBrush, cardPath);
                using (var borderPen = new Pen(BorderLight, 1))
                    e.Graphics.DrawPath(borderPen, cardPath);
            }

            // Accent Bar on Left
            using var leftBar = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(leftBar, 0, 2, 4, card.Height - 6);
        };

        var lblTitle = new Label
        {
            Text = title.ToUpper(),
            Font = FontCaption,
            ForeColor = TextSecondary,
            Location = new Point(16, 12),
            AutoSize = true
        };

        var lblValue = new Label
        {
            Text = value,
            Font = FontKpi,
            ForeColor = TextPrimary,
            Location = new Point(14, 30),
            AutoSize = true
        };

        var lblNote = new Label
        {
            Text = note,
            Font = FontCaption,
            ForeColor = accentColor,
            Location = new Point(16, 72),
            AutoSize = true
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        card.Controls.Add(lblNote);
        return card;
    }

    public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2f;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
