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
    private IconButton btnLogout = null!;

    // Nav Buttons
    private IconButton btnNavDashboard = null!;
    private IconButton btnNavProducts = null!;
    private IconButton btnNavCategories = null!;
    private IconButton btnNavSuppliers = null!;
    private IconButton btnNavInventory = null!;
    private IconButton btnNavImportReceipts = null!;
    private IconButton btnNavPosScan = null!;
    private IconButton btnNavAiCopilot = null!;
    private IconButton btnNavAdminUsers = null!;

    private IconButton? _activeNavButton;

    // KATQ Smart Brand Color Palette
    private static readonly Color NavyPrimary = Color.FromArgb(11, 37, 69);     // #0B2545 - KATQ Navy
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 204);    // #00A8CC - smart Teal
    private static readonly Color DarkSidebar = Color.FromArgb(7, 25, 46);      // #07192E - Darker Sidebar

    // View Instances
    private readonly DashboardView _dashboardView = new();
    private readonly ProductsView _productsView = new();
    private readonly CategoriesView _categoriesView = new();
    private readonly SuppliersView _suppliersView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly ImportReceiptsView _importReceiptsView = new();
    private readonly PosScanView _posScanView = new();
    private readonly AiCopilotView _aiCopilotView = new();
    private readonly AdminUsersView _adminUsersView = new();

    public MainForm()
    {
        InitializeComponent();
        SelectNavButton(btnNavDashboard, _dashboardView);
    }

    private void InitializeComponent()
    {
        this.Text = "KATQ smart - TRUNG TÂM ĐIỀU HÀNH ADMIN & AI INSIGHTS";
        this.Size = new Size(1380, 840);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 244, 248);

        // --- 1. Top Header Panel ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 235, 245), 1);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        lblAppTitle = new Label
        {
            Text = "KATQ smart  |  ENTERPRISE POS & AI INSIGHTS CONTROL CENTER",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = NavyPrimary,
            AutoSize = true,
            Location = new Point(20, 18)
        };

        // Realtime status badge
        var pnlStatusBadge = new Panel
        {
            Size = new Size(330, 32),
            Location = new Point(570, 16),
            BackColor = Color.FromArgb(230, 246, 250)
        };
        var lblStatus = new Label
        {
            Text = "🟢 Dự Báo Nhu Cầu & Giá Động AI Realtime (148 Siêu Thị)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = TealAccent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlStatusBadge.Controls.Add(lblStatus);

        // Admin User Info Profile Chip
        lblAdminProfile = new Label
        {
            Text = "👤 Nguyễn Văn An (Quản Lý Admin)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 55, 75),
            AutoSize = true,
            Location = new Point(pnlHeader.Width - 360, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        // Logout Button
        btnLogout = new IconButton
        {
            IconChar = IconChar.SignOutAlt,
            IconColor = Color.FromArgb(207, 19, 34),
            IconSize = 18,
            Text = " Đăng Xuất",
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(207, 19, 34),
            BackColor = Color.FromArgb(255, 241, 240),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 34),
            Location = new Point(pnlHeader.Width - 140, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Click += (s, e) =>
        {
            this.Hide();
            var loginForm = new LoginForm();
            loginForm.Show();
        };

        pnlHeader.Controls.Add(lblAppTitle);
        pnlHeader.Controls.Add(pnlStatusBadge);
        pnlHeader.Controls.Add(lblAdminProfile);
        pnlHeader.Controls.Add(btnLogout);

        // --- 2. Left Sidebar Panel (Dark KATQ Navy Theme) ---
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = DarkSidebar,
            Padding = new Padding(0, 10, 0, 10)
        };

        btnNavDashboard = CreateNavButton("Tổng Quan & AI Insights", IconChar.ChartLine);
        btnNavProducts = CreateNavButton("Quản Lý Sản Phẩm", IconChar.BoxOpen);
        btnNavCategories = CreateNavButton("Quản Lý Danh Mục", IconChar.FolderTree);
        btnNavSuppliers = CreateNavButton("Quản Lý Nhà Cung Cấp", IconChar.TruckLoading);
        btnNavInventory = CreateNavButton("Quản Lý Tồn Kho", IconChar.Warehouse);
        btnNavImportReceipts = CreateNavButton("Quản Lý Nhập Hàng", IconChar.FileInvoiceDollar);
        btnNavPosScan = CreateNavButton("Bán Hàng POS & Scan", IconChar.CashRegister);
        btnNavAiCopilot = CreateNavButton("Trợ Lý AI & Báo Cáo", IconChar.Robot);
        btnNavAdminUsers = CreateNavButton("Quản Lý Nhân Viên", IconChar.UsersCog);

        btnNavDashboard.Click += (s, e) => SelectNavButton(btnNavDashboard, _dashboardView);
        btnNavProducts.Click += (s, e) => SelectNavButton(btnNavProducts, _productsView);
        btnNavCategories.Click += (s, e) => SelectNavButton(btnNavCategories, _categoriesView);
        btnNavSuppliers.Click += (s, e) => SelectNavButton(btnNavSuppliers, _suppliersView);
        btnNavInventory.Click += (s, e) => SelectNavButton(btnNavInventory, _inventoryView);
        btnNavImportReceipts.Click += (s, e) => SelectNavButton(btnNavImportReceipts, _importReceiptsView);
        btnNavPosScan.Click += (s, e) => SelectNavButton(btnNavPosScan, _posScanView);
        btnNavAiCopilot.Click += (s, e) => SelectNavButton(btnNavAiCopilot, _aiCopilotView);
        btnNavAdminUsers.Click += (s, e) => SelectNavButton(btnNavAdminUsers, _adminUsersView);

        pnlSidebar.Controls.Add(btnNavAdminUsers);
        pnlSidebar.Controls.Add(btnNavAiCopilot);
        pnlSidebar.Controls.Add(btnNavPosScan);
        pnlSidebar.Controls.Add(btnNavImportReceipts);
        pnlSidebar.Controls.Add(btnNavInventory);
        pnlSidebar.Controls.Add(btnNavSuppliers);
        pnlSidebar.Controls.Add(btnNavCategories);
        pnlSidebar.Controls.Add(btnNavProducts);
        pnlSidebar.Controls.Add(btnNavDashboard);

        // --- 3. Content Panel ---
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 248)
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
            Height = 52,
            Text = "  " + text,
            IconChar = icon,
            IconColor = Color.FromArgb(160, 185, 210),
            IconSize = 20,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(160, 185, 210),
            BackColor = DarkSidebar,
            FlatStyle = FlatStyle.Flat,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SelectNavButton(IconButton navBtn, UserControl view)
    {
        if (_activeNavButton != null)
        {
            _activeNavButton.BackColor = DarkSidebar;
            _activeNavButton.ForeColor = Color.FromArgb(160, 185, 210);
            _activeNavButton.IconColor = Color.FromArgb(160, 185, 210);
            _activeNavButton.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        }

        _activeNavButton = navBtn;
        _activeNavButton.BackColor = TealAccent; // Active KATQ Teal Cyan
        _activeNavButton.ForeColor = Color.White;
        _activeNavButton.IconColor = Color.White;
        _activeNavButton.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }
}
