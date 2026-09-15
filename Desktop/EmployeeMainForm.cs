using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Desktop.Views;

namespace Desktop;

public class EmployeeMainForm : Form
{
    private Panel pnlHeader = null!;
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    
    private IconButton? _activeNavButton;

    // 8 Specialized Staff Modules
    private readonly PosScanView _posScanView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly ImportGoodsView _importGoodsView = new();
    private readonly StockAuditView _stockAuditView = new();
    private readonly StaffNotificationsView _notificationsView = new();
    private readonly StaffShiftView _staffShiftView = new();
    private readonly StaffDashboardView _staffDashboardView = new();
    private readonly AiCopilotView _aiCopilotView = new();

    // Top Header Status Badges
    private Control _badgeNotify = null!;
    private Form? _notificationPopup;
    private Label lblClock = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    public EmployeeMainForm()
    {
        InitializeComponent();
        var defaultNavBtn = pnlSidebar.Controls["btnNavPos"] as IconButton;
        if (defaultNavBtn != null)
        {
            SelectNavButton(defaultNavBtn, _posScanView);
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Smart SuperMarket  |  STAFF ENTERPRISE POS & WORKSTATION";
        this.Size = new Size(1440, 900);
        this.WindowState = FormWindowState.Maximized;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = AppTheme.BackgroundGray;

        // ==============================================================
        // 1. TOPBAR HEADER (Fluent 2 Style, Status Badges & Realtime Clock)
        // ==============================================================
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(15, 0, 15, 0)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        // Logo
        var picLogo = new PictureBox
        {
            Size = new Size(130, 42),
            Location = new Point(12, 10),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_katq.png");
        if (!System.IO.File.Exists(logoPath)) logoPath = @"D:\Laptrinhtrucquan\Desktop\Assets\logo_katq.png";
        if (System.IO.File.Exists(logoPath))
        {
            try
            {
                using var bmp = new Bitmap(logoPath);
                var transBmp = new Bitmap(bmp);
                transBmp.MakeTransparent(Color.White);
                picLogo.Image = transBmp;
            }
            catch { }
        }

        var lblSystemTitle = new Label
        {
            Text = "Smart SuperMarket  |  STAFF WORKSTATION",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(150, 18)
        };

        // Right side Status Badges container
        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 8)
        };

        // Badge 1: Ca làm
        var badgeShift = CreateTopStatusBadge("🟢 Ca làm: Đang mở", AppTheme.SuccessSubtle, AppTheme.Success);
        // Badge 2: Đơn nhập hôm nay
        var badgeImport = CreateTopStatusBadge("📦 Đơn nhập: 0", AppTheme.PrimarySubtle, AppTheme.Primary);
        // Badge 3: Thông báo (Clickable Popup)
        _badgeNotify = CreateTopStatusBadge("🔔 Thông báo: 0", AppTheme.PrimarySubtle, AppTheme.Primary);
        _badgeNotify.Cursor = Cursors.Hand;
        _badgeNotify.Click += (s, e) => ShowNotificationPopup();
        foreach (Control c in _badgeNotify.Controls)
        {
            c.Cursor = Cursors.Hand;
            c.Click += (s, e) => ShowNotificationPopup();
        }

        // Badge 4: Tồn kho thấp
        var badgeStock = CreateTopStatusBadge("📊 Tồn thấp: 0", AppTheme.SuccessSubtle, AppTheme.Success);
        // Badge 5: User
        var lblUser = new Label
        {
            Text = "👤 Thu Ngân (Staff)",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(8, 6, 8, 0),
            AutoSize = true
        };
        // Badge 6: Clock
        lblClock = new Label
        {
            Text = $"🕒 {DateTime.Now:HH:mm:ss}",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            Margin = new Padding(8, 5, 8, 0),
            AutoSize = true
        };
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (s, e) => lblClock.Text = $"🕒 {DateTime.Now:HH:mm:ss}";
        _clockTimer.Start();

        // Logout Button
        var btnLogout = new Button
        {
            Text = "🚪 Đăng Xuất",
            Font = AppTheme.FontBodyBold,
            Size = new Size(110, 32),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyDangerButton(btnLogout);
        btnLogout.Click += (s, e) =>
        {
            var res = MessageBox.Show("Bạn có muốn đăng xuất khỏi phiên làm việc thu ngân?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                this.Hide();
                var login = new LoginForm();
                login.Show();
            }
        };

        pnlHeaderRight.Controls.Add(badgeShift);
        pnlHeaderRight.Controls.Add(badgeImport);
        pnlHeaderRight.Controls.Add(_badgeNotify);
        pnlHeaderRight.Controls.Add(badgeStock);
        pnlHeaderRight.Controls.Add(lblUser);
        pnlHeaderRight.Controls.Add(lblClock);
        pnlHeaderRight.Controls.Add(btnLogout);

        pnlHeader.Controls.Add(picLogo);
        pnlHeader.Controls.Add(lblSystemTitle);
        pnlHeader.Controls.Add(pnlHeaderRight);

        // ==============================================================
        // 2. SIDEBAR PANEL (8 Chuyên Mục Chuẩn Yêu Cầu, Dark Navy Fluent 2)
        // ==============================================================
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = AppTheme.SidebarBg,
            Padding = new Padding(0, 8, 0, 8),
            AutoScroll = true
        };

        var btnPos = CreateNavButton("btnNavPos", "POS Bán hàng", IconChar.CashRegister);
        var btnInventory = CreateNavButton("btnNavInventory", "Kho Hàng HSD", IconChar.Warehouse);
        var btnImport = CreateNavButton("btnNavImport", "Nhập hàng", IconChar.TruckLoading);
        var btnAudit = CreateNavButton("btnNavAudit", "Kiểm kê", IconChar.ClipboardCheck);
        var btnShift = CreateNavButton("btnNavShift", "Ca làm việc", IconChar.Clock);
        var btnDashboard = CreateNavButton("btnNavDashboard", "Dashboard", IconChar.ChartLine);
        var btnAi = CreateNavButton("btnNavAi", "AI Assistant", IconChar.Robot);

        btnPos.Click += (s, e) => SelectNavButton(btnPos, _posScanView);
        btnInventory.Click += (s, e) => SelectNavButton(btnInventory, _inventoryView);
        btnImport.Click += (s, e) => SelectNavButton(btnImport, _importGoodsView);
        btnAudit.Click += (s, e) => SelectNavButton(btnAudit, _stockAuditView);
        btnShift.Click += (s, e) => SelectNavButton(btnShift, _staffShiftView);
        btnDashboard.Click += (s, e) => SelectNavButton(btnDashboard, _staffDashboardView);
        btnAi.Click += (s, e) => SelectNavButton(btnAi, _aiCopilotView);

        // Add in reverse because Dock = Top stacks them
        pnlSidebar.Controls.Add(btnAi);
        pnlSidebar.Controls.Add(btnDashboard);
        pnlSidebar.Controls.Add(btnShift);
        pnlSidebar.Controls.Add(btnAudit);
        pnlSidebar.Controls.Add(btnImport);
        pnlSidebar.Controls.Add(btnInventory);
        pnlSidebar.Controls.Add(btnPos);

        // Bottom Logout on Sidebar
        var btnBottomLogout = new IconButton
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Text = "  Đăng Xuất",
            IconChar = IconChar.SignOutAlt,
            IconColor = AppTheme.Danger,
            IconSize = 18,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Danger,
            BackColor = AppTheme.SidebarBg,
            FlatStyle = FlatStyle.Flat,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnBottomLogout.FlatAppearance.BorderSize = 0;
        btnBottomLogout.Click += (s, e) => btnLogout.PerformClick();
        pnlSidebar.Controls.Add(btnBottomLogout);

        // ==============================================================
        // 3. CONTENT PANEL
        // ==============================================================
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.BackgroundGray
        };

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlSidebar);
        this.Controls.Add(pnlHeader);
    }

    private Control CreateTopStatusBadge(string text, Color bg, Color fg)
    {
        var pnl = new Panel
        {
            BackColor = bg,
            Margin = new Padding(0, 0, 8, 0),
            Height = 32,
            AutoSize = true,
            Padding = new Padding(10, 6, 10, 6)
        };
        var lbl = new Label
        {
            Text = text,
            Font = AppTheme.FontBodyBold,
            ForeColor = fg,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true
        };
        pnl.Controls.Add(lbl);
        return pnl;
    }

    private IconButton CreateNavButton(string name, string text, IconChar icon)
    {
        var btn = new IconButton
        {
            Name = name,
            Dock = DockStyle.Top,
            Height = 46,
            Text = "  " + text,
            IconChar = icon,
            IconColor = AppTheme.SidebarText,
            IconSize = 18,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.SidebarText,
            BackColor = AppTheme.SidebarBg,
            FlatStyle = FlatStyle.Flat,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.Paint += (s, e) =>
        {
            if (btn == _activeNavButton)
            {
                using var accentBar = new SolidBrush(AppTheme.SidebarAccent);
                e.Graphics.FillRectangle(accentBar, 0, 6, 3, btn.Height - 12);
            }
        };

        btn.MouseEnter += (s, e) =>
        {
            if (btn != _activeNavButton)
            {
                btn.BackColor = AppTheme.SidebarHover;
                btn.ForeColor = Color.White;
                btn.IconColor = Color.White;
            }
        };

        btn.MouseLeave += (s, e) =>
        {
            if (btn != _activeNavButton)
            {
                btn.BackColor = AppTheme.SidebarBg;
                btn.ForeColor = AppTheme.SidebarText;
                btn.IconColor = AppTheme.SidebarText;
            }
        };

        return btn;
    }

    private void SelectNavButton(IconButton navBtn, UserControl view)
    {
        if (_activeNavButton != null)
        {
            _activeNavButton.BackColor = AppTheme.SidebarBg;
            _activeNavButton.ForeColor = AppTheme.SidebarText;
            _activeNavButton.IconColor = AppTheme.SidebarText;
            _activeNavButton.Font = AppTheme.FontBody;
            _activeNavButton.Invalidate();
        }

        _activeNavButton = navBtn;
        _activeNavButton.BackColor = AppTheme.SidebarActive;
        _activeNavButton.ForeColor = Color.White;
        _activeNavButton.IconColor = Color.White;
        _activeNavButton.Font = AppTheme.FontBodyBold;
        _activeNavButton.Invalidate();

        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }

    private void ShowNotificationPopup()
    {
        if (_notificationPopup != null && !_notificationPopup.IsDisposed)
        {
            _notificationPopup.Close();
            _notificationPopup = null;
            return;
        }

        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Size = new Size(360, 290),
            BackColor = AppTheme.SurfaceWhite,
            TopMost = true
        };

        // Position popup right below _badgeNotify
        Point btnScreenLocation = _badgeNotify.PointToScreen(Point.Empty);
        int popupX = btnScreenLocation.X + _badgeNotify.Width - popup.Width;
        int popupY = btnScreenLocation.Y + _badgeNotify.Height + 6;
        popup.Location = new Point(popupX, popupY);

        // Border & shadow drawing
        popup.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var borderPen = new Pen(AppTheme.BorderLight, 1.5f);
            e.Graphics.DrawRectangle(borderPen, 0, 0, popup.Width - 1, popup.Height - 1);
        };

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(14, 10, 14, 8)
        };
        var lblTitle = new Label
        {
            Text = "🔔 Thông Báo Hệ Thống (Staff)",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(14, 12)
        };
        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = AppTheme.TextSecondary,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(26, 24),
            Location = new Point(popup.Width - 36, 10),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => popup.Close();

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnClose);

        // Body Content
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = AppTheme.SurfaceWhite
        };

        var picEmpty = new IconPictureBox
        {
            IconChar = IconChar.BellSlash,
            IconColor = Color.FromArgb(160, 185, 205),
            IconSize = 48,
            Size = new Size(48, 48),
            Location = new Point((popup.Width - 48) / 2, 35),
            BackColor = Color.Transparent
        };

        var lblEmpty = new Label
        {
            Text = "Hiện tại không có thông báo mới nào",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Size = new Size(320, 24),
            Location = new Point(10, 95)
        };

        var lblSub = new Label
        {
            Text = "Các cảnh báo về tồn kho, hết hạn sử dụng hoặc đơn hàng mới sẽ xuất hiện tại đây theo thời gian thực.",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Size = new Size(300, 40),
            Location = new Point(20, 120)
        };

        pnlBody.Controls.Add(picEmpty);
        pnlBody.Controls.Add(lblEmpty);
        pnlBody.Controls.Add(lblSub);

        // Bottom action
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(10, 6, 10, 6)
        };
        var btnClearAll = new Button
        {
            Text = "Đánh dấu đã đọc tất cả",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.Primary,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderSize = 0;
        btnClearAll.Click += (s, e) =>
        {
            popup.Close();
            AntdUI.Message.success(this, "Đã đọc và xóa toàn bộ thông báo.");
        };
        pnlBottom.Controls.Add(btnClearAll);

        popup.Controls.Add(pnlBody);
        popup.Controls.Add(pnlBottom);
        popup.Controls.Add(pnlHeader);

        popup.Deactivate += (s, e) => popup.Close();

        _notificationPopup = popup;
        popup.Show(this);
    }
}
