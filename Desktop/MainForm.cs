using System;
using System.Drawing;
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
            Font = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 37, 69),
            AutoSize = true,
            Location = new Point(175, 18)
        };

        // Server Ready Pill Badge
        var pnlStatusBadge = new Panel
        {
            Size = new Size(220, 30),
            Location = new Point(620, 15),
            BackColor = AppTheme.PrimaryGreenLight
        };
        var lblStatus = new Label
        {
            Text = "🟢 Ready (Active) • POS Server",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = AppTheme.PrimaryGreenDark,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlStatusBadge.Controls.Add(lblStatus);

        // Notifications Button (Bell)
        btnNotification = new IconButton
        {
            IconChar = IconChar.Bell,
            IconColor = AppTheme.PrimaryGreen,
            IconSize = 18,
            Text = " 🔔 (11)",
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.PrimaryGreen,
            BackColor = AppTheme.PrimaryGreenLight,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(95, 32),
            Location = new Point(860, 14),
            Cursor = Cursors.Hand
        };
        btnNotification.FlatAppearance.BorderSize = 0;
        btnNotification.Click += (s, e) =>
        {
            AntdUI.Message.info(this, 
                "🔔 THÔNG BÁO HỆ THỐNG REALTIME:\n" +
                "• 5 Sản phẩm sắp hết hàng (< 10 sp)\n" +
                "• 3 Đơn hàng mới vừa tạo\n" +
                "• 2 Sản phẩm đã hết hàng trong kho\n" +
                "• 1 Lô hàng Sữa TH sắp hết hạn (3 ngày)");
        };

        // Admin User Profile Chip
        lblAdminProfile = new Label
        {
            Text = "👤 Nguyễn Huy Khang (Admin) ▼",
            Font = AppTheme.FontBodyBold,
            ForeColor = Color.FromArgb(11, 37, 69),
            AutoSize = true,
            Location = new Point(975, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };

        // Logout Button
        btnLogout = new Button
        {
            Text = "Đăng Xuất",
            Size = new Size(100, 32),
            Location = new Point(1310, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        AppTheme.ApplyAccentButton(btnLogout);
        btnLogout.Click += (s, e) =>
        {
            this.Hide();
            var loginForm = new LoginForm();
            loginForm.Show();
        };

        pnlHeader.Controls.Add(picLogo);
        pnlHeader.Controls.Add(lblAppTitle);
        pnlHeader.Controls.Add(pnlStatusBadge);
        pnlHeader.Controls.Add(btnNotification);
        pnlHeader.Controls.Add(lblAdminProfile);
        pnlHeader.Controls.Add(btnLogout);

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
        pnlSidebar.Controls.Add(btnNavPosScan);
        pnlSidebar.Controls.Add(btnNavInventory);
        pnlSidebar.Controls.Add(btnNavImport);
        pnlSidebar.Controls.Add(btnNavSuppliers);
        pnlSidebar.Controls.Add(btnNavCategories);
        pnlSidebar.Controls.Add(btnNavProducts);
        pnlSidebar.Controls.Add(btnNavDashboard);

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
            Height = 46,
            Text = "  " + text,
            IconChar = icon,
            IconColor = AppTheme.SidebarText,
            IconSize = 18,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.SidebarText,
            BackColor = ThemeManager.SidebarBg,
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
                // Draw 4px Accent Orange indicator bar on left edge
                using var orangeBar = new SolidBrush(AppTheme.AccentOrange);
                e.Graphics.FillRectangle(orangeBar, 0, 0, 4, btn.Height);
            }
        };

        btn.MouseEnter += (s, e) =>
        {
            if (btn != _activeNavButton)
            {
                btn.BackColor = AppTheme.PrimaryGreen;
                btn.ForeColor = Color.White;
                btn.IconColor = Color.White;
            }
        };

        btn.MouseLeave += (s, e) =>
        {
            if (btn != _activeNavButton)
            {
                btn.BackColor = ThemeManager.SidebarBg;
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
            _activeNavButton.BackColor = ThemeManager.SidebarBg;
            _activeNavButton.ForeColor = AppTheme.SidebarText;
            _activeNavButton.IconColor = AppTheme.SidebarText;
            _activeNavButton.Font = AppTheme.FontBody;
            _activeNavButton.Invalidate();
        }

        _activeNavButton = navBtn;
        _activeNavButton.BackColor = AppTheme.PrimaryGreen;
        _activeNavButton.ForeColor = Color.White;
        _activeNavButton.IconColor = Color.White;
        _activeNavButton.Font = AppTheme.FontBodyBold;
        _activeNavButton.Invalidate();

        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }
}
