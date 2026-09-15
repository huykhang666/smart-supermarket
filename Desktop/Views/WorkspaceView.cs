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

        var btnEmergency = new Button { Text = "🔔 Gọi Quản Lý (F11)", Size = new Size(180, 32), Location = new Point(pnlTop.Width - 380, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        AppTheme.ApplyDangerButton(btnEmergency);
        
        var btnHandover = new Button { Text = "✅ Kết Ca Bán Hàng (F12)", Size = new Size(180, 32), Location = new Point(pnlTop.Width - 190, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        AppTheme.ApplyPrimaryButton(btnHandover);

        pnlTop.Controls.Add(btnEmergency);
        pnlTop.Controls.Add(btnHandover);
        this.Controls.Add(pnlTop);

        // Spacer
        var pnlSpace1 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        this.Controls.Add(pnlSpace1);

        // --- 2. KPI Cards Row ---
        var pnlKpi = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 115, BackColor = Color.Transparent, WrapContents = false };
        
        pnlKpi.Controls.Add(AppTheme.CreateKpiCard("DOANH SỐ CA TRỰC", "0 đ", "Chưa có doanh số", AppTheme.Primary));
        pnlKpi.Controls.Add(AppTheme.CreateKpiCard("TỐC ĐỘ PHỤC VỤ POS", "0s", "Sẵn sàng phục vụ", AppTheme.Success));
        pnlKpi.Controls.Add(AppTheme.CreateKpiCard("CẬN HSD CẦN XỬ LÝ", "0 SP", "Không có cảnh báo", AppTheme.Warning));
        pnlKpi.Controls.Add(AppTheme.CreateKpiCard("YÊU CẦU HỖ TRỢ", "0 việc", "Hệ thống ổn định", AppTheme.Primary));

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

    private Panel CreateKpiCard(string title, string value, string unit, string subText1, string subText2, Color? bgColor = null, Color? fgColor = null)
    {
        var card = new Panel { Width = 310, Height = 135, BackColor = Color.White, Margin = new Padding(0, 0, 15, 0) };
        ThemeManager.ApplyCardPanel(card);
        
        var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 80, 90), AutoSize = true, Location = new Point(15, 15) };
        var lblVal = new Label { Text = value, Font = new Font("Segoe UI", 28f, FontStyle.Bold), ForeColor = fgColor ?? Color.FromArgb(20, 30, 40), AutoSize = true, Location = new Point(10, 40) };
        var lblUnit = new Label { Text = unit, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 120, 215), AutoSize = true, Location = new Point(lblVal.Right - 5, 58) };
        
        var pnlLine = new Panel { BackColor = Color.FromArgb(230, 235, 240), Height = 1, Width = 280, Location = new Point(15, 95) };
        var lblSub1 = new Label { Text = subText1, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, AutoSize = true, Location = new Point(15, 105) };
        var lblSub2 = new Label { Text = subText2, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 120, 215), AutoSize = true, Location = new Point(card.Width - 70, 105), Anchor = AnchorStyles.Right };
        
        if (bgColor != null) card.BackColor = bgColor.Value;

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblVal);
        card.Controls.Add(lblUnit);
        card.Controls.Add(pnlLine);
        card.Controls.Add(lblSub1);
        card.Controls.Add(lblSub2);
        
        return card;
    }

    private Panel CreateTaskPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 240, BackColor = Color.White };
        ThemeManager.ApplyCardPanel(pnl);
        
        var lblTitle = new Label { Text = "✔️ NHIỆM VỤ CA TRỰC & CHECK-LIST NHÂN VIÊN", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 50, 60), AutoSize = true, Location = new Point(15, 15) };
        var lblProgress = new Label { Text = "Sẵn sàng", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.FromArgb(30, 80, 120), ForeColor = Color.White, AutoSize = true, Location = new Point(pnl.Width - 140, 15), Anchor = AnchorStyles.Right };
        
        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblProgress);

        var lblEmpty = new Label { Text = "Hiện tại không có nhiệm vụ tồn đọng. Hệ thống vận hành bình thường.", Font = new Font("Segoe UI", 9.5f, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(20, 60), AutoSize = true };
        pnl.Controls.Add(lblEmpty);

        return pnl;
    }

    private Panel CreateTaskItem(string title, string sub, bool checkedState, string tag, Color tagColor, int y)
    {
        var p = new Panel { Width = 680, Height = 40, Location = new Point(15, y) };
        var chk = new CheckBox { Checked = checkedState, Location = new Point(5, 10), Size = new Size(20, 20) };
        var lblT = new Label { Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true, Location = new Point(30, 2) };
        var lblS = new Label { Text = sub, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, AutoSize = true, Location = new Point(30, 22) };
        var lblTag = new Label { Text = tag, Font = new Font("Segoe UI", 8f, FontStyle.Bold), BackColor = Color.FromArgb(40, tagColor), ForeColor = tagColor, AutoSize = true, Location = new Point(580, 10) };
        
        p.Controls.Add(chk); p.Controls.Add(lblT); p.Controls.Add(lblS); p.Controls.Add(lblTag);
        return p;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        ThemeManager.ApplyCardPanel(pnl);
        
        var lblTitle = new Label { Text = "🖨️ HÀNG CẬN DATE CẦN DÁN TEM NHANH", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 50, 60), AutoSize = true, Location = new Point(15, 15) };
        var btnPrintAll = new Button { Text = "In Tất Cả Tem (F8)", BackColor = Color.Crimson, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(130, 30), Location = new Point(pnl.Width - 160, 10), Anchor = AnchorStyles.Right };
        
        var dgv = new DataGridView { Location = new Point(15, 50), Size = new Size(pnl.Width - 30, pnl.Height - 65), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        ThemeManager.ApplyGridStyle(dgv);
        
        dgv.Columns.Add("SKU", "MÃ SKU");
        dgv.Columns.Add("Name", "TÊN SẢN PHẨM");
        dgv.Columns.Add("Date", "HSD CÒN");
        dgv.Columns.Add("Qty", "TỒN QUẦY");
        dgv.Columns.Add("Price", "GIÁ GỐC -> ĐỀ XUẤT");
        
        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(btnPrintAll);
        pnl.Controls.Add(dgv);
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
