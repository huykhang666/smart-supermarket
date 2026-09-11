using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
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

    private Label lblRevenueValue = null!;
    private Label lblOrdersValue = null!;
    private Label lblCustomersValue = null!;
    private Label lblWarningValue = null!;
    private Label lblAiAnalysisBody = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public DashboardView()
    {
        InitializeComponent();
        LoadRealtimeDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.AutoScroll = true;
        this.Padding = new Padding(20);

        // --- 1. Top Realtime Banner Card ---
        pnlHeaderBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20, 12, 20, 12),
            Margin = new Padding(0, 0, 0, 20)
        };
        ThemeManager.ApplyCardPanel(pnlHeaderBanner);

        var lblBannerTitle = new Label
        {
            Text = "TRUNG TÂM ĐIỀU HÀNH ERP & AI INSIGHTS  •  REALTIME DỮ LIỆU THỰC",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.Primary,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblBannerSub = new Label
        {
            Text = "Dữ liệu truy vấn trực tiếp từ Cơ sở dữ liệu Hệ thống  •  Tự động cập nhật theo thời gian thực",
            Font = ThemeManager.SmallFont,
            ForeColor = ThemeManager.TextSecondary,
            AutoSize = true,
            Location = new Point(20, 42)
        };

        var btnRefreshRealtime = new Button
        {
            Text = "🔄 Làm Mới Tức Thì",
            Size = new Size(150, 36),
            Location = new Point(pnlHeaderBanner.Width - 170, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.ApplyPrimaryButton(btnRefreshRealtime);
        btnRefreshRealtime.Click += (s, e) => LoadRealtimeDataAsync();

        pnlHeaderBanner.Controls.Add(lblBannerTitle);
        pnlHeaderBanner.Controls.Add(lblBannerSub);
        pnlHeaderBanner.Controls.Add(btnRefreshRealtime);

        // --- 2. 4 Metric KPI Cards ---
        tlpKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 15, 0, 15)
        };
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        tlpKpis.Controls.Add(CreateDynamicKpiCard("DOANH THU HÔM NAY 💵", "0 đ", "▲ Cập nhật từ DB", ThemeManager.Primary, out lblRevenueValue), 0, 0);
        tlpKpis.Controls.Add(CreateDynamicKpiCard("TỔNG ĐƠN HÀNG POS 🛒", "0 đơn", "Đơn hàng bán lẻ hôm nay", ThemeManager.Success, out lblOrdersValue), 1, 0);
        tlpKpis.Controls.Add(CreateDynamicKpiCard("KHÁCH HÀNG LOYALTY 👥", "0 thành viên", "Tổng tài khoản KH", ThemeManager.Warning, out lblCustomersValue), 2, 0);
        tlpKpis.Controls.Add(CreateDynamicKpiCard("CẢNH BÁO KHO & HSD ⚠️", "0 mặt hàng", "Cận kho & sắp hết hạn", ThemeManager.Danger, out lblWarningValue), 3, 0);

        // --- 3. LiveChart Revenue Panel ---
        pnlChartContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 350,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20),
            Margin = new Padding(0, 15, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlChartContainer);

        var lblChartTitle = new Label
        {
            Text = "📈 Biểu Đồ Doanh Thu Thực Tế (7 Ngày Gần Nhất)",
            Font = ThemeManager.SubtitleFont,
            ForeColor = ThemeManager.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        chartRevenue = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill
        };

        pnlChartContainer.Controls.Add(chartRevenue);
        pnlChartContainer.Controls.Add(lblChartTitle);

        // --- 4. AI Cause Analysis Panel ---
        pnlAiInsightsContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20),
            Margin = new Padding(0, 15, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlAiInsightsContainer);

        var lblAiAnalysisHeader = new Label
        {
            Text = "🤖 AI GIẢI TRÌNH PHÂN TÍCH THEO DỮ LIỆU THỰC TE",
            Font = ThemeManager.SubtitleFont,
            ForeColor = ThemeManager.Primary,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        lblAiAnalysisBody = new Label
        {
            Text = "Đang tải phân tích dữ liệu thực từ Server Backend...",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = ThemeManager.TextSecondary,
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

    private Panel CreateDynamicKpiCard(string title, string initialVal, string note, Color badgeColor, out Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Margin = new Padding(8),
            Padding = new Padding(16)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var cardPath = ThemeManager.GetRoundedPath(new Rectangle(0, 0, card.Width - 3, card.Height - 3), 10);
            using var borderPen = new Pen(ThemeManager.Border, 1);
            e.Graphics.DrawPath(borderPen, cardPath);
            using var leftAccent = new SolidBrush(badgeColor);
            e.Graphics.FillRectangle(leftAccent, 0, 2, 5, card.Height - 6);
        };

        var lblTitle = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = ThemeManager.TextSecondary, Location = new Point(18, 12), AutoSize = true };
        valueLabel = new Label { Text = initialVal, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = ThemeManager.TextPrimary, Location = new Point(16, 34), AutoSize = true };
        var lblBadge = new Label { Text = note, Font = new Font("Segoe UI", 9f, FontStyle.Regular), ForeColor = badgeColor, Location = new Point(18, 72), AutoSize = true };

        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);
        card.Controls.Add(lblBadge);
        return card;
    }

    private async void LoadRealtimeDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/dashboard/summary");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    lblRevenueValue.Text = data.GetProperty("todayRevenue").GetString();
                    lblOrdersValue.Text = data.GetProperty("todayOrders").GetString();
                    lblCustomersValue.Text = data.GetProperty("totalCustomers").GetString();
                    lblWarningValue.Text = data.GetProperty("warningCount").GetString();
                    lblAiAnalysisBody.Text = data.GetProperty("aiAnalysis").GetString();

                    // Render Chart from real DB values
                    var labels = new List<string>();
                    var values = new ChartValues<double>();

                    if (data.TryGetProperty("chartLabels", out var arrLabels))
                    {
                        foreach (var l in arrLabels.EnumerateArray()) labels.Add(l.GetString() ?? "");
                    }

                    if (data.TryGetProperty("chartValues", out var arrVals))
                    {
                        foreach (var v in arrVals.EnumerateArray()) values.Add(v.GetDouble());
                    }

                    chartRevenue.Series.Clear();
                    chartRevenue.AxisX.Clear();
                    chartRevenue.AxisY.Clear();

                    chartRevenue.Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Doanh thu thực tế (VNĐ)",
                            Values = values,
                            PointGeometrySize = 8,
                            Stroke = System.Windows.Media.Brushes.RoyalBlue,
                            Fill = System.Windows.Media.Brushes.Transparent
                        }
                    };

                    chartRevenue.AxisX.Add(new LiveCharts.Wpf.Axis
                    {
                        Title = "Ngày",
                        Labels = labels.ToArray()
                    });

                    chartRevenue.AxisY.Add(new LiveCharts.Wpf.Axis
                    {
                        Title = "VNĐ",
                        LabelFormatter = val => val.ToString("N0") + "đ"
                    });
                }
            }
        }
        catch
        {
            lblAiAnalysisBody.Text = "Không thể kết nối đến Backend API server. Vui lòng kiểm tra tiến trình Backend.";
        }
    }
}
