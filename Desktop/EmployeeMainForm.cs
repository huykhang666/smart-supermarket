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
    private WorkspaceView _workspaceView = new();
    
    public EmployeeMainForm()
    {
        InitializeComponent();
        SelectNavButton((IconButton)pnlSidebar.Controls["btnNavWorkspace"], _workspaceView);
    }

    private void InitializeComponent()
    {
        this.Text = "KATQ Smart Retail Management System v2.4";
        this.Size = new Size(1366, 768);
        this.WindowState = FormWindowState.Maximized;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 244, 248);

        // --- Header ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(15, 5, 15, 5)
        };
        pnlHeader.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 224, 228)), 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblLogo = new Label
        {
            Text = "KATQ Desktop POS",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 37, 69),
            AutoSize = true,
            Location = new Point(20, 15)
        };
        pnlHeader.Controls.Add(lblLogo);

        // Header menus
        int menuX = 220;
        string[] headerMenus = { "[Hệ Thống]", "[Bán Hàng POS]", "[Kho & HSD]", "[Quản Trị AI]", "[Báo Cáo]", "[Trợ Giúp]" };
        foreach (var menu in headerMenus)
        {
            var lblMenu = new Label
            {
                Text = menu,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(60, 70, 80),
                AutoSize = true,
                Location = new Point(menuX, 20),
                Cursor = Cursors.Hand
            };
            pnlHeader.Controls.Add(lblMenu);
            menuX += lblMenu.Width + 15;
        }

        // Header User Info
        var lblUserInfo = new Label
        {
            Text = "Thu Thảo\nNV-8821 • Thu Ngân Trưởng",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 50, 60),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(pnlHeader.Width - 250, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        pnlHeader.Controls.Add(lblUserInfo);

        // --- Sidebar ---
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Color.White, // In mockup it's light themed
            Padding = new Padding(10)
        };
        pnlSidebar.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 224, 228)), pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
        };

        var lblNavTitle = new Label
        {
            Text = "C# DESKTOP NAVIGATOR   WinForms\nDanh sách Form nghiệp vụ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(10, 10)
        };
        pnlSidebar.Controls.Add(lblNavTitle);

        int btnY = 60;
        
        pnlSidebar.Controls.Add(CreateNavButton("btnNavPos", "Thu Ngân & POS", "F2", IconChar.CashRegister, btnY += 45));
        pnlSidebar.Controls.Add(CreateNavButton("btnNavInventory", "Kho & Cảnh Báo HSD", "F3", IconChar.Warehouse, btnY += 45));
        pnlSidebar.Controls.Add(CreateNavButton("btnNavWorkspace", "Bàn Làm Việc NV", "F4", IconChar.UserTie, btnY += 45));
        pnlSidebar.Controls.Add(CreateNavButton("btnNavDashboard", "Dashboard & AI", "F5", IconChar.ChartLine, btnY += 45));
        pnlSidebar.Controls.Add(CreateNavButton("btnNavCopilot", "Trợ Lý AI Co-Pilot", "F6", IconChar.Robot, btnY += 45));

        // --- Footer (Status bar) ---
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.White
        };
        pnlFooter.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 224, 228)), 0, 0, pnlFooter.Width, 0);
        };
        var lblStatus = new Label { Text = "🟢 SQL SERVER: LocalDB / KATQ_DB | Ping: 2ms", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.SeaGreen, AutoSize = true, Location = new Point(10, 7) };
        pnlFooter.Controls.Add(lblStatus);

        // --- Content Panel ---
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 248)
        };

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlSidebar);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private IconButton CreateNavButton(string name, string text, string hotkey, IconChar icon, int yPos)
    {
        var btn = new IconButton
        {
            Name = name,
            Text = "   " + text,
            IconChar = icon,
            IconSize = 20,
            IconColor = Color.FromArgb(100, 110, 120),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 90, 100),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Size = new Size(220, 40),
            Location = new Point(10, yPos),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        
        // F2, F3 hotkey label simulator
        var lblHotkey = new Label
        {
            Text = hotkey,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            BackColor = Color.FromArgb(230, 235, 240),
            ForeColor = Color.FromArgb(100, 110, 120),
            AutoSize = true,
            Location = new Point(185, 12)
        };
        btn.Controls.Add(lblHotkey);

        btn.Click += (s, e) => {
            if (name == "btnNavWorkspace") SelectNavButton(btn, _workspaceView);
            else SelectNavButton(btn, new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 248) }); // Placeholder for others
        };

        return btn;
    }

    private void SelectNavButton(IconButton navBtn, Control view)
    {
        if (_activeNavButton != null)
        {
            _activeNavButton.BackColor = Color.White;
            _activeNavButton.ForeColor = Color.FromArgb(80, 90, 100);
            _activeNavButton.IconColor = Color.FromArgb(100, 110, 120);
        }

        _activeNavButton = navBtn;
        _activeNavButton.BackColor = Color.FromArgb(230, 245, 255); // Light blue selection
        _activeNavButton.ForeColor = Color.FromArgb(0, 120, 215);
        _activeNavButton.IconColor = Color.FromArgb(0, 120, 215);

        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }
}
