using System;
using System.Drawing;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;

namespace Desktop.Views;

public class ReportsView : UserControl
{
    private LiveCharts.WinForms.CartesianChart revenueChart = null!;

    public ReportsView()
    {
        InitializeComponent();
        LoadChart();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlHeader);

        var lblTitle = new Label
        {
            Text = "📊 BÁO CÁO PHÂN TÍCH DOANH THU & CHẤM CÔNG (ENTERPRISE REPORTS)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 18),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);

        var pnlChartContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15)
        };
        ThemeManager.ApplyCardPanel(pnlChartContainer);

        revenueChart = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg
        };

        pnlChartContainer.Controls.Add(revenueChart);

        this.Controls.Add(pnlChartContainer);
        this.Controls.Add(pnlHeader);
    }

    private void LoadChart()
    {
        var primaryMediaColor = System.Windows.Media.Color.FromArgb(
            AppTheme.PrimaryGreen.A, AppTheme.PrimaryGreen.R, AppTheme.PrimaryGreen.G, AppTheme.PrimaryGreen.B);
        var accentMediaColor = System.Windows.Media.Color.FromArgb(
            AppTheme.AccentOrange.A, AppTheme.AccentOrange.R, AppTheme.AccentOrange.G, AppTheme.AccentOrange.B);

        revenueChart.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Doanh thu POS (Triệu VNĐ)",
                Values = new ChartValues<double> { 45.5, 62.0, 58.2, 79.8, 85.0, 92.4, 110.0 },
                Stroke = new System.Windows.Media.SolidColorBrush(primaryMediaColor),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, AppTheme.PrimaryGreen.R, AppTheme.PrimaryGreen.G, AppTheme.PrimaryGreen.B)),
                PointGeometrySize = 10
            },
            new LineSeries
            {
                Title = "Lợi nhuận gộp (Triệu VNĐ)",
                Values = new ChartValues<double> { 12.0, 18.5, 15.0, 24.2, 28.0, 31.0, 39.5 },
                Stroke = new System.Windows.Media.SolidColorBrush(accentMediaColor),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, AppTheme.AccentOrange.R, AppTheme.AccentOrange.G, AppTheme.AccentOrange.B)),
                PointGeometrySize = 8
            }
        };

        revenueChart.AxisX.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Các ngày trong tuần",
            Labels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" }
        });

        revenueChart.AxisY.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Giá trị (Triệu VNĐ)",
            LabelFormatter = value => value.ToString("N0") + " Tr"
        });

        revenueChart.LegendLocation = LegendLocation.Right;
    }
}
