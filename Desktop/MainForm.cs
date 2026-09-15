using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Desktop.Views;
using FontAwesome.Sharp;

namespace Desktop;

public class MainForm : Form
{
    private Panel pnlHeader = null!;
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    private Label lblAppTitle = null!;
    private Label lblAdminProfile = null!;
    private IconButton btnNotification = null!;
    private Button btnLogout = null!;

    // Nav Buttons
    private IconButton btnNavDashboard = null!;
    private IconButton btnNavProducts = null!;
    private IconButton btnNavCategories = null!;
    private IconButton btnNavSuppliers = null!;
    private IconButton btnNavImport = null!;
    private IconButton btnNavInventory = null!;
    private IconButton btnNavPosScan = null!;
    private IconButton btnNavOrders = null!;
    private IconButton btnNavCustomers = null!;
    private IconButton btnNavPromotions = null!;
    private IconButton btnNavAiCopilot = null!;
    private IconButton btnNavReports = null!;
    private IconButton btnNavEmployees = null!;
    private IconButton btnNavSettings = null!;

    private IconButton? _activeNavButton;

    // View Instances
    private readonly DashboardView _dashboardView = new();
    private readonly ProductsView _productsView = new();
    private readonly CategoriesView _categoriesView = new();
    private readonly SuppliersView _suppliersView = new();
    private readonly ImportGoodsView _importGoodsView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly PosScanView _posScanView = new();
    private readonly OrdersView _ordersView = new();
    private readonly CustomersView _customersView = new();
    private readonly PromotionsView _promotionsView = new();
    private readonly AiCopilotView _aiCopilotView = new();
    private readonly ReportsView _reportsView = new();
    private readonly EmployeesView _employeesView = new();
    private readonly SettingsView _settingsView = new();

    public MainForm()
    {
        InitializeComponent();
        SelectNavButton(btnNavDashboard, _dashboardView);
    }

    private void InitializeComponent()
    {
        this.Text = "Smart SuperMarket - ENTERPRISE ERP & POS CONTROL CENTER";
        this.Size = new Size(1440, 900);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = AppTheme.BackgroundGray;
        this.Font = AppTheme.FontBody;

        // --- 1. Top Header Panel (Height 60px, White & KATQ Sky Blue Theme) ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        // KATQ Smart Logo (Top-Left Corner)
        var picLogo = new PictureBox
        {
            Size = new Size(150, 42),
            Location = new Point(15, 9),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_katq.png");
        if (!System.IO.File.Exists(logoPath))
        {
            logoPath = @"D:\Laptrinhtrucquan\Desktop\Assets\logo_katq.png";
        }
        if (System.IO.File.Exists(logoPath))
        {
            try 
            { 
                using var originalBmp = new Bitmap(logoPath);
                var transparentBmp = new Bitmap(originalBmp);
                transparentBmp.MakeTransparent(Color.White);
                picLogo.Image = transparentBmp;
            } 
            catch { }
        }

        lblAppTitle = new Label
        {
            Text = "Smart SuperMarket  |  ENTERPRISE ERP POS CONTROL CENTER",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(175, 18)
        };

        // Right Controls Flow (Notification, Profile Chip, Logout)
        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 5, 10, 5)
        };

        // Server Ready Pill Badge
        var pnlStatusBadge = new Panel
        {
            Size = new Size(180, 32),
            Margin = new Padding(0, 0, 10, 0),
            BackColor = AppTheme.SuccessSubtle
        };
        var lblStatus = new Label
        {
            Text = "🟢 Ready • POS Server",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Success,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlStatusBadge.Controls.Add(lblStatus);

        // Notifications Button (Bell)
        btnNotification = new IconButton
        {
            IconChar = IconChar.Bell,
            IconColor = AppTheme.Primary,
            IconSize = 18,
            Text = " 🔔 (0)",
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Primary,
            BackColor = AppTheme.PrimarySubtle,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(85, 32),
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btnNotification.FlatAppearance.BorderSize = 0;
        btnNotification.Click += (s, e) =>
        {
            ShowNotificationPopup();
        };

        // Admin User Profile Chip
        lblAdminProfile = new Label
        {
            Text = "👤 Quản trị viên (Admin)",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 7, 12, 0),
            Cursor = Cursors.Hand
        };

        // Logout Button
        btnLogout = new Button
        {
            Text = "🚪 Đăng Xuất",
            Font = AppTheme.FontBodyBold,
            Size = new Size(115, 32),
            Margin = new Padding(0, 0, 5, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyDangerButton(btnLogout);
        btnLogout.Click += (s, e) =>
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất khỏi hệ thống quản trị?", "Xác nhận đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.Show();
            }
        };

        pnlHeaderRight.Controls.Add(pnlStatusBadge);
        pnlHeaderRight.Controls.Add(btnNotification);
        pnlHeaderRight.Controls.Add(lblAdminProfile);
        pnlHeaderRight.Controls.Add(btnLogout);

        pnlHeader.Controls.Add(pnlHeaderRight);
        pnlHeader.Controls.Add(picLogo);
        pnlHeader.Controls.Add(lblAppTitle);

        // --- 2. Left Sidebar Panel (Width 240px, Dark Navy #0B2545 Theme) ---
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = ThemeManager.SidebarBg,
            Padding = new Padding(0, 5, 0, 5),
            AutoScroll = true
        };

        btnNavDashboard = CreateNavButton("Dashboard Tổng Quan", IconChar.ChartLine);
        btnNavProducts = CreateNavButton("Sản Phẩm (Products)", IconChar.BoxOpen);
        btnNavCategories = CreateNavButton("Danh Mục (Category)", IconChar.FolderTree);
        btnNavSuppliers = CreateNavButton("Nhà Cung Cấp", IconChar.TruckLoading);
        btnNavImport = CreateNavButton("Nhập Hàng Kho", IconChar.FileImport);
        btnNavInventory = CreateNavButton("Quản Lý Tồn Kho", IconChar.BoxesStacked);
        btnNavPosScan = CreateNavButton("Bán Hàng POS", IconChar.CashRegister);
        btnNavOrders = CreateNavButton("Quản Lý Đơn Hàng", IconChar.Receipt);
        btnNavCustomers = CreateNavButton("Khách Hàng Loyalty", IconChar.UserGroup);
        btnNavPromotions = CreateNavButton("Chương Trình Khuyến Mãi", IconChar.Tags);
        btnNavAiCopilot = CreateNavButton("Trợ Lý AI Assistant", IconChar.Robot);
        btnNavReports = CreateNavButton("Báo Cáo Enterprise", IconChar.ChartPie);
        btnNavEmployees = CreateNavButton("Nhân Viên & Ca Trực", IconChar.UsersCog);
        btnNavSettings = CreateNavButton("Cấu Hình System", IconChar.Sliders);

        btnNavDashboard.Click += (s, e) => SelectNavButton(btnNavDashboard, _dashboardView);
        btnNavProducts.Click += (s, e) => SelectNavButton(btnNavProducts, _productsView);
        btnNavCategories.Click += (s, e) => SelectNavButton(btnNavCategories, _categoriesView);
        btnNavSuppliers.Click += (s, e) => SelectNavButton(btnNavSuppliers, _suppliersView);
        btnNavImport.Click += (s, e) => SelectNavButton(btnNavImport, _importGoodsView);
        btnNavInventory.Click += (s, e) => SelectNavButton(btnNavInventory, _inventoryView);
        btnNavPosScan.Click += (s, e) => SelectNavButton(btnNavPosScan, _posScanView);
        btnNavOrders.Click += (s, e) => SelectNavButton(btnNavOrders, _ordersView);
        btnNavCustomers.Click += (s, e) => SelectNavButton(btnNavCustomers, _customersView);
        btnNavPromotions.Click += (s, e) => SelectNavButton(btnNavPromotions, _promotionsView);
        btnNavAiCopilot.Click += (s, e) => SelectNavButton(btnNavAiCopilot, _aiCopilotView);
        btnNavReports.Click += (s, e) => SelectNavButton(btnNavReports, _reportsView);
        btnNavEmployees.Click += (s, e) => SelectNavButton(btnNavEmployees, _employeesView);
        btnNavSettings.Click += (s, e) => SelectNavButton(btnNavSettings, _settingsView);

        pnlSidebar.Controls.Add(btnNavSettings);
        pnlSidebar.Controls.Add(btnNavEmployees);
        pnlSidebar.Controls.Add(btnNavReports);
        pnlSidebar.Controls.Add(btnNavAiCopilot);
        pnlSidebar.Controls.Add(btnNavPromotions);
        pnlSidebar.Controls.Add(btnNavCustomers);
        pnlSidebar.Controls.Add(btnNavOrders);
        pnlSidebar.Controls.Add(btnNavInventory);
        pnlSidebar.Controls.Add(btnNavImport);
        pnlSidebar.Controls.Add(btnNavSuppliers);
        pnlSidebar.Controls.Add(btnNavCategories);
        pnlSidebar.Controls.Add(btnNavProducts);
        pnlSidebar.Controls.Add(btnNavDashboard);

        // Sidebar Bottom Logout Button
        var btnSidebarLogout = new IconButton
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
        btnSidebarLogout.FlatAppearance.BorderSize = 0;
        btnSidebarLogout.MouseEnter += (s, e) => { btnSidebarLogout.BackColor = Color.FromArgb(45, 20, 25); };
        btnSidebarLogout.MouseLeave += (s, e) => { btnSidebarLogout.BackColor = AppTheme.SidebarBg; };
        btnSidebarLogout.Click += (s, e) => btnLogout.PerformClick();
        pnlSidebar.Controls.Add(btnSidebarLogout);

        // --- 3. Content Panel ---
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.BackgroundGray
        };

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlSidebar);
        this.Controls.Add(pnlHeader);
    }

    private IconButton CreateNavButton(string text, IconChar icon)
    {
        var btn = new IconButton
        {
            Dock = DockStyle.Top,
            Height = 44,
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
                // Draw 3px Accent bar on left edge (Fluent 2 Sidebar active indicator)
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

    private Form? _notificationPopup;

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

        // Position popup right below btnNotification
        Point btnScreenLocation = btnNotification.PointToScreen(Point.Empty);
        int popupX = btnScreenLocation.X + btnNotification.Width - popup.Width;
        int popupY = btnScreenLocation.Y + btnNotification.Height + 6;
        popup.Location = new Point(popupX, popupY);

        // Border & shadow drawing
        popup.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
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
            Text = "🔔 Thông Báo Hệ Thống",
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
            AntdUI.Message.success(this, "Đã xóa toàn bộ thông báo hệ thống.");
        };
        pnlBottom.Controls.Add(btnClearAll);

        popup.Controls.Add(pnlBody);
        popup.Controls.Add(pnlBottom);
        popup.Controls.Add(pnlHeader);

        // Close on blur (when user clicks outside)
        popup.Deactivate += (s, e) => popup.Close();

        _notificationPopup = popup;
        popup.Show(this);
    }
}
