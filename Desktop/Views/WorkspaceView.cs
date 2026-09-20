using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);
        this.AutoScroll = true;

        // --- 1. Top Bar (Employee Profile & Status) ---
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = AppTheme.SurfaceWhite, Padding = new Padding(16) };
        AppTheme.ApplyCardPanel(pnlTop);
        
        var iconComputer = new IconPictureBox { IconChar = IconChar.Desktop, IconColor = AppTheme.Primary, Size = new Size(42, 42), Location = new Point(20, 24), BackColor = AppTheme.PrimarySubtle };
        pnlTop.Controls.Add(iconComputer);

        var lblTags = new Label { Text = "TRẠM BÁN LẺ POS  •  PHIÊN LÀM VIỆC HIỆN TẠI", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, AutoSize = true, Location = new Point(80, 14) };
        var lblTitle = new Label { Text = "Không Gian Làm Việc Nhân Viên (Staff Workspace)", Font = AppTheme.FontH2, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(78, 34) };
        var lblSub = new Label { Text = "🟢 Sẵn sàng phục vụ khách hàng  |  Máy chủ POS: Hoạt động", Font = AppTheme.FontBody, ForeColor = AppTheme.Success, AutoSize = true, Location = new Point(80, 62) };
        
        pnlTop.Controls.Add(lblTags);
        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(lblSub);

        var pnlTopRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 16, 0, 0)
        };

        var btnEmergency = new Button { Text = "🔔 Gọi Quản Lý (F11)", Size = new Size(170, 32), Margin = new Padding(0, 0, 8, 0) };
        AppTheme.ApplyDangerButton(btnEmergency);
        
        var btnHandover = new Button { Text = "✅ Kết Ca Bán Hàng (F12)", Size = new Size(180, 32), Margin = new Padding(0) };
        AppTheme.ApplyPrimaryButton(btnHandover);

        pnlTopRight.Controls.Add(btnEmergency);
        pnlTopRight.Controls.Add(btnHandover);
        pnlTop.Controls.Add(pnlTopRight);
        this.Controls.Add(pnlTop);

        // Spacer
        var pnlSpace1 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        this.Controls.Add(pnlSpace1);

        // --- 2. KPI Cards Row (Responsive Grid) ---
        var pnlKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.Transparent
        };
        
        var c1 = AppTheme.CreateKpiCard("DOANH SỐ CA TRỰC", "0 đ", "Chưa có doanh số", AppTheme.Primary);
        var c2 = AppTheme.CreateKpiCard("TỐC ĐỘ PHỤC VỤ POS", "0s", "Sẵn sàng phục vụ", AppTheme.Success);
        var c3 = AppTheme.CreateKpiCard("CẬN HSD CẦN XỬ LÝ", "0 SP", "Không có cảnh báo", AppTheme.Warning);
        var c4 = AppTheme.CreateKpiCard("YÊU CẦU HỖ TRỢ", "0 việc", "Hệ thống ổn định", AppTheme.Primary);
        var kpiCards = new List<Control> { c1, c2, c3, c4 };

        AppTheme.EnableResponsiveKpiGrid(pnlKpi, kpiCards, this, 850, 440, 110);

        this.Controls.Add(pnlKpi);

        // Spacer
        var pnlSpace2 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        this.Controls.Add(pnlSpace2);

        // --- 3. Bottom Split Section ---
        var pnlSplit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        // Left Panel (Tasks + Grid)
        var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        pnlLeft.Controls.Add(CreateGridPanel());
        pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 10 }); // Spacer
        pnlLeft.Controls.Add(CreateTaskPanel());
        
        // Right Panel (Map + AI)
        var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        pnlRight.Controls.Add(CreateAiPanel());
        pnlRight.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 10 });
        pnlRight.Controls.Add(CreateMapPanel());

        pnlSplit.Controls.Add(pnlLeft, 0, 0);
        pnlSplit.Controls.Add(pnlRight, 1, 0);

        this.Controls.Add(pnlSplit);
    }

    private Panel CreateTaskPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 240, BackColor = Color.White };
        ThemeManager.ApplyCardPanel(pnl);

        var lblTitle = new Label { Text = "✔️ NHIỆM VỤ CA TRỰC & CHECK-LIST NHÂN VIÊN", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 50, 60), AutoSize = true, Location = new Point(15, 15) };
        var lblProgress = new Label { Text = "Sẵn sàng", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.FromArgb(30, 80, 120), ForeColor = Color.White, AutoSize = true, Location = new Point(pnl.Width - 140, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };

        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblProgress);

        var lblEmpty = new Label { Text = "Hiện tại không có nhiệm vụ tồn đọng. Hệ thống vận hành bình thường.", Font = new Font("Segoe UI", 9.5f, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(20, 60), AutoSize = true };
        pnl.Controls.Add(lblEmpty);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        ThemeManager.ApplyCardPanel(pnl);

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.Transparent };
        var lblTitle = new Label { Text = "🖨️ HÀNG CẬN DATE CẦN DÁN TEM NHANH", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 50, 60), AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
        var btnPrintAll = new Button { Text = "In Tất Cả Tem (F8)", BackColor = Color.Crimson, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(130, 30), Dock = DockStyle.Right, Cursor = Cursors.Hand };
        btnPrintAll.FlatAppearance.BorderSize = 0;
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnPrintAll);

        var dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        ThemeManager.ApplyGridStyle(dgv);

        dgv.Columns.Add("SKU", "MÃ SKU");
        dgv.Columns.Add("Name", "TÊN SẢN PHẨM");
        dgv.Columns.Add("Date", "HSD CÒN");
        dgv.Columns.Add("Qty", "TỒN QUẦY");
        dgv.Columns.Add("Price", "GIÁ GỐC -> ĐỀ XUẤT");

        pnl.Controls.Add(dgv);
        pnl.Controls.Add(pnlHeader);
        return pnl;
    }

    private Panel CreateMapPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.White };
        ThemeManager.ApplyCardPanel(pnl);
        var lblTitle = new Label { Text = "🗺️ SƠ ĐỒ KỆ HÀNG (PLANOGRAM TẦNG 1)", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15) };
        pnl.Controls.Add(lblTitle);
        return pnl;
    }

    private Panel CreateAiPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        ThemeManager.ApplyCardPanel(pnl);
        var lblTitle = new Label { Text = "🤖 AI CO-PILOT TRỢ GIÚP TÁC NGHIỆP", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15) };
        
        var lblTraffic = new Label { Text = "Dự Báo Lưu Lượng Khách:\nLưu lượng quầy dự kiến tăng +40% vào lúc 11:30 - 12:45. (Giờ nghỉ trưa của tòa nhà văn phòng lân cận).", Font = new Font("Segoe UI", 9.5f), AutoSize = true, Location = new Point(15, 50), MaximumSize = new Size(400, 0) };
        
        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblTraffic);
        return pnl;
    }
}
