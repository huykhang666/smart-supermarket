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
        dgv.RowTemplate.Height = 40;
        dgv.ColumnHeadersHeight = 58;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgv.EnableHeadersVisualStyles = false;
        dgv.Font = FontBody;

        // Header Styling (Fluent 2 Light Gray #F3F2F1 Header, Dark Text #242424)
        dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBg;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // Rows Styling
        dgv.DefaultCellStyle.BackColor = SurfaceWhite;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = PrimarySubtle;
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

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

    public static void ApplyPrimaryButton(Button btn, int radius = 6)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = Primary;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseDownBackColor = PrimaryPressed;
        btn.FlatAppearance.MouseOverBackColor = PrimaryHover;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(12, 4, 12, 4);
        btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        if (btn.Height < 34) btn.Height = 34;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = PrimaryHover;
            btn.ForeColor = Color.White;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = Primary;
            btn.ForeColor = Color.White;
        };
    }

    public static void ApplySuccessButton(Button btn, int radius = 6)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = Success;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x0E, 0x68, 0x0E);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x13, 0x8C, 0x13);
        btn.Cursor = Cursors.Hand;
        if (btn.Height < 34) btn.Height = 34;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = Color.FromArgb(0x13, 0x8C, 0x13);
            btn.ForeColor = Color.White;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = Success;
            btn.ForeColor = Color.White;
        };
    }

    public static void ApplyAccentButton(Button btn, int radius = 6) => ApplyPrimaryButton(btn, radius);

    // --- Spacing Tokens ---
    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space6 = 24;

    // --- System Centralized API Base URL ---
    public const string ApiBaseUrl = "http://localhost:5137";

    public static void ApplyOutlineButton(Button btn, int radius = 6)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = TextPrimary;
        btn.BackColor = SurfaceWhite;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderLight;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xD4, 0xEB, 0xFA);
        btn.FlatAppearance.MouseOverBackColor = PrimarySubtle;
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(12, 4, 12, 4);
        btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        if (btn.Height < 34) btn.Height = 34;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = PrimarySubtle;
            btn.ForeColor = Primary;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = SurfaceWhite;
            btn.ForeColor = TextPrimary;
        };
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

    public static void ApplySecondaryButton(Button btn, int radius = 6) => ApplyOutlineButton(btn, radius);

    public static void ApplyDangerButton(Button btn, int radius = 6)
    {
        btn.Font = FontBodyBold;
        btn.ForeColor = Color.White;
        btn.BackColor = Danger;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x8B, 0x1F, 0x14);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xA4, 0x26, 0x1C);
        btn.Cursor = Cursors.Hand;
        btn.Padding = new Padding(12, 4, 12, 4);
        btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        if (btn.Height < 34) btn.Height = 34;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = Color.FromArgb(0xA4, 0x26, 0x1C);
            btn.ForeColor = Color.White;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn.Tag?.ToString() == "loading") return;
            btn.BackColor = Danger;
            btn.ForeColor = Color.White;
        };
    }

    /// <summary>
    /// Thực thi một tác vụ bất đồng bộ với hiệu ứng loading trên nút:
    /// - Khi bấm: Chuyển sang Nền Trắng (#FFFFFF), Chữ Đen Đậm (#242424) kèm biểu tượng ⏳, Viền Primary rõ nét.
    /// - Chuột rê qua / click trong lúc loading: Giữ nguyên nền trắng chữ đen (không bị biến màu/trùng màu).
    /// - Khi hoàn tất: Khôi phục chính xác trạng thái, màu sắc và viền ban đầu.
    /// </summary>
    public static async Task ExecuteWithLoadingAsync(Button btn, string loadingText, Func<Task> action)
    {
        if (btn.Tag?.ToString() == "loading") return;

        string origText = btn.Text;
        Color origBg = btn.BackColor;
        Color origFg = btn.ForeColor;
        Font origFont = btn.Font;
        int origBorderSize = btn.FlatAppearance.BorderSize;
        Color origBorderColor = btn.FlatAppearance.BorderColor;
        Color origMouseOver = btn.FlatAppearance.MouseOverBackColor;
        Color origMouseDown = btn.FlatAppearance.MouseDownBackColor;
        Cursor origCursor = btn.Cursor;

        try
        {
            btn.Tag = "loading";
            btn.Text = $"⏳ {loadingText}";
            btn.Font = FontBodyBold;
            btn.BackColor = Color.White;
            btn.ForeColor = TextPrimary; // Chữ đen đậm nét #242424
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Primary;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 250);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 238, 242);
            btn.Cursor = Cursors.WaitCursor;
            btn.Refresh();

            var task = action();
            await Task.WhenAll(task, Task.Delay(250));
        }
        finally
        {
            btn.Text = origText;
            btn.Font = origFont;
            btn.BackColor = origBg;
            btn.ForeColor = origFg;
            btn.FlatAppearance.BorderSize = origBorderSize;
            btn.FlatAppearance.BorderColor = origBorderColor;
            btn.FlatAppearance.MouseOverBackColor = origMouseOver;
            btn.FlatAppearance.MouseDownBackColor = origMouseDown;
            btn.Cursor = origCursor;
            btn.Tag = null;
            btn.Refresh();
        }
    }

    /// <summary>
    /// Thực thi một tác vụ đồng bộ (như Xuất Excel, In PDF) với hiệu ứng loading trên nút:
    /// Nền Trắng, Chữ Đen Đậm, Viền rõ nét, khôi phục sau khi xong.
    /// </summary>
    public static void ExecuteWithLoading(Button btn, string loadingText, Action action)
    {
        if (btn.Tag?.ToString() == "loading") return;

        string origText = btn.Text;
        Color origBg = btn.BackColor;
        Color origFg = btn.ForeColor;
        Font origFont = btn.Font;
        int origBorderSize = btn.FlatAppearance.BorderSize;
        Color origBorderColor = btn.FlatAppearance.BorderColor;
        Color origMouseOver = btn.FlatAppearance.MouseOverBackColor;
        Color origMouseDown = btn.FlatAppearance.MouseDownBackColor;
        Cursor origCursor = btn.Cursor;

        try
        {
            btn.Tag = "loading";
            btn.Text = $"⏳ {loadingText}";
            btn.Font = FontBodyBold;
            btn.BackColor = Color.White;
            btn.ForeColor = TextPrimary; // Chữ đen đậm nét #242424
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Primary;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 250);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 238, 242);
            btn.Cursor = Cursors.WaitCursor;
            btn.Refresh();

            action();
        }
        finally
        {
            btn.Text = origText;
            btn.Font = origFont;
            btn.BackColor = origBg;
            btn.ForeColor = origFg;
            btn.FlatAppearance.BorderSize = origBorderSize;
            btn.FlatAppearance.BorderColor = origBorderColor;
            btn.FlatAppearance.MouseOverBackColor = origMouseOver;
            btn.FlatAppearance.MouseDownBackColor = origMouseDown;
            btn.Cursor = origCursor;
            btn.Tag = null;
            btn.Refresh();
        }
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
            Padding = new Padding(16, 12, 16, 12)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Card Body with subtle border
            using (var cardPath = GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
            {
                using (var cardBrush = new SolidBrush(SurfaceWhite))
                    e.Graphics.FillPath(cardBrush, cardPath);
                using (var borderPen = new Pen(BorderLight, 1))
                    e.Graphics.DrawPath(borderPen, cardPath);
            }

            // Accent Bar on Left (4px)
            using var leftBar = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(leftBar, 0, 4, 4, card.Height - 8);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(4, 0, 0, 0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));

        var lblTitle = new Label
        {
            Text = title.ToUpper(),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        lblNote = new Label
        {
            Text = note,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = accentColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        layout.Controls.Add(lblTitle, 0, 0);
        layout.Controls.Add(lblValue, 0, 1);
        layout.Controls.Add(lblNote, 0, 2);

        card.Controls.Add(layout);
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

    public static void EnableResponsiveKpiGrid(TableLayoutPanel tlp, IList<Control> cards, Control parentView, int singleRowMinTotalWidth = 850, int twoRowMinTotalWidth = 440, int baseCardHeight = 125)
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

