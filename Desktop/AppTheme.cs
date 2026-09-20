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
    // --- KATQ Smart Sky Blue Design Tokens ---
    public static readonly Color Primary = Color.FromArgb(0x00, 0x87, 0xE6);        // #0087E6 - KATQ Sky Blue Primary Accent
    public static readonly Color PrimaryHover = Color.FromArgb(0x00, 0x76, 0xCC);   // #0076CC - Sky Blue Hover
    public static readonly Color PrimaryPressed = Color.FromArgb(0x00, 0x5F, 0xB4); // #005FB4 - Sky Blue Pressed / Title Bar Blue
    public static readonly Color PrimarySubtle = Color.FromArgb(0xEB, 0xF6, 0xFF);  // #EBF6FF - Soft Ice Blue Highlight / Selected Row

    public static readonly Color Success = Color.FromArgb(0x10, 0x7C, 0x10);        // #107C10 - Success Green
    public static readonly Color SuccessSubtle = Color.FromArgb(0xDF, 0xF6, 0xDD);  // #DFF6DD - Success Badge
    public static readonly Color Warning = Color.FromArgb(0xF7, 0x63, 0x0C);        // #F7630C - Warning Amber
    public static readonly Color WarningSubtle = Color.FromArgb(0xFF, 0xF4, 0xE5);  // #FFF4E5 - Warning Badge
    public static readonly Color Danger = Color.FromArgb(0xC4, 0x2B, 0x1C);         // #C42B1C - Danger Red
    public static readonly Color DangerSubtle = Color.FromArgb(0xFD, 0xE7, 0xE9);   // #FDE7E9 - Danger Badge

    public static readonly Color SidebarBg = Color.FromArgb(0x0B, 0x25, 0x45);      // #0B2545 - Deep Navy KATQ Brand Sidebar
    public static readonly Color SidebarHover = Color.FromArgb(0x13, 0x36, 0x60);   // #133660 - Sidebar Item Hover
    public static readonly Color SidebarActive = Color.FromArgb(0x00, 0x87, 0xE6);  // #0087E6 - Sidebar Active Item Sky Blue
    public static readonly Color SidebarAccent = Color.FromArgb(0x5E, 0xD4, 0xFF);  // #5ED4FF - 3px Glowing Cyan Active Indicator
    public static readonly Color SidebarText = Color.FromArgb(0xC5, 0xE1, 0xFA);    // #C5E1FA - Soft Pastel Sky Inactive Text
    public static readonly Color SidebarTextActive = Color.White;

    public static readonly Color BackgroundGray = Color.FromArgb(0xE2, 0xF1, 0xFC); // #E2F1FC - Vibrant Fresh Sky Blue Tint Page Background
    public static readonly Color SurfaceWhite = Color.White;                       // #FFFFFF - Crisp Clean Card Surface
    public static readonly Color BorderLight = Color.FromArgb(0xBC, 0xE0, 0xFD);    // #BCE0FD - Harmonious Sky Blue Border Line
    public static readonly Color GridHeaderBg = Color.FromArgb(0xD4, 0xEB, 0xFA);   // #D4EBFA - Clear Sky Blue Header Tint

    public static readonly Color TextPrimary = Color.FromArgb(0x11, 0x22, 0x33);    // #112233 - Deep Navy-Charcoal Text for Crystal Clarity
    public static readonly Color TextSecondary = Color.FromArgb(0x4A, 0x68, 0x85);  // #4A6885 - Cool Slate Blue Subtitle
    public static readonly Color TextDisabled = Color.FromArgb(0x9E, 0xB5, 0xCB);   // #9EB5CB - Soft Steel Disabled Text

    // --- Backward Compatible Aliases ---
    public static Color PrimaryGreen => Primary;
    public static Color PrimaryGreenDark => PrimaryHover;
    public static Color PrimaryGreenLight => PrimarySubtle;
    public static Color AccentOrange => Warning;
    public static Color InfoBlue => Primary;
    public static Color SuccessGreen => Success;
    public static Color WarningAmber => Warning;
    public static Color DangerRed => Danger;

    // --- Typography Tokens (Segoe UI Phân Cấp Rõ Ràng & Cân Đối) ---
    public static readonly Font FontTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FontH1 = new Font("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font FontH2 = new Font("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font FontH3 = new Font("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font FontKpi = new Font("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font FontCaption = new Font("Segoe UI", 9F, FontStyle.Regular);

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

    // --- Spacing Tokens ---
    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space6 = 24;

    // --- System Centralized API Base URL ---
    public const string ApiBaseUrl = "http://localhost:5137";

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

    public static void ApplyComboBoxStyle(ComboBox cb, int height = 32)
    {
        cb.Font = FontBody;
        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.ItemHeight = height - 8;
        cb.Height = height;
        cb.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using var bg = new SolidBrush(selected ? PrimarySubtle : SurfaceWhite);
            using var fg = new SolidBrush(selected ? Primary : TextPrimary);
            e.Graphics.FillRectangle(bg, e.Bounds);
            string text = cb.Items[e.Index]?.ToString() ?? "";
            TextRenderer.DrawText(e.Graphics, text, FontBody, new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), fg.Color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            e.DrawFocusRectangle();
        };
    }

    public static void ApplySecondaryButton(Button btn) => ApplyOutlineButton(btn);

    public static void ApplyDangerButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = Danger;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Height = 32;

        btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(0xA4, 0x26, 0x1C);
        btn.MouseLeave += (s, e) => btn.BackColor = Danger;
    }

    public static void ApplyGhostButton(Button btn)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Primary;
        btn.BackColor = Color.Transparent;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Height = 32;

        btn.MouseEnter += (s, e) => btn.BackColor = PrimarySubtle;
        btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
    }

    public static Panel CreateKpiCard(string title, string value, string note, Color accentColor)
    {
        return CreateKpiCard(title, value, note, accentColor, out _, out _);
    }

    public static Panel CreateKpiCard(string title, string value, string note, Color accentColor, out Label lblValue, out Label lblNote)
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

        lblValue = new Label
        {
            Text = value,
            Font = FontKpi,
            ForeColor = TextPrimary,
            Location = new Point(14, 30),
            AutoSize = true
        };

        lblNote = new Label
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

    public static void EnableResponsiveKpiGrid(TableLayoutPanel tlp, IList<Control> cards, Control parentView, int singleRowMinTotalWidth = 850, int twoRowMinTotalWidth = 440, int baseCardHeight = 110)
    {
        int lastCols = -1;
        void Relayout()
        {
            if (tlp == null || cards == null || cards.Count == 0 || parentView.IsDisposed) return;
            int availableWidth = parentView.ClientSize.Width - parentView.Padding.Horizontal;
            int targetCols;
            if (availableWidth >= singleRowMinTotalWidth)
                targetCols = Math.Min(cards.Count, cards.Count <= 5 ? cards.Count : 4);
            else if (availableWidth >= twoRowMinTotalWidth)
                targetCols = cards.Count == 5 && availableWidth >= 600 ? 3 : 2;
            else
                targetCols = 1;

            if (targetCols == lastCols) return;
            lastCols = targetCols;

            tlp.SuspendLayout();
            tlp.Controls.Clear();
            tlp.ColumnStyles.Clear();
            tlp.RowStyles.Clear();

            int targetRows = (cards.Count + targetCols - 1) / targetCols;
            tlp.ColumnCount = targetCols;
            tlp.RowCount = targetRows;

            float colPct = 100f / targetCols;
            for (int c = 0; c < targetCols; c++)
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colPct));

            float rowPct = 100f / targetRows;
            for (int r = 0; r < targetRows; r++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, rowPct));

            tlp.Height = targetRows * baseCardHeight;

            for (int i = 0; i < cards.Count; i++)
            {
                int col = i % targetCols;
                int row = i / targetCols;
                tlp.Controls.Add(cards[i], col, row);
            }
            tlp.ResumeLayout(true);
        }

        parentView.Resize += (s, e) => Relayout();
        Relayout();
    }
}

