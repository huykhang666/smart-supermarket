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
        this.BackColor = Color.FromArgb(244, 246, 249);
        this.Padding = new Padding(15);
        this.AutoScroll = true;

        // --- 1. Top Bar (Employee Profile & Status) ---
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(15) };
        ThemeManager.ApplyCardPanel(pnlTop);
        
        var iconComputer = new IconPictureBox { IconChar = IconChar.Desktop, IconColor = Color.Teal, Size = new Size(50, 50), Location = new Point(20, 25), BackColor = Color.FromArgb(230, 245, 250) };
        pnlTop.Controls.Add(iconComputer);

        var lblTags = new Label { Text = "frmBanLamViecNhanVien.cs   POS-04 (Line 2 Tầng 1)   Ca Sáng [06:00 - 14:00]", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(160, 180, 200), AutoSize = true, Location = new Point(90, 15) };
        var lblTitle = new Label { Text = "Trạm Máy Nhân Viên: Trần Thị Thu Thảo", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 30, 40), AutoSize = true, Location = new Point(85, 35) };
        var lblSub = new Label { Text = "👨‍💼 Trưởng ca: Nguyễn Văn An   |   🟢 Đã điểm danh FaceID AI (05:58)", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.SeaGreen, AutoSize = true, Location = new Point(90, 68) };
        
        pnlTop.Controls.Add(lblTags);
        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(lblSub);

        var btnEmergency = new IconButton { Text = " Gọi Quản Lý / Khẩn Cấp (F11)", IconChar = IconChar.Bell, IconColor = Color.White, IconSize = 18, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, Size = new Size(240, 36), Location = new Point(pnlTop.Width - 460, 35), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, TextImageRelation = TextImageRelation.ImageBeforeText };
        btnEmergency.FlatAppearance.BorderSize = 0;
        
        var btnHandover = new IconButton { Text = " Bàn Giao Ca / Kết Ca (F12)", IconChar = IconChar.CheckCircle, IconColor = Color.White, IconSize = 18, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), BackColor = Color.FromArgb(0, 120, 150), ForeColor = Color.White, Size = new Size(220, 36), Location = new Point(pnlTop.Width - 210, 35), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, TextImageRelation = TextImageRelation.ImageBeforeText };
        btnHandover.FlatAppearance.BorderSize = 0;

        pnlTop.Controls.Add(btnEmergency);
        pnlTop.Controls.Add(btnHandover);
        this.Controls.Add(pnlTop);

        // Spacer
        var pnlSpace1 = new Panel { Dock = DockStyle.Top, Height = 15, BackColor = Color.Transparent };
        this.Controls.Add(pnlSpace1);

        // --- 2. KPI Cards Row ---
        var pnlKpi = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent, WrapContents = false };
        
        pnlKpi.Controls.Add(CreateKpiCard("DOANH SỐ CA TRỰC", "18.650.000", "đ", "Tiến độ: 82% KPI ca (22.500.000đ)", "94 bill"));
        pnlKpi.Controls.Add(CreateKpiCard("TỐC ĐỘ PHỤC VỤ POS", "38", "giây/khách", "Đánh giá: Đạt Chuẩn (5 Sao)", "Kỳ vọng < 45s"));
        pnlKpi.Controls.Add(CreateKpiCard("CẬN HSD CẦN XỬ LÝ", "08", "mặt hàng", "Hạn chót dán tem: 10:00 Sáng", "GẤP", Color.FromArgb(255, 230, 230), Color.Red));
        pnlKpi.Controls.Add(CreateKpiCard("YÊU CẦU HỖ TRỢ", "02", "việc chờ", "Khách đổi size (Q.3) • Đổi tiền 50k", "Chi tiết →"));

        this.Controls.Add(pnlKpi);

        // Spacer
        var pnlSpace2 = new Panel { Dock = DockStyle.Top, Height = 15, BackColor = Color.Transparent };
        this.Controls.Add(pnlSpace2);

        // --- 3. Bottom Split Section ---
        var pnlSplit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        // Left Panel (Tasks + Grid)
        var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
        pnlLeft.Controls.Add(CreateGridPanel());
        pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 15 }); // Spacer
        pnlLeft.Controls.Add(CreateTaskPanel());
        
        // Right Panel (Map + AI)
        var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 0, 0) };
        pnlRight.Controls.Add(CreateAiPanel());
        pnlRight.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 15 });
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
        var lblProgress = new Label { Text = "Hoàn thành: 2/4", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.FromArgb(30, 80, 120), ForeColor = Color.White, AutoSize = true, Location = new Point(pnl.Width - 140, 15), Anchor = AnchorStyles.Right };
        
        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblProgress);

        int y = 50;
        pnl.Controls.Add(CreateTaskItem("Kiểm quỹ tiền lẻ đầu ca tại ngăn kéo (5.000.000 đ)", "06:05 • Xác nhận két an toàn: Đủ tiền niêm phong", true, "Đã xong", Color.SeaGreen, y));
        pnl.Controls.Add(CreateTaskItem("Bán hàng cao điểm sáng & Hỗ trợ thanh toán QR/Ví", "07:00 - 09:30 • Đang phục vụ khách hàng lượt 95", false, "Đang chạy", Color.SteelBlue, y += 45));
        pnl.Controls.Add(CreateTaskItem("Kiểm tra date khu vực rau củ tươi & Sữa tươi thanh trùng", "Trước 10:00 • Còn 8 mã sản phẩm cần rà soát", false, "Cần làm", Color.Crimson, y += 45));
        pnl.Controls.Add(CreateTaskItem("Dán tem giảm giá AI (Markdown -30% / -50%) cho mặt hàng", "11:30 • Chờ AI Co-pilot phát hành biểu giá tối ưu", false, "Chờ tới giờ", Color.Gray, y += 45));

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
        
        dgv.Rows.Add("SUA-019", "Sữa Tươi Tiệt Trùng 900ml", "6 Giờ", "12 hộp", "29.000đ -> 20.300đ (-30%)");
        dgv.Rows.Add("KATQ-BK-0082", "Bánh Mì Sandwich Lúa Mạch", "18 Giờ", "06 gói", "24.000đ -> 12.000đ (-50%)");
        dgv.Rows.Add("KATQ-RAU-031", "Xà Lách Thủy Canh 300g", "22 Giờ", "15 khay", "19.500đ -> 15.600đ (-20%)");
        
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
