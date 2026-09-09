using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;

namespace Desktop.Views;

public class DashboardView : UserControl
{
    private Panel pnlHeaderBanner = null!;
    private TableLayoutPanel tlpKpis = null!;
    private Panel pnlChartContainer = null!;
    private Panel pnlAiInsightsContainer = null!;
    private LiveCharts.WinForms.CartesianChart chartRevenue = null!;

    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.AutoScroll = true;
        this.Padding = new Padding(20);

        // --- 1. Top Realtime Banner ---
        pnlHeaderBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.FromArgb(230, 244, 255),
            Padding = new Padding(20, 15, 20, 15),
            Margin = new Padding(0, 0, 0, 20)
        };

        var lblBannerTitle = new Label
        {
            Text = "TRUNG TÂM ĐIỀU HÀNH MARKET AI  •  REALTIME V1.0",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var lblBannerSub = new Label
        {
            Text = "148 Siêu thị đang kết nối  •  Hệ thống phân tích tự động chuỗi cung ứng, sức mua người dùng và cảnh báo rủi ro tức thời theo chuẩn Enterprise",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(80, 90, 100),
            AutoSize = true,
            Location = new Point(20, 48)
        };

        var btnRefreshRealtime = new Button
        {
            Text = "🔄 Làm Mới Tức Thì",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 36),
            Location = new Point(pnlHeaderBanner.Width - 190, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnRefreshRealtime.FlatAppearance.BorderSize = 0;
        btnRefreshRealtime.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Đã cập nhật dữ liệu Realtime mới nhất!");

        pnlHeaderBanner.Controls.Add(lblBannerTitle);
        pnlHeaderBanner.Controls.Add(lblBannerSub);
        pnlHeaderBanner.Controls.Add(btnRefreshRealtime);

        // --- 2. 4 Metric KPI Cards ---
        tlpKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 120,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 20, 0, 20)
        };
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        tlpKpis.Controls.Add(CreateKpiCard("DOANH THU HÔM NAY 💵", "248.500.000 đ", "▲ +14.2% Kế hoạch: 103.5%", Color.FromArgb(9, 109, 217), Color.FromArgb(230, 244, 255)), 0, 0);
        tlpKpis.Controls.Add(CreateKpiCard("TỔNG ĐƠN HÀNG 🛒", "1.420 đơn", "Giờ trung bình (AOV): 175.000đ", Color.FromArgb(22, 119, 255), Color.FromArgb(240, 245, 255)), 1, 0);
        tlpKpis.Controls.Add(CreateKpiCard("KHÁCH HÀNG MỚI & VIP 👥", "186 thành viên", "+42 VIP Diamond  •  +28.5%/tuần", Color.FromArgb(56, 158, 13), Color.FromArgb(246, 255, 237)), 2, 0);
        tlpKpis.Controls.Add(CreateKpiCard("CẢNH BÁO ĐỎ & HSD ⚠️", "12 mặt hàng", "5 cận kho & 7 cận date  •  Xử lý ngay", Color.FromArgb(207, 19, 34), Color.FromArgb(255, 241, 240)), 3, 0);

        // --- 3. LiveChart Revenue & Forecast Panel ---
        pnlChartContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 360,
            BackColor = Color.White,
            Padding = new Padding(20),
            Margin = new Padding(0, 20, 0, 20)
        };

        var lblChartTitle = new Label
        {
            Text = "📈 AI Insights & Phân Tích Doanh Thu Toàn Hệ Thống",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 21, 41),
            Dock = DockStyle.Top,
            Height = 30
        };

        chartRevenue = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill
        };

        // Populate LiveCharts Data
        chartRevenue.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Thực tế 7 ngày (Doanh thu)",
                Values = new ChartValues<double> { 185, 192, 178, 215, 230, 228, 248.5 },
                PointGeometrySize = 8,
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                Fill = System.Windows.Media.Brushes.Transparent
            },
            new LineSeries
            {
                Title = "AI Dự báo tăng trưởng",
                Values = new ChartValues<double> { 185, 192, 178, 215, 230, 235, 260 },
                Stroke = System.Windows.Media.Brushes.LightSeaGreen,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4 },
                PointGeometrySize = 6,
                Fill = System.Windows.Media.Brushes.Transparent
            }
        };

        chartRevenue.AxisX.Add(new Axis
        {
            Title = "Thứ trong tuần",
            Labels = new[] { "T2 (185M)", "T3 (192M)", "T4 (178M)", "T5 (215M)", "T6 (230M)", "T7 (228M)", "CN (248.5M)" }
        });

        chartRevenue.AxisY.Add(new Axis
        {
            Title = "Doanh thu (Triệu VNĐ)",
            LabelFormatter = value => value + "M"
        });

        pnlChartContainer.Controls.Add(chartRevenue);
        pnlChartContainer.Controls.Add(lblChartTitle);

        // --- 4. AI Cause Analysis Panel ---
        pnlAiInsightsContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130,
            BackColor = Color.White,
            Padding = new Padding(20),
            Margin = new Padding(0, 20, 0, 20)
        };

        var lblAiAnalysisHeader = new Label
        {
            Text = "🤖 AI GIẢI TRÌNH TỰ ĐỘNG NGUYÊN NHÂN BIẾN ĐỘNG (Độ tin cậy: 96.4%)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var lblAiAnalysisBody = new Label
        {
            Text = "“Doanh thu nhóm Nước giải khát & Bánh kẹo tăng vọt 28% do hiệu ứng thời tiết nắng gắt tại TP.HCM (đỉnh nhiệt 36.5°C) kết hợp khuyến mãi 'Mua 2 Tặng 1' toàn chuỗi. Nhóm rau củ quả tươi giảm nhẹ 6% do nguồn cung buổi chiều chậm trễ 45 phút từ chợ đầu mối Thủ Đức (đã điều phối bổ sung thành công).”",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(60, 70, 80),
            Size = new Size(1100, 50),
            Location = new Point(20, 45)
        };

        pnlAiInsightsContainer.Controls.Add(lblAiAnalysisHeader);
        pnlAiInsightsContainer.Controls.Add(lblAiAnalysisBody);

        this.Controls.Add(pnlAiInsightsContainer);
        this.Controls.Add(pnlChartContainer);
        this.Controls.Add(tlpKpis);
        this.Controls.Add(pnlHeaderBanner);
    }

    private Panel CreateKpiCard(string title, string value, string note, Color borderAccent, Color bg)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bg,
            Margin = new Padding(6),
            Padding = new Padding(15)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = borderAccent,
            AutoSize = true,
            Location = new Point(12, 10)
        };

        var lblVal = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 40, 50),
            AutoSize = true,
            Location = new Point(12, 35)
        };

        var lblNote = new Label
        {
            Text = note,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 110, 120),
            AutoSize = true,
            Location = new Point(12, 75)
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblVal);
        card.Controls.Add(lblNote);

        return card;
    }
}
