using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class StaffDashboardView : UserControl
{
    private Label lblOrdersVal = null!;
    private Label lblRevenueVal = null!;
    private Label lblCustomersVal = null!;
    private Label lblItemsVal = null!;
    private DataGridView dgvTop = null!;

    public StaffDashboardView()
    {
        InitializeComponent();
        DataStore.OrderCompleted += (s, e) => RefreshRealtimeMetrics();
        RefreshRealtimeMetrics();
    }

    private void RefreshRealtimeMetrics()
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(RefreshRealtimeMetrics));
            return;
        }

        lblOrdersVal.Text = $"{DataStore.TodayOrdersCount}";
        lblRevenueVal.Text = $"{DataStore.TodayRevenue:N0} đ";
        lblCustomersVal.Text = $"{DataStore.TodayOrdersCount}";
        lblItemsVal.Text = $"{DataStore.TodayItemsSoldCount}";

        // Refresh Top Sold Products Table
        if (dgvTop != null)
        {
            dgvTop.Rows.Clear();
            var topProducts = DataStore.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new
                {
                    Name = g.Key,
                    Qty = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Price * i.Quantity)
                })
                .OrderByDescending(x => x.Qty)
                .Take(10)
                .ToList();

            int rank = 1;
            foreach (var item in topProducts)
            {
                dgvTop.Rows.Add($"#{rank++}", item.Name, $"{item.Qty} món", $"{item.Revenue:N0} đ", "Còn hàng");
            }
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);
        this.AutoScroll = true;

        // --- 1. Page Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📊 Bảng Điều Khiển Ca Trực Nhân Viên (Staff Dashboard)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Theo dõi chỉ số hiệu suất bán hàng hôm nay, khách phục vụ, top sản phẩm và cảnh báo quầy POS",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. 5 KPI Stat Cards (Hôm nay: Đã bán 0 đơn, Doanh thu 0 đ, Khách 0, Sản phẩm 0, Lỗi quét 0) ---
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };

        var c1 = AppTheme.CreateKpiCard("ĐÃ BÁN HÔM NAY", "0", "🧾 Đơn hàng POS", AppTheme.Primary, out lblOrdersVal, out _);
        var c2 = AppTheme.CreateKpiCard("DOANH THU CA", "0 đ", "💵 Tiền về hệ thống", AppTheme.Success, out lblRevenueVal, out _);
        var c3 = AppTheme.CreateKpiCard("KHÁCH PHỤC VỤ", "0", "👥 Khách lẻ & Loyalty", AppTheme.Primary, out lblCustomersVal, out _);
        var c4 = AppTheme.CreateKpiCard("SẢN PHẨM ĐÃ QUÉT", "0", "📦 Mặt hàng xuất kho", AppTheme.Warning, out lblItemsVal, out _);
        var c5 = AppTheme.CreateKpiCard("LỖI QUÉT BARCODE", "0", "⚠️ Cần kiểm tra tem", AppTheme.Danger);
        var kpiCards = new List<Control> { c1, c2, c3, c4, c5 };

        AppTheme.EnableResponsiveKpiGrid(pnlKpis, kpiCards, this, 980, 480, 105);

        // --- 3. Content Split (Left: Top Sản Phẩm Bán Chạy, Right: Tác Vụ & Lỗi Thao Tác) ---
        var pnlSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        pnlSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        pnlSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // LEFT: Top Products Card
        var pnlTopProductsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(0, 8, 10, 0)
        };
        AppTheme.ApplyCardPanel(pnlTopProductsCard);

        var pnlCardHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 8)
        };

        var lblTopTitle = new Label
        {
            Text = "🏆 TOP SẢN PHẨM BÁN CHẠY NHẤT TRONG CA",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlCardHeader.Controls.Add(lblTopTitle);

        var pnlGridWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 8, 4, 4)
        };

        dgvTop = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        AppTheme.ApplyGridStyle(dgvTop);
        dgvTop.Columns.Add("Rank", "Hạng");
        dgvTop.Columns.Add("Name", "Tên Sản Phẩm");
        dgvTop.Columns.Add("Qty", "Đã Bán");
        dgvTop.Columns.Add("Revenue", "Doanh Thu");
        dgvTop.Columns.Add("Stock", "Còn Lại");

        dgvTop.Columns["Rank"].FillWeight = 12;
        dgvTop.Columns["Name"].FillWeight = 38;
        dgvTop.Columns["Qty"].FillWeight = 16;
        dgvTop.Columns["Revenue"].FillWeight = 20;
        dgvTop.Columns["Stock"].FillWeight = 14;

        pnlGridWrapper.Controls.Add(dgvTop);

        pnlTopProductsCard.Controls.Add(pnlGridWrapper);
        pnlTopProductsCard.Controls.Add(pnlCardHeader);

        // RIGHT: Quick Checklist & Issue alerts
        var pnlRightCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(10, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlRightCard);

        var pnlRightHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 8)
        };

        var lblRightTitle = new Label
        {
            Text = "✔️ VIỆC CẦN LÀM HÔM NAY (CHECKLIST)",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlRightHeader.Controls.Add(lblRightTitle);

        var pnlTasksFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 8, 4, 4)
        };

        string[] tasks = {
            "Kiểm tra quầy thu ngân & cuộn giấy in nhiệt",
            "Đếm & xác nhận số tiền mặt đầu ca",
            "Quét kiểm đếm đơn hàng nhập nếu có",
            "Kiểm tra hạn sử dụng các sản phẩm trên kệ",
            "Kiểm kê tồn thực tế kệ hàng phân công",
            "Đóng gói & bàn giao đơn hàng online",
            "Chốt kết ca & in biên bản bàn giao tiền mặt"
        };

        foreach (var task in tasks)
        {
            var chk = new CheckBox
            {
                Text = "  " + task,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6),
                Cursor = Cursors.Hand
            };
            pnlTasksFlow.Controls.Add(chk);
        }

        pnlRightCard.Controls.Add(pnlTasksFlow);
        pnlRightCard.Controls.Add(pnlRightHeader);

        pnlSplit.Controls.Add(pnlTopProductsCard, 0, 0);
        pnlSplit.Controls.Add(pnlRightCard, 1, 0);

        this.Controls.Add(pnlSplit);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }
}
