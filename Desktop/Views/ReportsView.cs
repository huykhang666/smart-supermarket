using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop.Services;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;

namespace Desktop.Views;

public class ReportsView : UserControl
{
    private TabControl tabReports = null!;

    // ==========================================
    // TAB 1: DOANH THU & LỢI NHUẬN GỘP
    // ==========================================
    private TabPage tabRevenue = null!;
    private DateTimePicker dtpRevStart = null!;
    private DateTimePicker dtpRevEnd = null!;
    private Button btnFilterRev = null!;
    private Button btnExportRevExcel = null!;
    private TableLayoutPanel tlpRevKpis = null!;
    private Label lblRevTotalVal = null!;
    private Label lblRevTotalNote = null!;
    private Label lblRevProfitVal = null!;
    private Label lblRevProfitNote = null!;
    private Label lblRevOrdersVal = null!;
    private Label lblRevOrdersNote = null!;
    private Label lblRevAvgVal = null!;
    private Label lblRevAvgNote = null!;
    private LiveCharts.WinForms.CartesianChart revenueChart = null!;
    private DataGridView dgvDailyTrends = null!;
    private Label lblPaymentCash = null!;
    private Label lblPaymentQr = null!;
    private Label lblPaymentCard = null!;

    // ==========================================
    // TAB 2: TOP BÁN CHẠY NHẤT
    // ==========================================
    private TabPage tabBestSellers = null!;
    private ComboBox cboTopN = null!;
    private ComboBox cboTopDays = null!;
    private Button btnFilterTop = null!;
    private Button btnExportTopExcel = null!;
    private DataGridView dgvBestSellers = null!;
    private Label lblTopSummary = null!;

    // ==========================================
    // TAB 3: TỒN KHO & CẬN HSD
    // ==========================================
    private TabPage tabExpiry = null!;
    private ComboBox cboExpiryDays = null!;
    private Button btnFilterExpiry = null!;
    private Button btnExportExpiryExcel = null!;
    private TableLayoutPanel tlpExpiryKpis = null!;
    private Label lblExpiredVal = null!;
    private Label lblExpiredNote = null!;
    private Label lblExpiringVal = null!;
    private Label lblExpiringNote = null!;
    private Label lblSlowVal = null!;
    private Label lblSlowNote = null!;
    private TabControl tabExpirySub = null!;
    private DataGridView dgvExpiringBatches = null!;
    private DataGridView dgvSlowMoving = null!;

    // ==========================================
    // TAB 4: CA TRỰC & ĐỐI SOÁT KÉT TIỀN
    // ==========================================
    private TabPage tabCashierShift = null!;
    private DateTimePicker dtpShiftDate = null!;
    private Button btnFilterShift = null!;
    private Button btnExportShiftExcel = null!;
    private Button btnPrintZReportPdf = null!;
    private TableLayoutPanel tlpShiftKpis = null!;
    private Label lblShiftRevVal = null!;
    private Label lblShiftRevNote = null!;
    private Label lblShiftCashVal = null!;
    private Label lblShiftCashNote = null!;
    private Label lblShiftQrVal = null!;
    private Label lblShiftQrNote = null!;
    private Label lblShiftCardVal = null!;
    private Label lblShiftCardNote = null!;
    private DataGridView dgvCashiers = null!;
    private DataGridView dgvShifts = null!;

    // Cache latest shift report for PDF printing
    private decimal _lastShiftRevenue = 0;
    private int _lastShiftOrders = 0;
    private decimal _lastShiftCash = 0;
    private decimal _lastShiftQr = 0;
    private decimal _lastShiftCard = 0;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public ReportsView()
    {
        InitializeComponent();
        this.Load += async (s, e) =>
        {
            await LoadRevenueReportAsync();
            await LoadBestSellersAsync();
            await LoadExpiryReportAsync();
            await LoadCashierShiftReportAsync();
        };
    }

    private Task ExecuteWithButtonLoadingAsync(Button btn, string loadingText, Func<Task> action)
        => AppTheme.ExecuteWithLoadingAsync(btn, loadingText, action);

    private void ExecuteWithButtonLoading(Button btn, string loadingText, Action action)
        => AppTheme.ExecuteWithLoading(btn, loadingText, action);

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(20);

        // --- 1. Top Header Banner ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📊 HỆ THỐNG BÁO CÁO PHÂN TÍCH & ĐỐI SOÁT KINH DOANH",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 2),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Theo dõi doanh thu, lãi gộp, top sản phẩm bán chạy, cảnh báo cận date và đối soát kết ca thu ngân",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 28),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Main TabControl (4 Core Reports) ---
        tabReports = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold,
            Padding = new Point(16, 8)
        };

        InitializeTabRevenue();
        InitializeTabBestSellers();
        InitializeTabExpiry();
        InitializeTabCashierShift();

        tabReports.TabPages.Add(tabRevenue);
        tabReports.TabPages.Add(tabBestSellers);
        tabReports.TabPages.Add(tabExpiry);
        tabReports.TabPages.Add(tabCashierShift);

        this.Controls.Add(tabReports);
        this.Controls.Add(pnlHeader);
    }

    // =========================================================================
    // 1. TAB DOANH THU & LỢI NHUẬN GỘP
    // =========================================================================
    private void InitializeTabRevenue()
    {
        tabRevenue = new TabPage("  📈 Doanh Thu & Lợi Nhuận Gộp  ")
        {
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(12)
        };

        // Toolbar
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        var lblFrom = new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        dtpRevStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 115, Value = DateTime.Today.AddDays(-6), Margin = new Padding(4, 5, 8, 4) };

        var lblTo = new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        dtpRevEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 115, Value = DateTime.Today, Margin = new Padding(4, 5, 8, 4) };

        btnFilterRev = new Button { Text = "🔄 Lọc Báo Cáo", Size = new Size(125, 32), Margin = new Padding(4, 3, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnFilterRev);
        btnFilterRev.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnFilterRev, "Đang tải...", () => LoadRevenueReportAsync());

        var btnQuick7 = new Button { Text = "7 Ngày Qua", Size = new Size(95, 32), Margin = new Padding(2, 3, 4, 4) };
        AppTheme.ApplySecondaryButton(btnQuick7);
        btnQuick7.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnQuick7, "Đang tải...", async () =>
        {
            dtpRevStart.Value = DateTime.Today.AddDays(-6);
            dtpRevEnd.Value = DateTime.Today;
            await LoadRevenueReportAsync();
        });

        var btnQuick30 = new Button { Text = "30 Ngày Qua", Size = new Size(100, 32), Margin = new Padding(2, 3, 8, 4) };
        AppTheme.ApplySecondaryButton(btnQuick30);
        btnQuick30.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnQuick30, "Đang tải...", async () =>
        {
            dtpRevStart.Value = DateTime.Today.AddDays(-29);
            dtpRevEnd.Value = DateTime.Today;
            await LoadRevenueReportAsync();
        });

        btnExportRevExcel = new Button { Text = "📥 Xuất Excel", Size = new Size(120, 32), Margin = new Padding(8, 3, 4, 4) };
        AppTheme.ApplySuccessButton(btnExportRevExcel);
        btnExportRevExcel.Click += (s, e) => ExecuteWithButtonLoading(btnExportRevExcel, "Đang xuất...", () => ExcelExportService.ExportDataGridViewToExcel(dgvDailyTrends, "Báo Cáo Doanh Thu & Lợi Nhuận Gộp Theo Ngày", "BaoCao_DoanhThu_LoiNhuan"));

        pnlToolbar.Controls.Add(lblFrom);
        pnlToolbar.Controls.Add(dtpRevStart);
        pnlToolbar.Controls.Add(lblTo);
        pnlToolbar.Controls.Add(dtpRevEnd);
        pnlToolbar.Controls.Add(btnFilterRev);
        pnlToolbar.Controls.Add(btnQuick7);
        pnlToolbar.Controls.Add(btnQuick30);
        pnlToolbar.Controls.Add(btnExportRevExcel);

        // 4 KPI Summary Cards
        tlpRevKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6)
        };

        var c1 = AppTheme.CreateKpiCard("DOANH THU THỰC TẾ", "0 đ", "Đang tải dữ liệu...", AppTheme.Primary, out lblRevTotalVal, out lblRevTotalNote);
        var c2 = AppTheme.CreateKpiCard("LỢI NHUẬN GỘP", "0 đ", "Biên lãi gộp: 0%", AppTheme.Success, out lblRevProfitVal, out lblRevProfitNote);
        var c3 = AppTheme.CreateKpiCard("TỔNG ĐƠN HÀNG", "0 đơn", "0 đơn hoàn tất", AppTheme.Primary, out lblRevOrdersVal, out lblRevOrdersNote);
        var c4 = AppTheme.CreateKpiCard("GIÁ TRỊ TB / ĐƠN", "0 đ", "Khuyến mãi: 0 đ", AppTheme.Warning, out lblRevAvgVal, out lblRevAvgNote);

        AppTheme.EnableResponsiveKpiGrid(tlpRevKpis, new List<Control> { c1, c2, c3, c4 }, tabRevenue, 850, 440, 105);

        // Main Content Split: Chart & Payment Cards on Top, Grid at Bottom
        var pnlRevContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        // Split container for Chart + Breakdown & Grid
        var splitRev = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 270,
            SplitterWidth = 6,
            BackColor = Color.Transparent
        };

        // Chart Container
        var pnlChartCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12)
        };
        AppTheme.ApplyCardPanel(pnlChartCard);

        var pnlPaymentBreakdown = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 240,
            BackColor = Color.FromArgb(0xFA, 0xFC, 0xFF),
            Padding = new Padding(10),
            FlowDirection = FlowDirection.TopDown
        };

        var lblPayTitle = new Label { Text = "💳 HÌNH THỨC THANH TOÁN", Font = AppTheme.FontH3, ForeColor = AppTheme.TextPrimary, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        lblPaymentCash = new Label { Text = "💵 Tiền mặt: 0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Primary, AutoSize = true, Margin = new Padding(0, 4, 0, 8) };
        lblPaymentQr = new Label { Text = "📱 VietQR / App: 0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Success, AutoSize = true, Margin = new Padding(0, 4, 0, 8) };
        lblPaymentCard = new Label { Text = "💳 Quẹt thẻ POS: 0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Warning, AutoSize = true, Margin = new Padding(0, 4, 0, 8) };

        pnlPaymentBreakdown.Controls.Add(lblPayTitle);
        pnlPaymentBreakdown.Controls.Add(lblPaymentCash);
        pnlPaymentBreakdown.Controls.Add(lblPaymentQr);
        pnlPaymentBreakdown.Controls.Add(lblPaymentCard);

        revenueChart = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite
        };

        pnlChartCard.Controls.Add(revenueChart);
        pnlChartCard.Controls.Add(pnlPaymentBreakdown);
        splitRev.Panel1.Controls.Add(pnlChartCard);

        // DataGridView Trends
        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12)
        };
        AppTheme.ApplyCardPanel(pnlGridCard);

        var lblGridTitle = new Label
        {
            Text = "📋 Bảng Thống Kê Doanh Số & Chi Phí Từng Ngày",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        dgvDailyTrends = new DataGridView();
        AppTheme.ApplyGridStyle(dgvDailyTrends);
        SetupDailyTrendsColumns();

        pnlGridCard.Controls.Add(dgvDailyTrends);
        pnlGridCard.Controls.Add(lblGridTitle);
        splitRev.Panel2.Controls.Add(pnlGridCard);

        pnlRevContent.Controls.Add(splitRev);

        tabRevenue.Controls.Add(pnlRevContent);
        tabRevenue.Controls.Add(tlpRevKpis);
        tabRevenue.Controls.Add(pnlToolbar);
    }

    private void SetupDailyTrendsColumns()
    {
        dgvDailyTrends.Columns.Clear();
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày Bán", DataPropertyName = "Date", Width = 110 });
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Đơn Hàng", DataPropertyName = "OrderCount", Width = 90 });
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Doanh Thu Thực Tế (VNĐ)", DataPropertyName = "Revenue", Width = 170 });
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá Vốn Hàng Bán (VNĐ)", DataPropertyName = "Cost", Width = 170 });
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Lợi Nhuận Gộp (VNĐ)", DataPropertyName = "Profit", Width = 170 });
        dgvDailyTrends.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tỷ Suất Lãi Gộp", DataPropertyName = "Margin", Width = 110 });
    }

    private async Task LoadRevenueReportAsync()
    {
        try
        {
            string url = $"{_apiBaseUrl}/api/v1/reports/revenue-profit?startDate={dtpRevStart.Value:yyyy-MM-dd}&endDate={dtpRevEnd.Value:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data)) return;

            decimal totalRev = data.GetProperty("totalRevenue").GetDecimal();
            decimal totalCost = data.GetProperty("totalCost").GetDecimal();
            decimal grossProfit = data.GetProperty("grossProfit").GetDecimal();
            decimal profitMargin = data.GetProperty("profitMargin").GetDecimal();
            int totalOrders = data.GetProperty("totalOrders").GetInt32();
            decimal avgOrder = data.GetProperty("averageOrderValue").GetDecimal();
            decimal totalDiscount = data.GetProperty("totalDiscount").GetDecimal();

            lblRevTotalVal.Text = $"{totalRev:N0} đ";
            lblRevTotalNote.Text = $"Giá vốn: {totalCost:N0} đ";

            lblRevProfitVal.Text = $"{grossProfit:N0} đ";
            decimal totalLoss = data.TryGetProperty("totalLossAmount", out var tl) ? tl.GetDecimal() : 0m;
            lblRevProfitNote.Text = totalLoss > 0
                ? $"Biên lãi: {profitMargin:N2}% (Hao hụt kho: -{totalLoss:N0} đ)"
                : $"Biên lãi gộp: {profitMargin:N2}%";

            lblRevOrdersVal.Text = $"{totalOrders:N0} đơn";
            lblRevOrdersNote.Text = $"{totalOrders:N0} đơn hoàn tất";

            lblRevAvgVal.Text = $"{avgOrder:N0} đ";
            lblRevAvgNote.Text = $"Khuyến mãi: {totalDiscount:N0} đ";

            // Payment breakdown
            if (data.TryGetProperty("paymentBreakdown", out var pb))
            {
                decimal cash = pb.GetProperty("cash").GetDecimal();
                int cashOrders = pb.GetProperty("cashOrders").GetInt32();
                decimal qr = pb.GetProperty("qrCode").GetDecimal();
                int qrOrders = pb.GetProperty("qrOrders").GetInt32();
                decimal card = pb.GetProperty("creditCard").GetDecimal();
                int cardOrders = pb.GetProperty("cardOrders").GetInt32();

                lblPaymentCash.Text = $"💵 Tiền mặt: {cash:N0} đ ({cashOrders} đơn)";
                lblPaymentQr.Text = $"📱 Chuyển khoản QR: {qr:N0} đ ({qrOrders} đơn)";
                lblPaymentCard.Text = $"💳 Thẻ ngân hàng: {card:N0} đ ({cardOrders} đơn)";
            }

            // Daily Trends Chart & Grid
            dgvDailyTrends.Rows.Clear();
            var chartLabels = new List<string>();
            var revValues = new ChartValues<double>();
            var profitValues = new ChartValues<double>();

            if (data.TryGetProperty("dailyTrends", out var trendsArr))
            {
                foreach (var t in trendsArr.EnumerateArray())
                {
                    string date = t.GetProperty("date").GetString() ?? "";
                    decimal rev = t.GetProperty("revenue").GetDecimal();
                    decimal cost = t.GetProperty("cost").GetDecimal();
                    decimal profit = t.GetProperty("profit").GetDecimal();
                    int orders = t.GetProperty("orderCount").GetInt32();
                    decimal margin = rev > 0 ? Math.Round((profit / rev) * 100m, 1) : 0m;

                    chartLabels.Add(date.Length >= 5 ? date.Substring(0, 5) : date);
                    revValues.Add((double)rev);
                    profitValues.Add((double)profit);

                    dgvDailyTrends.Rows.Add(
                        date,
                        $"{orders} đơn",
                        $"{rev:N0} đ",
                        $"{cost:N0} đ",
                        $"{profit:N0} đ",
                        $"{margin:N1}%"
                    );
                }
            }

            // Bind LiveChart
            revenueChart.Series.Clear();
            revenueChart.AxisX.Clear();
            revenueChart.AxisY.Clear();

            revenueChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Doanh thu (VNĐ)",
                    Values = revValues,
                    PointGeometrySize = 7,
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0x00, 0x87, 0xE6)),
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 0x00, 0x87, 0xE6))
                },
                new LineSeries
                {
                    Title = "Lợi nhuận gộp (VNĐ)",
                    Values = profitValues,
                    PointGeometrySize = 7,
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0x10, 0x7C, 0x10)),
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 0x10, 0x7C, 0x10))
                }
            };

            revenueChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Ngày",
                Labels = chartLabels.ToArray()
            });

            revenueChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                Title = "VNĐ",
                LabelFormatter = val => val.ToString("N0") + " đ"
            });

            revenueChart.LegendLocation = LegendLocation.Top;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải báo cáo doanh thu: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // =========================================================================
    // 2. TAB TOP SẢN PHẨM BÁN CHẠY NHẤT
    // =========================================================================
    private void InitializeTabBestSellers()
    {
        tabBestSellers = new TabPage("  🏆 Top Sản Phẩm Bán Chạy  ")
        {
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(12)
        };

        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        var lblTop = new Label { Text = "Hiển thị:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        cboTopN = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95, Margin = new Padding(4, 5, 8, 4) };
        cboTopN.Items.AddRange(new object[] { "Top 10", "Top 20", "Top 50" });
        cboTopN.SelectedIndex = 0;

        var lblDays = new Label { Text = "Khoảng thời gian:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        cboTopDays = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 115, Margin = new Padding(4, 5, 8, 4) };
        cboTopDays.Items.AddRange(new object[] { "7 Ngày qua", "15 Ngày qua", "30 Ngày qua", "45 Ngày qua" });
        cboTopDays.SelectedIndex = 0;

        btnFilterTop = new Button { Text = "🔄 Xem Bảng Xếp Hạng", Size = new Size(160, 32), Margin = new Padding(4, 3, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnFilterTop);
        btnFilterTop.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnFilterTop, "Đang tải...", () => LoadBestSellersAsync());

        btnExportTopExcel = new Button { Text = "📥 Xuất Excel Top Bán Chạy", Size = new Size(180, 32), Margin = new Padding(8, 3, 4, 4) };
        AppTheme.ApplySuccessButton(btnExportTopExcel);
        btnExportTopExcel.Click += (s, e) => ExecuteWithButtonLoading(btnExportTopExcel, "Đang xuất...", () => ExcelExportService.ExportDataGridViewToExcel(dgvBestSellers, "Báo Cáo Top Sản Phẩm Bán Chạy Nhất", "BaoCao_TopBanChay"));

        lblTopSummary = new Label
        {
            Text = "Đang tải dữ liệu...",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(12, 8, 4, 4),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(lblTop);
        pnlToolbar.Controls.Add(cboTopN);
        pnlToolbar.Controls.Add(lblDays);
        pnlToolbar.Controls.Add(cboTopDays);
        pnlToolbar.Controls.Add(btnFilterTop);
        pnlToolbar.Controls.Add(btnExportTopExcel);
        pnlToolbar.Controls.Add(lblTopSummary);

        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12)
        };
        AppTheme.ApplyCardPanel(pnlGridCard);

        dgvBestSellers = new DataGridView();
        AppTheme.ApplyGridStyle(dgvBestSellers);
        SetupBestSellersColumns();

        pnlGridCard.Controls.Add(dgvBestSellers);

        tabBestSellers.Controls.Add(pnlGridCard);
        tabBestSellers.Controls.Add(pnlToolbar);
    }

    private void SetupBestSellersColumns()
    {
        dgvBestSellers.Columns.Clear();
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", Width = 45 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Vạch", DataPropertyName = "Barcode", Width = 130 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Sản Phẩm", DataPropertyName = "ProductName", Width = 280 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngành Hàng", DataPropertyName = "CategoryName", Width = 170 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng Đã Bán", DataPropertyName = "QuantitySold", Width = 130 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Doanh Thu Đóng Góp (VNĐ)", DataPropertyName = "TotalRevenue", Width = 180 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tồn Kho Hiện Tại", DataPropertyName = "CurrentStock", Width = 130 });
        dgvBestSellers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đánh Giá Tiêu Thụ", Width = 140 });
    }

    private async Task LoadBestSellersAsync()
    {
        try
        {
            int top = cboTopN.SelectedIndex switch { 1 => 20, 2 => 50, _ => 10 };
            int days = cboTopDays.SelectedIndex switch { 1 => 15, 2 => 30, 3 => 45, _ => 7 };

            string url = $"{_apiBaseUrl}/api/v1/reports/best-sellers?top={top}&days={days}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var listArr)) return;

            dgvBestSellers.Rows.Clear();
            int rank = 1;
            int totalSold = 0;
            decimal totalRev = 0;

            foreach (var item in listArr.EnumerateArray())
            {
                string barcode = item.GetProperty("barcode").GetString() ?? "";
                string name = item.GetProperty("productName").GetString() ?? "";
                string cat = item.GetProperty("categoryName").GetString() ?? "";
                int sold = item.GetProperty("quantitySold").GetInt32();
                decimal rev = item.GetProperty("totalRevenue").GetDecimal();
                int stock = item.GetProperty("currentStock").GetInt32();

                totalSold += sold;
                totalRev += rev;

                string badge = rank switch
                {
                    1 => "🥇 Quán Quân Bán Chạy",
                    2 => "🥈 Á Quân Doanh Số",
                    3 => "🥉 Top 3 Tiêu Thụ",
                    _ => stock <= 15 ? "⚠️ Tồn Thấp Cần Nhập" : "✅ Tiêu Thụ Tốt"
                };

                dgvBestSellers.Rows.Add(
                    rank,
                    barcode,
                    name,
                    cat,
                    $"{sold:N0} cái",
                    $"{rev:N0} đ",
                    $"{stock:N0} cái",
                    badge
                );

                rank++;
            }

            lblTopSummary.Text = $"Tổng sản lượng: {totalSold:N0} cái | Tổng doanh thu nhóm: {totalRev:N0} đ";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải top bán chạy: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // =========================================================================
    // 3. TAB TỒN KHO & CẬN HẠN SỬ DỤNG
    // =========================================================================
    private void InitializeTabExpiry()
    {
        tabExpiry = new TabPage("  ⚠️ Tồn Kho & Cận Date  ")
        {
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(12)
        };

        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        var lblThresh = new Label { Text = "Ngưỡng cận HSD:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        cboExpiryDays = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(4, 5, 8, 4) };
        cboExpiryDays.Items.AddRange(new object[] { "Cận hạn ≤ 5 ngày", "Cận hạn ≤ 15 ngày", "Cận hạn ≤ 30 ngày" });
        cboExpiryDays.SelectedIndex = 0;

        btnFilterExpiry = new Button { Text = "🔄 Quét Hàng Cận Hạn", Size = new Size(160, 32), Margin = new Padding(4, 3, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnFilterExpiry);
        btnFilterExpiry.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnFilterExpiry, "Đang quét...", () => LoadExpiryReportAsync());

        btnExportExpiryExcel = new Button { Text = "📥 Xuất Danh Sách Cận HSD (Excel)", Size = new Size(220, 32), Margin = new Padding(8, 3, 4, 4) };
        AppTheme.ApplySuccessButton(btnExportExpiryExcel);
        btnExportExpiryExcel.Click += (s, e) => ExecuteWithButtonLoading(btnExportExpiryExcel, "Đang xuất...", () =>
        {
            if (tabExpirySub.SelectedIndex == 0)
                ExcelExportService.ExportDataGridViewToExcel(dgvExpiringBatches, "Danh Sách Lô Hàng Cận & Quá Hạn Sử Dụng", "BaoCao_CanHan_SuDung");
            else
                ExcelExportService.ExportDataGridViewToExcel(dgvSlowMoving, "Danh Sách Hàng Tồn Kho Chậm Luân Chuyển", "BaoCao_HangChamLuanChuyen");
        });

        pnlToolbar.Controls.Add(lblThresh);
        pnlToolbar.Controls.Add(cboExpiryDays);
        pnlToolbar.Controls.Add(btnFilterExpiry);
        pnlToolbar.Controls.Add(btnExportExpiryExcel);

        // 3 Summary Cards
        tlpExpiryKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6)
        };

        var c1 = AppTheme.CreateKpiCard("HÀNG ĐÃ HẾT HẠN", "0 SP", "Cần tiêu hủy / thu hồi", AppTheme.Danger, out lblExpiredVal, out lblExpiredNote);
        var c2 = AppTheme.CreateKpiCard("HÀNG CẬN HSD NGUY CẤP", "0 SP", "Cần dán tem giảm giá xả hàng", AppTheme.Warning, out lblExpiringVal, out lblExpiringNote);
        var c3 = AppTheme.CreateKpiCard("HÀNG CHẬM LUÂN CHUYỂN", "0 SKU", "Tồn > 10, bán chậm 30 ngày", AppTheme.Primary, out lblSlowVal, out lblSlowNote);

        AppTheme.EnableResponsiveKpiGrid(tlpExpiryKpis, new List<Control> { c1, c2, c3 }, tabExpiry, 850, 440, 105);

        // 2 Sub-Tabs for Expiring Batches vs Slow Moving
        tabExpirySub = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        var subPage1 = new TabPage("  ⏳ Lô Hàng Cận & Quá Hạn Sử Dụng  ") { BackColor = AppTheme.SurfaceWhite, Padding = new Padding(8) };
        dgvExpiringBatches = new DataGridView();
        AppTheme.ApplyGridStyle(dgvExpiringBatches);
        SetupExpiringBatchesColumns();
        subPage1.Controls.Add(dgvExpiringBatches);

        var subPage2 = new TabPage("  📦 Hàng Tồn Kho Chậm Luân Chuyển  ") { BackColor = AppTheme.SurfaceWhite, Padding = new Padding(8) };
        dgvSlowMoving = new DataGridView();
        AppTheme.ApplyGridStyle(dgvSlowMoving);
        SetupSlowMovingColumns();
        subPage2.Controls.Add(dgvSlowMoving);

        tabExpirySub.TabPages.Add(subPage1);
        tabExpirySub.TabPages.Add(subPage2);

        tabExpiry.Controls.Add(tabExpirySub);
        tabExpiry.Controls.Add(tlpExpiryKpis);
        tabExpiry.Controls.Add(pnlToolbar);
    }

    private void SetupExpiringBatchesColumns()
    {
        dgvExpiringBatches.Columns.Clear();
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Lô", DataPropertyName = "BatchCode", Width = 110 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Sản Phẩm", DataPropertyName = "ProductName", Width = 260 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Vạch", DataPropertyName = "Barcode", Width = 130 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngành Hàng", DataPropertyName = "CategoryName", Width = 150 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng Lô", DataPropertyName = "Quantity", Width = 110 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hạn Sử Dụng", DataPropertyName = "ExpiryDate", Width = 120 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Ngày Còn Lại", DataPropertyName = "DaysRemaining", Width = 130 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Vị Trí Kho / Kệ", DataPropertyName = "StorageLocation", Width = 130 });
        dgvExpiringBatches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái Cảnh Báo", DataPropertyName = "Status", Width = 180 });
    }

    private void SetupSlowMovingColumns()
    {
        dgvSlowMoving.Columns.Clear();
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã SP", DataPropertyName = "ProductId", Width = 70 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Vạch", DataPropertyName = "Barcode", Width = 130 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Sản Phẩm", DataPropertyName = "ProductName", Width = 280 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngành Hàng", DataPropertyName = "CategoryName", Width = 160 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tồn Kho Hiện Tại", DataPropertyName = "QuantityOnHand", Width = 130 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đã Bán (30 Ngày)", DataPropertyName = "SoldLast30Days", Width = 130 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đơn Giá Bán", DataPropertyName = "Price", Width = 130 });
        dgvSlowMoving.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá Trị Tồn Đọng (VNĐ)", DataPropertyName = "InventoryValue", Width = 170 });
    }

    private async Task LoadExpiryReportAsync()
    {
        try
        {
            int threshold = cboExpiryDays.SelectedIndex switch { 1 => 15, 2 => 30, _ => 5 };
            string url = $"{_apiBaseUrl}/api/v1/reports/inventory-expiry?daysThreshold={threshold}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data)) return;

            int expired = data.GetProperty("totalExpiredItems").GetInt32();
            int expiring = data.GetProperty("totalExpiringItems").GetInt32();
            int slow = data.GetProperty("totalSlowMovingItems").GetInt32();

            lblExpiredVal.Text = $"{expired:N0} SP";
            lblExpiringVal.Text = $"{expiring:N0} SP";
            lblSlowVal.Text = $"{slow:N0} SKU";

            // Bind Expiring Batches
            dgvExpiringBatches.Rows.Clear();
            if (data.TryGetProperty("expiringBatches", out var batchesArr))
            {
                foreach (var b in batchesArr.EnumerateArray())
                {
                    string batchCode = b.GetProperty("batchCode").GetString() ?? "";
                    string pName = b.GetProperty("productName").GetString() ?? "";
                    string barcode = b.GetProperty("barcode").GetString() ?? "";
                    string cat = b.GetProperty("categoryName").GetString() ?? "";
                    int qty = b.GetProperty("quantity").GetInt32();
                    string expDate = b.GetProperty("expiryDate").GetDateTime().ToString("dd/MM/yyyy");
                    int daysLeft = b.GetProperty("daysRemaining").GetInt32();
                    string loc = b.GetProperty("storageLocation").GetString() ?? "";
                    string status = b.GetProperty("status").GetString() ?? "";

                    int rowIdx = dgvExpiringBatches.Rows.Add(
                        batchCode,
                        pName,
                        barcode,
                        cat,
                        $"{qty:N0} cái",
                        expDate,
                        daysLeft < 0 ? $"Quá hạn {Math.Abs(daysLeft)} ngày" : $"Còn {daysLeft} ngày",
                        loc,
                        status
                    );

                    // Color code rows
                    if (daysLeft < 0)
                    {
                        dgvExpiringBatches.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xEB, 0xEE);
                        dgvExpiringBatches.Rows[rowIdx].DefaultCellStyle.ForeColor = AppTheme.Danger;
                    }
                    else if (daysLeft <= 5)
                    {
                        dgvExpiringBatches.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xF8, 0xE1);
                        dgvExpiringBatches.Rows[rowIdx].DefaultCellStyle.ForeColor = AppTheme.Warning;
                    }
                }
            }

            // Bind Slow Moving Items
            dgvSlowMoving.Rows.Clear();
            if (data.TryGetProperty("slowMovingItems", out var slowArr))
            {
                foreach (var sItem in slowArr.EnumerateArray())
                {
                    int pId = sItem.GetProperty("productId").GetInt32();
                    string barcode = sItem.GetProperty("barcode").GetString() ?? "";
                    string pName = sItem.GetProperty("productName").GetString() ?? "";
                    string cat = sItem.GetProperty("categoryName").GetString() ?? "";
                    int onHand = sItem.GetProperty("quantityOnHand").GetInt32();
                    int sold30 = sItem.GetProperty("soldLast30Days").GetInt32();
                    decimal price = sItem.GetProperty("price").GetDecimal();
                    decimal val = sItem.GetProperty("inventoryValue").GetDecimal();

                    dgvSlowMoving.Rows.Add(
                        pId,
                        barcode,
                        pName,
                        cat,
                        $"{onHand:N0} cái",
                        $"{sold30:N0} cái",
                        $"{price:N0} đ",
                        $"{val:N0} đ"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải báo cáo cận date: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // =========================================================================
    // 4. TAB CA TRỰC & ĐỐI SOÁT KÉT TIỀN
    // =========================================================================
    private void InitializeTabCashierShift()
    {
        tabCashierShift = new TabPage("  ⏱️ Ca Trực & Đối Soát Két  ")
        {
            BackColor = AppTheme.BackgroundGray,
            Padding = new Padding(12)
        };

        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        var lblDate = new Label { Text = "Ngày kiểm tra ca:", AutoSize = true, Margin = new Padding(4, 8, 4, 4), Font = AppTheme.FontBodyBold };
        dtpShiftDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 115, Value = DateTime.Today, Margin = new Padding(4, 5, 8, 4) };

        btnFilterShift = new Button { Text = "🔄 Xem Báo Cáo Ca", Size = new Size(150, 32), Margin = new Padding(4, 3, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnFilterShift);
        btnFilterShift.Click += async (s, e) => await ExecuteWithButtonLoadingAsync(btnFilterShift, "Đang tải...", () => LoadCashierShiftReportAsync());

        btnExportShiftExcel = new Button { Text = "📥 Xuất Excel Ca", Size = new Size(130, 32), Margin = new Padding(8, 3, 4, 4) };
        AppTheme.ApplySuccessButton(btnExportShiftExcel);
        btnExportShiftExcel.Click += (s, e) => ExecuteWithButtonLoading(btnExportShiftExcel, "Đang xuất...", () => ExcelExportService.ExportDataGridViewToExcel(dgvCashiers, $"Báo Cáo Doanh Thu Thu Ngân Ngày {dtpShiftDate.Value:dd/MM/yyyy}", "BaoCao_CaTruc_ThuNgan"));

        btnPrintZReportPdf = new Button { Text = "🖨️ In Biên Bản Kết Ca (Z-Report PDF)", Size = new Size(245, 32), Margin = new Padding(8, 3, 4, 4) };
        AppTheme.ApplyPrimaryButton(btnPrintZReportPdf);
        btnPrintZReportPdf.BackColor = Color.FromArgb(0x00, 0x5F, 0xB4);
        btnPrintZReportPdf.Click += (s, e) => ExecuteWithButtonLoading(btnPrintZReportPdf, "Đang tạo PDF...", () => PrintZReport());

        pnlToolbar.Controls.Add(lblDate);
        pnlToolbar.Controls.Add(dtpShiftDate);
        pnlToolbar.Controls.Add(btnFilterShift);
        pnlToolbar.Controls.Add(btnExportShiftExcel);
        pnlToolbar.Controls.Add(btnPrintZReportPdf);

        // 4 Summary Cards
        tlpShiftKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6)
        };

        var c1 = AppTheme.CreateKpiCard("DOANH THU TOÀN CA", "0 đ", "Đang tải dữ liệu...", AppTheme.Primary, out lblShiftRevVal, out lblShiftRevNote);
        var c2 = AppTheme.CreateKpiCard("TIỀN MẶT CẦN TRONG KÉT", "0 đ", "Đối soát két tiền mặt", AppTheme.Success, out lblShiftCashVal, out lblShiftCashNote);
        var c3 = AppTheme.CreateKpiCard("DOANH THU VIETQR", "0 đ", "Chuyển khoản ngân hàng", AppTheme.Primary, out lblShiftQrVal, out lblShiftQrNote);
        var c4 = AppTheme.CreateKpiCard("DOANH THU THẺ POS", "0 đ", "Quẹt thẻ Visa / ATM", AppTheme.Warning, out lblShiftCardVal, out lblShiftCardNote);

        AppTheme.EnableResponsiveKpiGrid(tlpShiftKpis, new List<Control> { c1, c2, c3, c4 }, tabCashierShift, 850, 440, 105);

        // Split 2 Grids: Cashiers vs Shifts
        var splitShift = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 220,
            SplitterWidth = 6,
            BackColor = Color.Transparent
        };

        // Grid 1: Cashiers
        var pnlCashierCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12)
        };
        AppTheme.ApplyCardPanel(pnlCashierCard);

        var lblCashierTitle = new Label { Text = "👤 Báo Cáo Doanh Thu Chi Tiết Theo Nhân Viên Thu Ngân", Font = AppTheme.FontH2, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Top, Height = 28 };
        dgvCashiers = new DataGridView();
        AppTheme.ApplyGridStyle(dgvCashiers);
        SetupCashierColumns();

        pnlCashierCard.Controls.Add(dgvCashiers);
        pnlCashierCard.Controls.Add(lblCashierTitle);
        splitShift.Panel1.Controls.Add(pnlCashierCard);

        // Grid 2: Shifts
        var pnlShiftCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12)
        };
        AppTheme.ApplyCardPanel(pnlShiftCard);

        var lblShiftTitle = new Label { Text = "🕒 Báo Cáo Phân Bổ Doanh Thu Theo Ca Bán Hàng (Sáng & Chiều - Tối)", Font = AppTheme.FontH2, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Top, Height = 28 };
        dgvShifts = new DataGridView();
        AppTheme.ApplyGridStyle(dgvShifts);
        SetupShiftColumns();

        pnlShiftCard.Controls.Add(dgvShifts);
        pnlShiftCard.Controls.Add(lblShiftTitle);
        splitShift.Panel2.Controls.Add(pnlShiftCard);

        tabCashierShift.Controls.Add(splitShift);
        tabCashierShift.Controls.Add(tlpShiftKpis);
        tabCashierShift.Controls.Add(pnlToolbar);
    }

    private void SetupCashierColumns()
    {
        dgvCashiers.Columns.Clear();
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã NV", DataPropertyName = "UserId", Width = 80 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ Tên Thu Ngân", DataPropertyName = "FullName", Width = 220 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tài Khoản", DataPropertyName = "Username", Width = 130 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Đơn Hoàn Tất", DataPropertyName = "TotalOrders", Width = 130 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng Doanh Thu (VNĐ)", DataPropertyName = "TotalRevenue", Width = 170 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiền Mặt Thu (Két)", DataPropertyName = "CashAmount", Width = 160 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiền Chuyển QR", DataPropertyName = "QrAmount", Width = 150 });
        dgvCashiers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiền Thẻ POS", DataPropertyName = "CardAmount", Width = 150 });
    }

    private void SetupShiftColumns()
    {
        dgvShifts.Columns.Clear();
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Ca Bán Hàng", DataPropertyName = "ShiftName", Width = 180 });
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Khung Giờ Ca", DataPropertyName = "TimeRange", Width = 160 });
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Đơn Hoàn Tất", DataPropertyName = "TotalOrders", Width = 130 });
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Doanh Thu Ca (VNĐ)", DataPropertyName = "TotalRevenue", Width = 180 });
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiền Mặt Trong Ca", DataPropertyName = "CashAmount", Width = 170 });
        dgvShifts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thanh Toán Điện Tử (QR + Thẻ)", DataPropertyName = "NonCashAmount", Width = 210 });
    }

    private async Task LoadCashierShiftReportAsync()
    {
        try
        {
            string url = $"{_apiBaseUrl}/api/v1/reports/cashier-shift?date={dtpShiftDate.Value:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data)) return;

            decimal totalRev = data.GetProperty("totalRevenue").GetDecimal();
            decimal totalCash = data.GetProperty("totalCash").GetDecimal();
            decimal totalQr = data.GetProperty("totalQr").GetDecimal();
            decimal totalCard = data.GetProperty("totalCard").GetDecimal();
            int totalOrders = data.GetProperty("totalOrders").GetInt32();

            _lastShiftRevenue = totalRev;
            _lastShiftOrders = totalOrders;
            _lastShiftCash = totalCash;
            _lastShiftQr = totalQr;
            _lastShiftCard = totalCard;

            lblShiftRevVal.Text = $"{totalRev:N0} đ";
            lblShiftRevNote.Text = $"{totalOrders:N0} đơn hoàn tất";

            lblShiftCashVal.Text = $"{totalCash:N0} đ";
            lblShiftCashNote.Text = "Khớp két tiền mặt";

            lblShiftQrVal.Text = $"{totalQr:N0} đ";
            lblShiftQrNote.Text = "Đã vào tài khoản NH";

            lblShiftCardVal.Text = $"{totalCard:N0} đ";
            lblShiftCardNote.Text = "POS máy cà thẻ";

            // Bind Cashiers
            dgvCashiers.Rows.Clear();
            if (data.TryGetProperty("cashiers", out var cArr))
            {
                foreach (var c in cArr.EnumerateArray())
                {
                    int uId = c.GetProperty("userId").GetInt32();
                    string name = c.GetProperty("fullName").GetString() ?? "";
                    string uname = c.GetProperty("username").GetString() ?? "";
                    int orders = c.GetProperty("totalOrders").GetInt32();
                    decimal rev = c.GetProperty("totalRevenue").GetDecimal();
                    decimal cash = c.GetProperty("cashAmount").GetDecimal();
                    decimal qr = c.GetProperty("qrAmount").GetDecimal();
                    decimal card = c.GetProperty("cardAmount").GetDecimal();

                    dgvCashiers.Rows.Add(
                        uId,
                        name,
                        uname,
                        $"{orders} đơn",
                        $"{rev:N0} đ",
                        $"{cash:N0} đ",
                        $"{qr:N0} đ",
                        $"{card:N0} đ"
                    );
                }
            }

            // Bind Shifts
            dgvShifts.Rows.Clear();
            if (data.TryGetProperty("shifts", out var sArr))
            {
                foreach (var s in sArr.EnumerateArray())
                {
                    string sName = s.GetProperty("shiftName").GetString() ?? "";
                    string sRange = s.GetProperty("timeRange").GetString() ?? "";
                    int orders = s.GetProperty("totalOrders").GetInt32();
                    decimal rev = s.GetProperty("totalRevenue").GetDecimal();
                    decimal cash = s.GetProperty("cashAmount").GetDecimal();
                    decimal nonCash = s.GetProperty("nonCashAmount").GetDecimal();

                    dgvShifts.Rows.Add(
                        sName,
                        sRange,
                        $"{orders} đơn",
                        $"{rev:N0} đ",
                        $"{cash:N0} đ",
                        $"{nonCash:N0} đ"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải báo cáo ca trực: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PrintZReport()
    {
        try
        {
            var printModel = new ShiftReportPrintModel
            {
                ReportDate = dtpShiftDate.Value.ToString("dd/MM/yyyy"),
                ShiftName = "Ca Chiều - Tối (14:00 - 22:00)",
                CashierName = CurrentSession.FullName ?? "Nhân viên Thu ngân (Staff POS)",
                TotalOrders = _lastShiftOrders,
                TotalRevenue = _lastShiftRevenue,
                TotalDiscount = 0,
                CashAmount = _lastShiftCash,
                QrAmount = _lastShiftQr,
                CardAmount = _lastShiftCard,
                OpeningCash = 2000000m
            };

            ShiftReportPdfService.PrintOrPreview(printModel, true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo biên bản kết ca PDF: {ex.Message}", "Lỗi QuestPDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
