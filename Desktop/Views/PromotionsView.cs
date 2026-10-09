using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class PromotionsView : UserControl
{
    private TabControl tabControlMain = null!;
    private TabPage tabVouchers = null!;
    private TabPage tabNearExpiry = null!;

    // Tab 1: Vouchers
    private DataGridView dgvPromotions = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnDelete = null!;
    private Label lblStatus = null!;

    // Tab 2: Near-Expiry & Discount Rules
    private DataGridView dgvDiscountRules = null!;
    private DataGridView dgvExpiringProducts = null!;
    private Button btnRefreshExpiry = null!;
    private Button btnAddRule = null!;
    private Button btnDeleteRule = null!;
    private Button btnAiAdvice = null!;
    private Label lblExpiryStatus = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public PromotionsView()
    {
        InitializeComponent();
        _ = LoadPromotionsAsync();
        _ = LoadExpiryDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Quản Lý Khuyến Mãi & Giảm Giá Cận Date (HSD)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 2),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        tabControlMain = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        tabVouchers = new TabPage("🎟️ Chương Trình Khuyến Mãi & Voucher");
        tabNearExpiry = new TabPage("⏳ Quản Lý HSD & Giảm Giá Xả Hàng Cận Date");

        BuildVouchersTab();
        BuildNearExpiryTab();

        tabControlMain.TabPages.Add(tabVouchers);
        tabControlMain.TabPages.Add(tabNearExpiry);

        this.Controls.Add(tabControlMain);
        this.Controls.Add(pnlHeader);
    }

    // =========================================================================
    // TAB 1: KHUYẾN MÃI & VOUCHER
    // =========================================================================
    private void BuildVouchersTab()
    {
        tabVouchers.BackColor = AppTheme.BackgroundGray;
        tabVouchers.Padding = new Padding(12);

        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 52),
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        btnRefresh = new Button { Text = "🔄 Tải lại", Size = new Size(100, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += async (s, e) => await LoadPromotionsAsync();

        btnAdd = new Button { Text = "➕ Thêm Khuyến Mãi", Size = new Size(170, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnAdd);
        btnAdd.Click += BtnAdd_Click;

        btnDelete = new Button { Text = "🗑️ Xóa Mã", Size = new Size(110, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyDangerButton(btnDelete);
        btnDelete.Click += async (s, e) => await DeleteSelectedAsync();

        lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(8, 6, 0, 4),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(btnAdd);
        pnlToolbar.Controls.Add(btnDelete);
        pnlToolbar.Controls.Add(lblStatus);

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1),
            Margin = new Padding(0, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlGridContainer);

        dgvPromotions = new DataGridView();
        AppTheme.ApplyGridStyle(dgvPromotions);
        dgvPromotions.Dock = DockStyle.Fill;

        dgvPromotions.Columns.Add("Code", "Mã Voucher");
        dgvPromotions.Columns.Add("Name", "Tên Chương Trình");
        dgvPromotions.Columns.Add("DiscountType", "Loại Giảm");
        dgvPromotions.Columns.Add("Discount", "Mức Giảm Giá");
        dgvPromotions.Columns.Add("Condition", "Đơn Hàng Tối Thiểu");
        dgvPromotions.Columns.Add("MaxDiscount", "Giảm Tối Đa");
        dgvPromotions.Columns.Add("TimeRange", "Thời Gian Hiệu Lực");
        dgvPromotions.Columns.Add("Status", "Trạng Thái");
        dgvPromotions.Columns.Add("RawId", "RawId");
        dgvPromotions.Columns["RawId"].Visible = false;

        pnlGridContainer.Controls.Add(dgvPromotions);

        tabVouchers.Controls.Add(pnlGridContainer);
        tabVouchers.Controls.Add(pnlToolbar);
    }

    // =========================================================================
    // TAB 2: QUẢN LÝ HSD & GIẢM GIÁ XẢ HÀNG CẬN DATE
    // =========================================================================
    private void BuildNearExpiryTab()
    {
        tabNearExpiry.BackColor = AppTheme.BackgroundGray;
        tabNearExpiry.Padding = new Padding(12);

        var pnlExpiryToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 52),
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        AppTheme.ApplyCardPanel(pnlExpiryToolbar);

        btnRefreshExpiry = new Button { Text = "🔄 Tải lại HSD", Size = new Size(115, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplySecondaryButton(btnRefreshExpiry);
        btnRefreshExpiry.Click += async (s, e) => await LoadExpiryDataAsync();

        btnAddRule = new Button { Text = "➕ Thêm Quy Tắc", Size = new Size(140, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnAddRule);
        btnAddRule.Click += BtnAddRule_Click;

        btnDeleteRule = new Button { Text = "🗑️ Xóa Quy Tắc", Size = new Size(125, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyDangerButton(btnDeleteRule);
        btnDeleteRule.Click += async (s, e) => await DeleteSelectedRuleAsync();

        btnAiAdvice = new Button { Text = "🤖 AI Tư Vấn Xả Hàng", Size = new Size(185, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplySecondaryButton(btnAiAdvice);
        btnAiAdvice.Click += async (s, e) => await ShowAiAdviceAsync();

        lblExpiryStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(8, 6, 0, 4),
            AutoSize = true
        };

        pnlExpiryToolbar.Controls.Add(btnRefreshExpiry);
        pnlExpiryToolbar.Controls.Add(btnAddRule);
        pnlExpiryToolbar.Controls.Add(btnDeleteRule);
        pnlExpiryToolbar.Controls.Add(btnAiAdvice);
        pnlExpiryToolbar.Controls.Add(lblExpiryStatus);

        // Chia giao diện 2 cột: Cột trái (38% - Quy tắc giảm giá HSD) | Cột phải (62% - Hàng cận date trong kho)
        var splitContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        splitContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        splitContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        splitContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // --- CỘT TRÁI: QUY TẮC GIẢM GIÁ (DiscountRule) ---
        var pnlLeftCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12),
            Margin = new Padding(0, 8, 6, 0)
        };
        AppTheme.ApplyCardPanel(pnlLeftCard);

        var lblLeftTitle = new Label
        {
            Text = "📋 Cấu Hình Quy Tắc Giảm Giá HSD (DiscountRule)",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        dgvDiscountRules = new DataGridView();
        AppTheme.ApplyGridStyle(dgvDiscountRules);
        dgvDiscountRules.Dock = DockStyle.Fill;
        dgvDiscountRules.Columns.Add("Days", "Còn Lại (≤ Ngày)");
        dgvDiscountRules.Columns.Add("Discount", "Mức Giảm");
        dgvDiscountRules.Columns.Add("Description", "Mô Tả Quy Tắc");
        dgvDiscountRules.Columns.Add("Status", "Trạng Thái");
        dgvDiscountRules.Columns.Add("RawId", "RawId");
        dgvDiscountRules.Columns["RawId"].Visible = false;

        pnlLeftCard.Controls.Add(dgvDiscountRules);
        pnlLeftCard.Controls.Add(lblLeftTitle);

        // --- CỘT PHẢI: MẶT HÀNG CẬN DATE CẦN XẢ HÀNG ---
        var pnlRightCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12),
            Margin = new Padding(6, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlRightCard);

        var lblRightTitle = new Label
        {
            Text = "🏷️ Danh Sách Hàng Cận Date Trong Kho (≤ 30 Ngày Tự Động Giảm)",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        dgvExpiringProducts = new DataGridView();
        AppTheme.ApplyGridStyle(dgvExpiringProducts);
        dgvExpiringProducts.Dock = DockStyle.Fill;
        dgvExpiringProducts.Columns.Add("Alert", "Mức Cảnh Báo");
        dgvExpiringProducts.Columns.Add("Name", "Tên Sản Phẩm");
        dgvExpiringProducts.Columns.Add("Barcode", "Mã Vạch");
        dgvExpiringProducts.Columns.Add("Stock", "Tồn");
        dgvExpiringProducts.Columns.Add("ExpDate", "HSD");
        dgvExpiringProducts.Columns.Add("DaysLeft", "Còn (Ngày)");
        dgvExpiringProducts.Columns.Add("OrigPrice", "Giá Gốc");
        dgvExpiringProducts.Columns.Add("Discount", "Giảm");
        dgvExpiringProducts.Columns.Add("ClearPrice", "Giá Xả Hàng");

        pnlRightCard.Controls.Add(dgvExpiringProducts);
        pnlRightCard.Controls.Add(lblRightTitle);

        splitContainer.Controls.Add(pnlLeftCard, 0, 0);
        splitContainer.Controls.Add(pnlRightCard, 1, 0);

        tabNearExpiry.Controls.Add(splitContainer);
        tabNearExpiry.Controls.Add(pnlExpiryToolbar);
    }

    // =========================================================================
    // DỮ LIỆU & SỰ KIỆN: TAB 1 (Vouchers)
    // =========================================================================
    private async Task LoadPromotionsAsync()
    {
        lblStatus.Text = "Đang tải...";
        dgvPromotions.Rows.Clear();

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/promotions?page=1&pageSize=100");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data))
                {
                    var itemsProp = data.TryGetProperty("items", out var items) ? items : data;
                    if (itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsProp.EnumerateArray())
                        {
                            int id = item.GetProperty("promotionId").GetInt32();
                            string code = item.GetProperty("promotionCode").GetString() ?? "";
                            string name = item.GetProperty("promotionName").GetString() ?? "";
                            string discType = item.TryGetProperty("discountType", out var dt) ? (dt.GetString() ?? "Percentage") : "Percentage";
                            decimal discVal = item.GetProperty("discountValue").GetDecimal();
                            string discStr = discType.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                                ? $"{discVal:N0}%" : $"{discVal:N0} ₫";
                            decimal minOrder = item.TryGetProperty("minimumOrderAmount", out var mo) ? mo.GetDecimal() : 0m;
                            string maxStr = item.TryGetProperty("maximumDiscountAmount", out var mx) && mx.ValueKind != JsonValueKind.Null
                                ? $"{mx.GetDecimal():N0} ₫" : "Không giới hạn";
                            string discTypeLbl = discType.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                                ? "📊 % Phần Trăm" : "💰 Cố Định";

                            string startRaw = item.TryGetProperty("startDate", out var sd) ? sd.GetString() ?? "" : "";
                            string endRaw = item.TryGetProperty("endDate", out var ed) ? ed.GetString() ?? "" : "";
                            string timeRange = $"{ParseDateShort(startRaw)} → {ParseDateShort(endRaw)}";

                            bool isActive = item.TryGetProperty("isActive", out var ia) && ia.GetBoolean();
                            DateTime now = DateTime.UtcNow;
                            DateTime endDate = string.IsNullOrEmpty(endRaw) ? DateTime.MinValue : DateTime.Parse(endRaw);
                            string statusStr;
                            if (!isActive) statusStr = "🔴 Đã Tắt";
                            else if (endDate < now) statusStr = "⚪ Đã Hết Hạn";
                            else if ((endDate - now).TotalDays <= 3) statusStr = "⏳ Sắp Hết Hạn";
                            else statusStr = "🟢 Đang Diễn Ra";

                            dgvPromotions.Rows.Add(
                                code, name, discTypeLbl, discStr,
                                $"{minOrder:N0} ₫", maxStr, timeRange, statusStr, id
                            );
                        }
                        lblStatus.Text = $"Đã tải {dgvPromotions.Rows.Count} khuyến mãi.";
                        return;
                    }
                }
            }
            lblStatus.Text = "Không có dữ liệu hoặc API offline.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
        }
    }

    private static string ParseDateShort(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "-";
        if (DateTime.TryParse(raw, out var dt)) return dt.ToLocalTime().ToString("dd/MM/yyyy");
        return raw;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new AddPromotionForm(_apiBaseUrl, _httpClient);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadPromotionsAsync();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (dgvPromotions.CurrentRow == null) return;
        var rawId = dgvPromotions.CurrentRow.Cells["RawId"].Value;
        if (rawId == null) return;

        int id = Convert.ToInt32(rawId);
        string code = dgvPromotions.CurrentRow.Cells["Code"].Value?.ToString() ?? $"ID-{id}";

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn XÓA mã khuyến mãi '{code}'?\nHành động này không thể hoàn tác.",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            lblStatus.Text = $"Đang xóa {code}...";
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/promotions/{id}");
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"✅ Đã xóa mã '{code}' thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPromotionsAsync();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể xóa.\nChi tiết: {body}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Xóa thất bại.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // =========================================================================
    // DỮ LIỆU & SỰ KIỆN: TAB 2 (Quản lý HSD & Discount Rules)
    // =========================================================================
    private async Task LoadExpiryDataAsync()
    {
        lblExpiryStatus.Text = "Đang tải...";
        dgvDiscountRules.Rows.Clear();
        dgvExpiringProducts.Rows.Clear();

        try
        {
            // 1. Tải danh sách DiscountRules
            var rulesResp = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/discount-rules?includeInactive=true");
            if (rulesResp.IsSuccessStatusCode)
            {
                var content = await rulesResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in data.EnumerateArray())
                    {
                        int id = r.GetProperty("discountRuleId").GetInt32();
                        int days = r.GetProperty("daysBeforeExpiry").GetInt32();
                        decimal pct = r.GetProperty("discountPercent").GetDecimal();
                        bool active = r.GetProperty("isActive").GetBoolean();
                        string desc = r.TryGetProperty("description", out var ds) && ds.ValueKind != JsonValueKind.Null
                            ? (ds.GetString() ?? "") : $"≤ {days} ngày giảm {pct:N0}%";

                        string activeStr = active ? "🟢 Đang áp dụng" : "🔴 Đang tắt";
                        dgvDiscountRules.Rows.Add($"≤ {days} ngày", $"{pct:N0}%", desc, activeStr, id);
                    }
                }
            }

            // 2. Tải danh sách Hàng cận date cần xả hàng
            var expResp = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/discount-rules/expiring-products");
            if (expResp.IsSuccessStatusCode)
            {
                var content = await expResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        string name = item.GetProperty("productName").GetString() ?? "";
                        string barcode = item.GetProperty("barcode").GetString() ?? "";
                        int qty = item.GetProperty("quantity").GetInt32();
                        string expStr = item.GetProperty("expiryDate").GetString() ?? "";
                        string expDate = DateTime.TryParse(expStr, out var exp) ? exp.ToString("dd/MM/yyyy") : expStr;
                        int daysLeft = item.GetProperty("daysRemaining").GetInt32();
                        decimal origPrice = item.GetProperty("originalPrice").GetDecimal();
                        decimal discPct = item.GetProperty("discountPercent").GetDecimal();
                        decimal clearPrice = item.GetProperty("clearancePrice").GetDecimal();
                        string alertLevel = item.GetProperty("alertLevel").GetString() ?? "Notice";

                        string alertStr;
                        if (alertLevel == "Expired" || daysLeft < 0) alertStr = "⚪ ĐÃ HẾT HẠN";
                        else if (alertLevel == "Critical" || daysLeft <= 7) alertStr = "🔴 Khẩn cấp (≤7d)";
                        else if (alertLevel == "Warning" || daysLeft <= 15) alertStr = "🟠 Cảnh báo (≤15d)";
                        else alertStr = "🟡 Chú ý (≤30d)";

                        dgvExpiringProducts.Rows.Add(
                            alertStr, name, barcode, qty, expDate, daysLeft,
                            $"{origPrice:N0} ₫",
                            discPct > 0 ? $"-{discPct:N0}%" : "-",
                            $"{clearPrice:N0} ₫"
                        );
                    }
                }
            }

            lblExpiryStatus.Text = $"Đã tải {dgvDiscountRules.Rows.Count} quy tắc, {dgvExpiringProducts.Rows.Count} SP cận date.";
        }
        catch (Exception ex)
        {
            lblExpiryStatus.Text = $"Lỗi: {ex.Message}";
        }
    }

    private void BtnAddRule_Click(object? sender, EventArgs e)
    {
        using var form = new AddDiscountRuleForm(_apiBaseUrl, _httpClient);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadExpiryDataAsync();
        }
    }

    private async Task DeleteSelectedRuleAsync()
    {
        if (dgvDiscountRules.CurrentRow == null) return;
        var rawId = dgvDiscountRules.CurrentRow.Cells["RawId"].Value;
        if (rawId == null) return;

        int id = Convert.ToInt32(rawId);
        string desc = dgvDiscountRules.CurrentRow.Cells["Description"].Value?.ToString() ?? $"Quy tắc #{id}";

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn XÓA quy tắc:\n'{desc}'?\n\nThao tác này sẽ hủy áp dụng tự động giảm giá cận date của mức này.",
            "Xác nhận xóa quy tắc", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            lblExpiryStatus.Text = $"Đang xóa quy tắc #{id}...";
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/discount-rules/{id}");
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("✅ Đã xóa quy tắc thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadExpiryDataAsync();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể xóa quy tắc.\nChi tiết: {body}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblExpiryStatus.Text = "Xóa thất bại.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowAiAdviceAsync()
    {
        try
        {
            lblExpiryStatus.Text = "🤖 AI đang phân tích dữ liệu kho cận date...";
            btnAiAdvice.Enabled = false;

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/ai/expiry-markdown", null);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;
                string aiText = data.TryGetProperty("content", out var c) ? (c.GetString() ?? "") : "Không có phản hồi.";

                using var dlg = new AiMarkdownAdviceDialog(aiText);
                dlg.ShowDialog(this);
                lblExpiryStatus.Text = "Đã hoàn thành tư vấn AI.";
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"AI không thể tạo báo cáo.\nChi tiết: {err}", "Lỗi AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblExpiryStatus.Text = "Lỗi phản hồi AI.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối AI: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblExpiryStatus.Text = "Lỗi kết nối AI.";
        }
        finally
        {
            btnAiAdvice.Enabled = true;
        }
    }
}

// =========================================================================
// FORM POPUP: THÊM QUY TẮC GIẢM GIÁ CẬN DATE
// =========================================================================
internal class AddDiscountRuleForm : Form
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient;

    private NumericUpDown numDays = null!;
    private NumericUpDown numDiscount = null!;
    private TextBox txtDesc = null!;
    private CheckBox chkActive = null!;
    private Button btnSave = null!;
    private Button btnCancel_ = null!;

    public AddDiscountRuleForm(string apiBaseUrl, HttpClient httpClient)
    {
        _apiBaseUrl = apiBaseUrl;
        _httpClient = httpClient;
        InitForm();
    }

    private void InitForm()
    {
        this.Text = "➕ Thêm Quy Tắc Giảm Giá Cận Date";
        this.Size = new Size(520, 360);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ThemeManager.Background;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 20;
        int lx = 20;
        int cx = 200;
        int cw = 270;

        Label Lbl(string text) => new Label
        {
            Text = text,
            Font = ThemeManager.BodyBold,
            ForeColor = ThemeManager.TextPrimary,
            Location = new Point(lx, y + 4),
            AutoSize = true
        };

        void NextRow(int h = 46) { y += h; }

        this.Controls.Add(Lbl("Số ngày còn lại (≤):"));
        numDays = new NumericUpDown { Location = new Point(cx, y), Size = new Size(120, 28), Font = ThemeManager.BodyFont, Minimum = 1, Maximum = 365, Value = 15 };
        var lblDaysUnit = new Label { Text = "ngày", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(cx + 130, y + 4), AutoSize = true };
        this.Controls.Add(numDays);
        this.Controls.Add(lblDaysUnit);
        NextRow();

        this.Controls.Add(Lbl("Mức giảm giá (%):"));
        numDiscount = new NumericUpDown { Location = new Point(cx, y), Size = new Size(120, 28), Font = ThemeManager.BodyFont, Minimum = 1, Maximum = 100, Value = 25 };
        var lblDiscUnit = new Label { Text = "%", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextSecondary, Location = new Point(cx + 130, y + 4), AutoSize = true };
        this.Controls.Add(numDiscount);
        this.Controls.Add(lblDiscUnit);
        NextRow();

        this.Controls.Add(Lbl("Mô tả quy tắc:"));
        txtDesc = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Text = "Hàng còn 15 ngày hết hạn giảm 25%" };
        this.Controls.Add(txtDesc);
        NextRow();

        numDays.ValueChanged += (s, e) => UpdateSuggestedDesc();
        numDiscount.ValueChanged += (s, e) => UpdateSuggestedDesc();

        this.Controls.Add(Lbl("Kích hoạt ngay:"));
        chkActive = new CheckBox { Location = new Point(cx, y + 2), Checked = true, Font = ThemeManager.BodyFont };
        this.Controls.Add(chkActive);
        NextRow(52);

        btnSave = new Button { Text = "💾 Lưu Quy Tắc", Size = new Size(140, 36), Location = new Point(cx, y) };
        ThemeManager.ApplyPrimaryButton(btnSave);
        btnSave.Click += async (s, e) => await SaveRuleAsync();
        this.Controls.Add(btnSave);

        btnCancel_ = new Button { Text = "Hủy", Size = new Size(85, 36), Location = new Point(cx + 155, y) };
        ThemeManager.ApplySecondaryButton(btnCancel_);
        btnCancel_.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnCancel_);
    }

    private void UpdateSuggestedDesc()
    {
        txtDesc.Text = $"Hàng còn {numDays.Value} ngày hết hạn giảm {numDiscount.Value}% xả hàng";
    }

    private async Task SaveRuleAsync()
    {
        var payload = new
        {
            daysBeforeExpiry = (int)numDays.Value,
            discountPercent = numDiscount.Value,
            isActive = chkActive.Checked,
            description = txtDesc.Text.Trim()
        };

        try
        {
            btnSave.Enabled = false;
            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/discount-rules",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("✅ Đã tạo quy tắc giảm giá cận date thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Lỗi: {body}", "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSave.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
        }
    }
}

// =========================================================================
// FORM POPUP: HIỂN THỊ KẾT QUẢ TƯ VẤN AI MARKDOWN
// =========================================================================
internal class AiMarkdownAdviceDialog : Form
{
    public AiMarkdownAdviceDialog(string markdownContent)
    {
        this.Text = "🤖 AI Tư Vấn Chiến Lược Xả Hàng Cận Date (Google Gemini)";
        this.Size = new Size(780, 620);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.Sizable;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(240, 245, 255),
            Padding = new Padding(16, 12, 16, 12)
        };
        var lblH = new Label
        {
            Text = "💡 Phân tích tồn kho cận date & Đề xuất mức giảm giá tối ưu",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 88, 217),
            Dock = DockStyle.Fill
        };
        pnlHeader.Controls.Add(lblH);

        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(12)
        };
        var btnCopy = new Button
        {
            Text = "📋 Sao Chép Nội Dung",
            Size = new Size(160, 32),
            Dock = DockStyle.Left
        };
        ThemeManager.ApplySecondaryButton(btnCopy);
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(markdownContent);
            MessageBox.Show("Đã sao chép nội dung vào Clipboard!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(90, 32),
            Dock = DockStyle.Right
        };
        ThemeManager.ApplyPrimaryButton(btnClose);
        btnClose.Click += (s, e) => this.Close();

        pnlBottom.Controls.Add(btnCopy);
        pnlBottom.Controls.Add(btnClose);

        var txtContent = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10.5f),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(33, 37, 41),
            Text = markdownContent
        };

        this.Controls.Add(txtContent);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHeader);
    }
}

// =========================================================================
// FORM POPUP CŨ: THÊM VOUCHER
// =========================================================================
internal class AddPromotionForm : Form
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient;

    private TextBox txtCode = null!;
    private TextBox txtName = null!;
    private TextBox txtDesc = null!;
    private ComboBox cboType = null!;
    private NumericUpDown numValue = null!;
    private Label lblUnitValue = null!;
    private NumericUpDown numMinOrder = null!;
    private NumericUpDown numMaxDiscount = null!;
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private CheckBox chkActive = null!;
    private Button btnSave = null!;
    private Button btnCancel_ = null!;

    public AddPromotionForm(string apiBaseUrl, HttpClient httpClient)
    {
        _apiBaseUrl = apiBaseUrl;
        _httpClient = httpClient;
        InitForm();
    }

    private void InitForm()
    {
        this.Text = "➕ Thêm Mã Khuyến Mãi";
        this.Size = new Size(620, 580);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ThemeManager.Background;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 20;
        int lx = 20;
        int cx = 225;
        int cw = 320;

        Label Lbl(string text) => new Label
        {
            Text = text,
            Font = ThemeManager.BodyBold,
            ForeColor = ThemeManager.TextPrimary,
            Location = new Point(lx, y + 4),
            AutoSize = true
        };

        void NextRow(int h = 44) { y += h; }

        this.Controls.Add(Lbl("Mã Voucher:"));
        txtCode = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, CharacterCasing = CharacterCasing.Upper };
        this.Controls.Add(txtCode); NextRow();

        this.Controls.Add(Lbl("Tên chương trình:"));
        txtName = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont };
        this.Controls.Add(txtName); NextRow();

        this.Controls.Add(Lbl("Mô tả:"));
        txtDesc = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont };
        this.Controls.Add(txtDesc); NextRow();

        this.Controls.Add(Lbl("Loại giảm giá:"));
        cboType = new ComboBox
        {
            Location = new Point(cx, y),
            Size = new Size(cw, 28),
            Font = ThemeManager.BodyFont,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboType.Items.Add(new KeyValuePair<string, string>("Percentage", "Giảm theo % (Phần trăm)"));
        cboType.Items.Add(new KeyValuePair<string, string>("FixedAmount", "Giảm số tiền cố định (VNĐ)"));
        cboType.DisplayMember = "Value";
        cboType.ValueMember = "Key";
        cboType.SelectedIndex = 0;
        cboType.SelectedIndexChanged += (s, e) =>
        {
            var selected = (KeyValuePair<string, string>)cboType.SelectedItem!;
            if (selected.Key == "Percentage")
            {
                lblUnitValue.Text = "%";
                numValue.Maximum = 100;
            }
            else
            {
                lblUnitValue.Text = "VNĐ";
                numValue.Maximum = 100000000;
            }
        };
        this.Controls.Add(cboType); NextRow();

        this.Controls.Add(Lbl("Giá trị giảm:"));
        numValue = new NumericUpDown { Location = new Point(cx, y), Size = new Size(200, 28), Font = ThemeManager.BodyFont, Maximum = 100, DecimalPlaces = 0, ThousandsSeparator = true };
        lblUnitValue = new Label { Text = "%", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextSecondary, Location = new Point(cx + 210, y + 4), AutoSize = true };
        this.Controls.Add(numValue);
        this.Controls.Add(lblUnitValue);
        NextRow();

        this.Controls.Add(Lbl("Đơn hàng tối thiểu:"));
        numMinOrder = new NumericUpDown { Location = new Point(cx, y), Size = new Size(200, 28), Font = ThemeManager.BodyFont, Maximum = 100000000, DecimalPlaces = 0, ThousandsSeparator = true };
        var lblUnitMin = new Label { Text = "VNĐ", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(cx + 210, y + 4), AutoSize = true };
        this.Controls.Add(numMinOrder);
        this.Controls.Add(lblUnitMin);
        NextRow();

        this.Controls.Add(Lbl("Giảm tối đa:"));
        numMaxDiscount = new NumericUpDown { Location = new Point(cx, y), Size = new Size(200, 28), Font = ThemeManager.BodyFont, Maximum = 100000000, DecimalPlaces = 0, ThousandsSeparator = true };
        var lblUnitMax = new Label { Text = "VNĐ", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(cx + 210, y + 4), AutoSize = true };
        this.Controls.Add(numMaxDiscount);
        this.Controls.Add(lblUnitMax);
        NextRow();

        this.Controls.Add(Lbl("Ngày bắt đầu:"));
        dtpStart = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Value = DateTime.Today };
        this.Controls.Add(dtpStart); NextRow();

        this.Controls.Add(Lbl("Ngày kết thúc:"));
        dtpEnd = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Value = DateTime.Today.AddMonths(1) };
        this.Controls.Add(dtpEnd); NextRow();

        this.Controls.Add(Lbl("Kích hoạt:"));
        chkActive = new CheckBox { Location = new Point(cx, y + 2), Checked = true, Font = ThemeManager.BodyFont };
        this.Controls.Add(chkActive); NextRow(40);

        btnSave = new Button { Text = "💾 Lưu", Size = new Size(110, 36), Location = new Point(cx, y) };
        ThemeManager.ApplyPrimaryButton(btnSave);
        btnSave.Click += async (s, e) => await SaveAsync();
        this.Controls.Add(btnSave);

        btnCancel_ = new Button { Text = "Hủy", Size = new Size(85, 36), Location = new Point(cx + 125, y) };
        ThemeManager.ApplySecondaryButton(btnCancel_);
        btnCancel_.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnCancel_);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Mã voucher và tên chương trình là bắt buộc.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedType = (KeyValuePair<string, string>)cboType.SelectedItem!;

        var payload = new
        {
            promotionCode = txtCode.Text.Trim().ToUpper(),
            promotionName = txtName.Text.Trim(),
            description = txtDesc.Text.Trim(),
            discountType = selectedType.Key,
            discountValue = numValue.Value,
            minimumOrderAmount = numMinOrder.Value,
            maximumDiscountAmount = numMaxDiscount.Value > 0 ? (decimal?)numMaxDiscount.Value : null,
            startDate = dtpStart.Value.ToUniversalTime(),
            endDate = dtpEnd.Value.ToUniversalTime(),
            isActive = chkActive.Checked
        };

        try
        {
            btnSave.Enabled = false;
            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/promotions",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"✅ Đã tạo mã khuyến mãi '{txtCode.Text.ToUpper()}' thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Lỗi: {body}", "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSave.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
        }
    }
}