using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Desktop;

/// <summary>
/// Microsoft Fluent 2 Design Tokens for KATQ Smart Workstation ERP/POS.
/// Strictly conforms to docs/KATQ_WinForms_UI_Redesign_Guide.md.
/// </summary>
public static class AppTheme
{
    // --- Fluent 2 Color Tokens ---
    public static readonly Color Primary = Color.FromArgb(0x0F, 0x6C, 0xBD);        // #0F6CBD - Primary Blue Accent
    public static readonly Color PrimaryHover = Color.FromArgb(0x11, 0x5E, 0xA3);   // #115EA3 - Hover
    public static readonly Color PrimaryPressed = Color.FromArgb(0x0C, 0x3B, 0x5E); // #0C3B5E - Pressed
    public static readonly Color PrimarySubtle = Color.FromArgb(0xEB, 0xF3, 0xFC);  // #EBF3FC - Selected / KPI subtle bg

    public static readonly Color Success = Color.FromArgb(0x10, 0x7C, 0x10);        // #107C10 - Success
    public static readonly Color SuccessSubtle = Color.FromArgb(0xDF, 0xF6, 0xDD);  // #DFF6DD - Success Badge
    public static readonly Color Warning = Color.FromArgb(0xF7, 0x63, 0x0C);        // #F7630C - Warning
    public static readonly Color WarningSubtle = Color.FromArgb(0xFF, 0xF4, 0xE5);  // #FFF4E5 - Warning Badge
    public static readonly Color Danger = Color.FromArgb(0xC4, 0x2B, 0x1C);         // #C42B1C - Danger
    public static readonly Color DangerSubtle = Color.FromArgb(0xFD, 0xE7, 0xE9);   // #FDE7E9 - Danger Badge

    public static readonly Color SidebarBg = Color.FromArgb(0x1B, 0x1F, 0x23);      // #1B1F23 - Dark Charcoal Black Sidebar
    public static readonly Color SidebarHover = Color.FromArgb(0x2D, 0x33, 0x38);   // #2D3338 - Sidebar Hover
    public static readonly Color SidebarActive = Color.FromArgb(0x0F, 0x6C, 0xBD);  // #0F6CBD - Sidebar Active Item
    public static readonly Color SidebarAccent = Color.FromArgb(0x5E, 0xB4, 0xF2);  // #5EB4F2 - 3px Active Bar
    public static readonly Color SidebarText = Color.FromArgb(0xD6, 0xD6, 0xD6);    // #D6D6D6 - Sidebar Inactive Text
    public static readonly Color SidebarTextActive = Color.White;

    public static readonly Color BackgroundGray = Color.FromArgb(0xF5, 0xF5, 0xF5); // #F5F5F5 - Page Background
    public static readonly Color SurfaceWhite = Color.White;                       // #FFFFFF - Card Surface
    public static readonly Color BorderLight = Color.FromArgb(0xE0, 0xE0, 0xE0);    // #E0E0E0 - Border Lines
    public static readonly Color GridHeaderBg = Color.FromArgb(0xF3, 0xF2, 0xF1);   // #F3F2F1 - Light Gray Header (NEVER Dark Blue)

    public static readonly Color TextPrimary = Color.FromArgb(0x24, 0x24, 0x24);    // #242424 - Main Text
    public static readonly Color TextSecondary = Color.FromArgb(0x61, 0x61, 0x61);  // #616161 - Subtitle Text
    public static readonly Color TextDisabled = Color.FromArgb(0xBD, 0xBD, 0xBD);   // #BDBDBD - Disabled Text

    // --- Backward Compatible Aliases ---
    public static Color PrimaryGreen => Primary;
    public static Color PrimaryGreenDark => PrimaryHover;
    public static Color PrimaryGreenLight => PrimarySubtle;
    public static Color AccentOrange => Warning;
    public static Color InfoBlue => Primary;
    public static Color SuccessGreen => Success;
    public static Color WarningAmber => Warning;
    public static Color DangerRed => Danger;

    // --- Typography Tokens (Microsoft Fluent 2 Segoe UI) ---
    public static readonly Font FontTitle = new Font("Segoe UI", 20F, FontStyle.Bold);
    public static readonly Font FontH1 = new Font("Segoe UI", 20F, FontStyle.Bold);
    public static readonly Font FontH2 = new Font("Segoe UI", 14F, FontStyle.Bold);
    public static readonly Font FontH3 = new Font("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font FontKpi = new Font("Segoe UI", 24F, FontStyle.Bold);
    public static readonly Font FontCaption = new Font("Segoe UI", 9.5F, FontStyle.Regular);

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
        dgv.RowTemplate.Height = 36;
        dgv.ColumnHeadersHeight = 38;
        dgv.EnableHeadersVisualStyles = false;
        dgv.Font = FontBody;

        // Header Styling (Fluent 2 Light Gray #F3F2F1 Header, Dark Text #242424)
        dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Font = FontH3;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBg;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // Rows Styling
        dgv.DefaultCellStyle.BackColor = SurfaceWhite;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = PrimarySubtle;
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // Alternating Rows (Zebra Striping)
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFA, 0xFA);
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = PrimarySubtle;
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextPrimary;
    }

    public static void ApplyCardPanel(Panel panel, int radius = 8)
    {
        panel.BackColor = SurfaceWhite;
        panel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Subtle Drop Shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
            {
                using var shadowPath = GetRoundedPath(new Rectangle(1, 2, panel.Width - 3, panel.Height - 3), radius);
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
        btn.BackColor = Primary;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Height = 32;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Primary;
    }

    public static void ApplyAccentButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = Primary;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Height = 32;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Primary;
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
        btn.Height = 32;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimarySubtle;
        btn.MouseLeave += (s, e) => btn.BackColor = SurfaceWhite;
    }

    public static Panel CreateKpiCard(string title, string value, string note, Color accentColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceWhite,
            Margin = new Padding(6),
            Padding = new Padding(14)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Card Body
            using (var cardPath = GetRoundedPath(new Rectangle(0, 0, card.Width - 3, card.Height - 3), 8))
            {
                using (var cardBrush = new SolidBrush(SurfaceWhite))
                    e.Graphics.FillPath(cardBrush, cardPath);
                using (var borderPen = new Pen(BorderLight, 1))
                    e.Graphics.DrawPath(borderPen, cardPath);
            }

            // Accent Bar on Left (3px)
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
            Location = new Point(16, 70),
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

