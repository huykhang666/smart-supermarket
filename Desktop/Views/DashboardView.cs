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
        DataStore.OrderCompleted += (s, e) =>
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateLocalKpiMetrics()));
                return;
            }
            UpdateLocalKpiMetrics();
        };
        LoadRealtimeDataAsync();
    }

    private void UpdateLocalKpiMetrics()
    {
        lblRevenueValue.Text = $"{DataStore.TodayRevenue:N0} đ";
        lblOrdersValue.Text = $"{DataStore.TodayOrdersCount} đơn";
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
            Height = 88,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 0, 0, 10)
        };
        ThemeManager.ApplyCardPanel(pnlHeaderBanner);

        var pnlBannerRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        var btnRefreshRealtime = new Button
        {
            Text = "🔄 Làm Mới Dữ Liệu",
            Size = new Size(160, 36)
        };
        ThemeManager.ApplyPrimaryButton(btnRefreshRealtime);
        btnRefreshRealtime.Click += (s, e) => LoadRealtimeDataAsync();
        pnlBannerRight.Controls.Add(btnRefreshRealtime);

        var pnlBannerLeft = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var lblBannerTitle = new Label
        {
            Text = "TRUNG TÂM ĐIỀU HÀNH ERP & AI INSIGHTS  •  DỮ LIỆU THỰC TẾ",
            Font = AppTheme.FontH1,
            ForeColor = ThemeManager.Primary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblBannerSub = new Label
        {
            Text = "Dữ liệu truy vấn trực tiếp từ Cơ sở dữ liệu Hệ thống  •  Tự động đồng bộ theo thời gian thực",
            Font = AppTheme.FontBody,
            ForeColor = ThemeManager.TextSecondary,
            Location = new Point(0, 36),
            AutoSize = true
        };

        pnlBannerLeft.Controls.Add(lblBannerTitle);
        pnlBannerLeft.Controls.Add(lblBannerSub);

        pnlHeaderBanner.Controls.Add(pnlBannerLeft);
        pnlHeaderBanner.Controls.Add(pnlBannerRight);

        // --- 2. 4 Metric KPI Cards (Responsive Wrapping) ---
        tlpKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 10)
        };

        var card1 = CreateDynamicKpiCard("DOANH THU HÔM NAY 💵", "0 đ", "▲ Cập nhật từ DB", ThemeManager.Primary, out lblRevenueValue);
        var card2 = CreateDynamicKpiCard("TỔNG ĐƠN HÀNG POS 🛒", "0 đơn", "Đơn hàng bán lẻ hôm nay", ThemeManager.Success, out lblOrdersValue);
        var card3 = CreateDynamicKpiCard("KHÁCH HÀNG LOYALTY 👥", "0 thành viên", "Tổng tài khoản KH", ThemeManager.Warning, out lblCustomersValue);
        var card4 = CreateDynamicKpiCard("CẢNH BÁO KHO & HSD ⚠️", "0 mặt hàng", "Cận kho & sắp hết hạn", ThemeManager.Danger, out lblWarningValue);
        var kpiCards = new List<Control> { card1, card2, card3, card4 };

        AppTheme.EnableResponsiveKpiGrid(tlpKpis, kpiCards, this, 850, 440, 125);

        // --- 3. LiveChart Revenue Panel ---
        pnlChartContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 350,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(16),
            Margin = new Padding(0, 6, 0, 6)
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
            Height = 135,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 8, 0, 10)
        };
        ThemeManager.ApplyCardPanel(pnlAiInsightsContainer);

        var lblAiAnalysisHeader = new Label
        {
            Text = "🤖 AI GIẢI TRÌNH & PHÂN TÍCH THEO DỮ LIỆU THỰC TẾ",
            Font = AppTheme.FontH2,
            ForeColor = ThemeManager.Primary,
            AutoSize = true,
            Location = new Point(18, 14)
        };

        lblAiAnalysisBody = new Label
        {
            Text = "Đang tải phân tích dữ liệu thực từ Server Backend...",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = ThemeManager.TextSecondary,
            Location = new Point(18, 48),
            Size = new Size(pnlAiInsightsContainer.Width - 36, 68),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
            Margin = new Padding(6),
            Padding = new Padding(16, 12, 16, 12)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var cardPath = ThemeManager.GetRoundedPath(new Rectangle(0, 0, card.Width - 3, card.Height - 3), 8);
            using var borderPen = new Pen(ThemeManager.Border, 1);
            e.Graphics.DrawPath(borderPen, cardPath);
            using var leftAccent = new SolidBrush(badgeColor);
            e.Graphics.FillRectangle(leftAccent, 0, 4, 4, card.Height - 10);
        };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));

        var lblTitle = new Label
        {
            Text = title.ToUpper(),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ThemeManager.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        valueLabel = new Label
        {
            Text = initialVal,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ThemeManager.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var lblBadge = new Label
        {
            Text = note,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = badgeColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        tlp.Controls.Add(lblTitle, 0, 0);
        tlp.Controls.Add(valueLabel, 0, 1);
        tlp.Controls.Add(lblBadge, 0, 2);

        card.Controls.Add(tlp);
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
