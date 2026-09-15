using System;
using System.Drawing;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;

namespace Desktop.Views;

public class ReportsView : UserControl
{
    private TableLayoutPanel tlpKpis = null!;
    private LiveCharts.WinForms.CartesianChart revenueChart = null!;

    public ReportsView()
    {
        InitializeComponent();
        LoadChart();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Page Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Báo Cáo Phân Tích Doanh Thu & Hiệu Quả Kinh Doanh",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        // --- 2. 4 KPI Summary Cards ---
        tlpKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        tlpKpis.Controls.Add(AppTheme.CreateKpiCard("DOANH THU TUẦN", "0 đ", "Chưa có phát sinh", AppTheme.Primary), 0, 0);
        tlpKpis.Controls.Add(AppTheme.CreateKpiCard("LỢI NHUẬN GỘP", "0 đ", "Biên lãi gộp: 0%", AppTheme.Success), 1, 0);
        tlpKpis.Controls.Add(AppTheme.CreateKpiCard("TỔNG ĐƠN HÀNG", "0 đơn", "0 đơn hoàn tất", AppTheme.Primary), 2, 0);
        tlpKpis.Controls.Add(AppTheme.CreateKpiCard("GIÁ TRỊ TB / ĐƠN", "0 đ", "0 đ / đơn", AppTheme.Warning), 3, 0);

        // --- 3. Chart Container Card ---
        var pnlChartContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16)
        };
        AppTheme.ApplyCardPanel(pnlChartContainer);

        var lblChartTitle = new Label
        {
            Text = "Xu Hướng Doanh Thu & Lợi Nhuận Tuần (VNĐ)",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        pnlChartContainer.Controls.Add(lblChartTitle);

        revenueChart = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite
        };

        pnlChartContainer.Controls.Add(revenueChart);

        this.Controls.Add(pnlChartContainer);
        this.Controls.Add(tlpKpis);
        this.Controls.Add(pnlHeader);
    }

    private void LoadChart()
    {
        var primaryMediaColor = System.Windows.Media.Color.FromArgb(
            AppTheme.Primary.A, AppTheme.Primary.R, AppTheme.Primary.G, AppTheme.Primary.B);
        var warningMediaColor = System.Windows.Media.Color.FromArgb(
            AppTheme.Warning.A, AppTheme.Warning.R, AppTheme.Warning.G, AppTheme.Warning.B);

        revenueChart.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Doanh thu POS (VNĐ)",
                Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                Stroke = new System.Windows.Media.SolidColorBrush(primaryMediaColor),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, AppTheme.Primary.R, AppTheme.Primary.G, AppTheme.Primary.B)),
                PointGeometrySize = 6
            },
            new LineSeries
            {
                Title = "Lợi nhuận gộp (VNĐ)",
                Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                Stroke = new System.Windows.Media.SolidColorBrush(warningMediaColor),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, AppTheme.Warning.R, AppTheme.Warning.G, AppTheme.Warning.B)),
                PointGeometrySize = 6
            }
        };

        revenueChart.AxisX.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Các ngày trong tuần",
            Labels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" }
        });

        revenueChart.AxisY.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Giá trị (VNĐ)",
            LabelFormatter = value => value.ToString("N0") + " đ"
        });

        revenueChart.LegendLocation = LegendLocation.Right;
    }
}
