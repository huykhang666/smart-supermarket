using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class AiCopilotView : UserControl
{
    private TabControl tabControl = null!;
    private TabPage tabForecast = null!;
    private TabPage tabRevenue = null!;
    private TabPage tabProducts = null!;

    private RichTextBox rtbForecast = null!;
    private RichTextBox rtbRevenue = null!;
    private RichTextBox rtbProducts = null!;

    private Button btnForecast = null!;
    private Button btnRevenue = null!;
    private Button btnProducts = null!;

    private NumericUpDown numForecastDays = null!;
    private DateTimePicker dtpRevenueStart = null!;
    private DateTimePicker dtpRevenueEnd = null!;
    private NumericUpDown numProductTopN = null!;
    private NumericUpDown numProductDays = null!;

    private Label lblStatusForecast = null!;
    private Label lblStatusRevenue = null!;
    private Label lblStatusProducts = null!;

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(90) };
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;
    private CancellationTokenSource? _cts;

    public AiCopilotView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(20);

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };

        var lblTitle = new Label
        {
            Text = "🤖 Trợ Lý AI – Google Gemini",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Phân tích dữ liệu thực tế từ CSDL siêu thị, sinh báo cáo bằng Google Gemini AI",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 36),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBody
        };

        tabForecast = new TabPage("📦  Dự Báo Nhập Hàng");
        tabRevenue  = new TabPage("📊  Báo Cáo Doanh Thu");
        tabProducts = new TabPage("🏆  Phân Tích Sản Phẩm");

        BuildForecastTab();
        BuildRevenueTab();
        BuildProductsTab();

        tabControl.TabPages.Add(tabForecast);
        tabControl.TabPages.Add(tabRevenue);
        tabControl.TabPages.Add(tabProducts);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlHeader);
    }

    private void BuildForecastTab()
    {
        tabForecast.BackColor = AppTheme.SurfaceWhite;
        tabForecast.Padding = new Padding(12, 8, 12, 8);

        var pnlOptions = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent, Padding = new Padding(4, 8, 4, 8) };

        var lblDays = new Label { Text = "Phân tích dữ liệu bán hàng trong:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextPrimary, Location = new Point(4, 16), AutoSize = true };
        numForecastDays = new NumericUpDown { Location = new Point(220, 12), Size = new Size(70, 28), Minimum = 7, Maximum = 90, Value = 30, Font = AppTheme.FontBody };
        var lblDaysUnit = new Label { Text = "ngày gần nhất", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(296, 16), AutoSize = true };

        btnForecast = new Button { Text = "🤖 Chạy AI Dự Báo Nhập Hàng", Size = new Size(220, 32), Location = new Point(450, 10) };
        AppTheme.ApplyPrimaryButton(btnForecast);
        btnForecast.Click += async (s, e) => await RunForecastAsync();

        lblStatusForecast = new Label { Text = "", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(680, 16), AutoSize = true };

        pnlOptions.Controls.Add(lblDays);
        pnlOptions.Controls.Add(numForecastDays);
        pnlOptions.Controls.Add(lblDaysUnit);
        pnlOptions.Controls.Add(btnForecast);
        pnlOptions.Controls.Add(lblStatusForecast);

        rtbForecast = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = AppTheme.SurfaceWhite,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text = "Nhấn nút \"Chạy AI\" để bắt đầu phân tích và nhận dự báo nhập hàng từ Gemini AI..."
        };

        tabForecast.Controls.Add(rtbForecast);
        tabForecast.Controls.Add(pnlOptions);
    }

    private void BuildRevenueTab()
    {
        tabRevenue.BackColor = AppTheme.SurfaceWhite;
        tabRevenue.Padding = new Padding(12, 8, 12, 8);

        var pnlOptions = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent, Padding = new Padding(4, 8, 4, 8) };

        var lblFrom = new Label { Text = "Từ ngày:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextPrimary, Location = new Point(4, 16), AutoSize = true };
        dtpRevenueStart = new DateTimePicker { Location = new Point(70, 11), Size = new Size(130, 28), Font = AppTheme.FontBody, Value = DateTime.Today.AddDays(-30) };

        var lblTo = new Label { Text = "đến:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextPrimary, Location = new Point(208, 16), AutoSize = true };
        dtpRevenueEnd = new DateTimePicker { Location = new Point(240, 11), Size = new Size(130, 28), Font = AppTheme.FontBody, Value = DateTime.Today };

        btnRevenue = new Button { Text = "🤖 Tạo Báo Cáo AI", Size = new Size(180, 32), Location = new Point(390, 10) };
        AppTheme.ApplyPrimaryButton(btnRevenue);
        btnRevenue.Click += async (s, e) => await RunRevenueReportAsync();

        lblStatusRevenue = new Label { Text = "", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(580, 16), AutoSize = true };

        pnlOptions.Controls.Add(lblFrom);
        pnlOptions.Controls.Add(dtpRevenueStart);
        pnlOptions.Controls.Add(lblTo);
        pnlOptions.Controls.Add(dtpRevenueEnd);
        pnlOptions.Controls.Add(btnRevenue);
        pnlOptions.Controls.Add(lblStatusRevenue);

        rtbRevenue = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = AppTheme.SurfaceWhite,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text = "Chọn khoảng thời gian và nhấn \"Tạo Báo Cáo AI\" để nhận báo cáo doanh thu tự động..."
        };

        tabRevenue.Controls.Add(rtbRevenue);
        tabRevenue.Controls.Add(pnlOptions);
    }

    private void BuildProductsTab()
    {
        tabProducts.BackColor = AppTheme.SurfaceWhite;
        tabProducts.Padding = new Padding(12, 8, 12, 8);

        var pnlOptions = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent, Padding = new Padding(4, 8, 4, 8) };

        var lblTop = new Label { Text = "Top", Font = AppTheme.FontBody, ForeColor = AppTheme.TextPrimary, Location = new Point(4, 16), AutoSize = true };
        numProductTopN = new NumericUpDown { Location = new Point(38, 12), Size = new Size(60, 28), Minimum = 5, Maximum = 50, Value = 10, Font = AppTheme.FontBody };
        var lblSP = new Label { Text = "sản phẩm trong", Font = AppTheme.FontBody, ForeColor = AppTheme.TextPrimary, Location = new Point(104, 16), AutoSize = true };
        numProductDays = new NumericUpDown { Location = new Point(202, 12), Size = new Size(60, 28), Minimum = 7, Maximum = 90, Value = 30, Font = AppTheme.FontBody };
        var lblDU = new Label { Text = "ngày gần nhất", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(268, 16), AutoSize = true };

        btnProducts = new Button { Text = "🤖 Phân Tích AI", Size = new Size(165, 32), Location = new Point(390, 10) };
        AppTheme.ApplyPrimaryButton(btnProducts);
        btnProducts.Click += async (s, e) => await RunProductAnalysisAsync();

        lblStatusProducts = new Label { Text = "", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(565, 16), AutoSize = true };

        pnlOptions.Controls.Add(lblTop);
        pnlOptions.Controls.Add(numProductTopN);
        pnlOptions.Controls.Add(lblSP);
        pnlOptions.Controls.Add(numProductDays);
        pnlOptions.Controls.Add(lblDU);
        pnlOptions.Controls.Add(btnProducts);
        pnlOptions.Controls.Add(lblStatusProducts);

        rtbProducts = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = AppTheme.SurfaceWhite,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text = "Nhấn \"Phân Tích AI\" để nhận báo cáo phân tích sản phẩm bán chạy từ Gemini AI..."
        };

        tabProducts.Controls.Add(rtbProducts);
        tabProducts.Controls.Add(pnlOptions);
    }

    private async Task RunForecastAsync()
    {
        btnForecast.Enabled = false;
        lblStatusForecast.Text = "⏳ AI đang phân tích...";
        rtbForecast.Text = "🤖 Đang kết nối Gemini AI và phân tích dữ liệu tồn kho...";

        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var body = JsonSerializer.Serialize(new { daysBack = (int)numForecastDays.Value });
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/ai/import-forecast",
                new StringContent(body, Encoding.UTF8, "application/json"),
                _cts.Token);

            var json = await response.Content.ReadAsStringAsync(_cts.Token);
            var content = ExtractAiContent(json);
            rtbForecast.Text = content;
            lblStatusForecast.Text = $"✅ Hoàn tất lúc {DateTime.Now:HH:mm:ss}";
        }
        catch (TaskCanceledException)
        {
            rtbForecast.Text = "[Đã hủy yêu cầu]";
            lblStatusForecast.Text = "Đã hủy";
        }
        catch (Exception ex)
        {
            rtbForecast.Text = $"[Lỗi kết nối API]\n{ex.Message}\n\nHãy đảm bảo Backend đang chạy tại {_apiBaseUrl}";
            lblStatusForecast.Text = "❌ Lỗi";
        }
        finally
        {
            btnForecast.Enabled = true;
        }
    }

    private async Task RunRevenueReportAsync()
    {
        btnRevenue.Enabled = false;
        lblStatusRevenue.Text = "⏳ AI đang tạo báo cáo...";
        rtbRevenue.Text = "🤖 Đang phân tích dữ liệu doanh thu và tạo báo cáo...";

        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var body = JsonSerializer.Serialize(new
            {
                startDate = dtpRevenueStart.Value.ToString("yyyy-MM-ddT00:00:00"),
                endDate = dtpRevenueEnd.Value.ToString("yyyy-MM-ddT23:59:59")
            });

            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/ai/revenue-report",
                new StringContent(body, Encoding.UTF8, "application/json"),
                _cts.Token);

            var json = await response.Content.ReadAsStringAsync(_cts.Token);
            var content = ExtractAiContent(json);
            rtbRevenue.Text = content;
            lblStatusRevenue.Text = $"✅ Hoàn tất lúc {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            rtbRevenue.Text = $"[Lỗi]\n{ex.Message}";
            lblStatusRevenue.Text = "❌ Lỗi";
        }
        finally
        {
            btnRevenue.Enabled = true;
        }
    }

    private async Task RunProductAnalysisAsync()
    {
        btnProducts.Enabled = false;
        lblStatusProducts.Text = "⏳ AI đang phân tích...";
        rtbProducts.Text = "🤖 Đang phân tích danh mục sản phẩm bán chạy...";

        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var body = JsonSerializer.Serialize(new
            {
                topN = (int)numProductTopN.Value,
                daysBack = (int)numProductDays.Value
            });

            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/ai/product-analysis",
                new StringContent(body, Encoding.UTF8, "application/json"),
                _cts.Token);

            var json = await response.Content.ReadAsStringAsync(_cts.Token);
            var content = ExtractAiContent(json);
            rtbProducts.Text = content;
            lblStatusProducts.Text = $"✅ Hoàn tất lúc {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            rtbProducts.Text = $"[Lỗi]\n{ex.Message}";
            lblStatusProducts.Text = "❌ Lỗi";
        }
        finally
        {
            btnProducts.Enabled = true;
        }
    }

    private static string ExtractAiContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("content", out var content))
                    return content.GetString() ?? json;
            }
            return json;
        }
        catch
        {
            return json;
        }
    }
}
