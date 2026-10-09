using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class InventoryView : UserControl
{
    private TabControl tabInventory = null!;
    private TabPage tabStockList = null!;
    private TabPage tabAudit = null!;
    private TabPage tabExpiry = null!;
    private TabPage tabTransfer = null!;

    public InventoryView()
    {
        InitializeComponent();
        LoadStockData();
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
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📦 Quản Lý Kho Hàng & Hạn Sử Dụng (Warehouse ERP)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Kiểm soát lượng tồn thực tế, theo dõi date hàng nhập, phân vùng kệ hàng và điều phối kho nội bộ",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Dashboard Nhỏ: 4 KPI Cards (Tổng sản phẩm 0, Hết hàng 0, Sắp hết 0, HSD ≤ 5 ngày 0) ---
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };

        var c1 = AppTheme.CreateKpiCard("TỔNG SẢN PHẨM KHO", "0", "📦 Toàn bộ SKU đang lưu", AppTheme.Primary);
        var c2 = AppTheme.CreateKpiCard("HẾT HÀNG TỒN QUẦY", "0", "🔴 Cần châm hàng ngay", AppTheme.Danger);
        var c3 = AppTheme.CreateKpiCard("SẮP HẾT HÀNG", "0", "⚠️ Dưới mức tồn an toàn", AppTheme.Warning);
        var c4 = AppTheme.CreateKpiCard("CẬN HSD (≤ 5 NGÀY)", "0", "⏳ Cần xả hàng / dán tem", AppTheme.Warning);
        var kpiCards = new List<Control> { c1, c2, c3, c4 };

        AppTheme.EnableResponsiveKpiGrid(pnlKpis, kpiCards, this, 850, 440, 105);

        // --- 3. TabControl Kho (Danh sách tồn, Kiểm kê, HSD, Chuyển kho) ---
        tabInventory = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        tabStockList = new TabPage("📋 Danh Sách Tồn Kho");
        tabAudit = new TabPage("🔍 Kiểm Kê Nhanh");
        tabExpiry = new TabPage("⏳ Cảnh Báo Hạn Sử Dụng (HSD)");
        tabTransfer = new TabPage("🔄 Chuyển Kho Nội Bộ");

        BuildStockListTab();
        BuildExpiryTab();
        BuildTransferTab();

        // Integrate StockAuditView into tabAudit
        var auditControl = new StockAuditView { Dock = DockStyle.Fill, Padding = new Padding(12) };
        tabAudit.Controls.Add(auditControl);

        tabInventory.TabPages.Add(tabStockList);
        tabInventory.TabPages.Add(tabAudit);
        tabInventory.TabPages.Add(tabExpiry);
        tabInventory.TabPages.Add(tabTransfer);

        this.Controls.Add(tabInventory);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private void BuildStockListTab()
    {
        var dgv = new DataGridView();
        AppTheme.ApplyGridStyle(dgv);
        dgv.Dock = DockStyle.Fill;
        dgv.Columns.Add("Barcode", "Mã Barcode");
        dgv.Columns.Add("Name", "Tên Sản Phẩm");
        dgv.Columns.Add("Stock", "Số Lượng Tồn");
        dgv.Columns.Add("Location", "Vị Trí Kệ");
        dgv.Columns.Add("Status", "Trạng Thái");

        tabStockList.Controls.Add(dgv);
    }

    private void BuildExpiryTab()
    {
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
        var btnAiInsights = new Button
        {
            Text = "🤖 Phân tích AI Gợi Ý Cận Date",
            Size = new Size(240, 40),
            Location = new Point(10, 10),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnAiInsights);

        var btnRefresh = new Button
        {
            Text = "🔄 Tải Lại",
            Size = new Size(110, 40),
            Location = new Point(260, 10),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplySecondaryButton(btnRefresh);

        pnlTop.Controls.Add(btnAiInsights);
        pnlTop.Controls.Add(btnRefresh);

        var dgv = new DataGridView();
        AppTheme.ApplyGridStyle(dgv);
        dgv.Dock = DockStyle.Fill;
        dgv.Columns.Add("Batch", "Mã Lô Hàng");
        dgv.Columns.Add("Name", "Tên Sản Phẩm");
        dgv.Columns.Add("Qty", "SL Còn Lại");
        dgv.Columns.Add("ExpiryDate", "Hạn Sử Dụng");
        dgv.Columns.Add("DaysLeft", "Số Ngày Còn Lại");
        dgv.Columns.Add("Action", "Khuyến Nghị / Xả Hàng");

        async Task LoadDataAsync()
        {
            try
            {
                dgv.Rows.Clear();
                using var http = new System.Net.Http.HttpClient();
                var resp = await http.GetAsync("http://localhost:5137/api/v1/discount-rules/expiring-products");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var data = root.TryGetProperty("data", out var d) ? d : root;
                    if (data.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in data.EnumerateArray())
                        {
                            string batch = item.GetProperty("batchCode").GetString() ?? "";
                            string name = item.GetProperty("productName").GetString() ?? "";
                            int qty = item.GetProperty("quantity").GetInt32();
                            string expRaw = item.GetProperty("expiryDate").GetString() ?? "";
                            string expDate = DateTime.TryParse(expRaw, out var exp) ? exp.ToString("dd/MM/yyyy") : expRaw;
                            int daysLeft = item.GetProperty("daysRemaining").GetInt32();
                            decimal discPct = item.GetProperty("discountPercent").GetDecimal();
                            string alertStr = discPct > 0 ? $"🏷️ Giảm {discPct:N0}% xả hàng" : "Bình thường";

                            dgv.Rows.Add(batch, name, qty, expDate, $"{daysLeft} ngày", alertStr);
                        }
                    }
                }
            }
            catch { }
        }

        btnRefresh.Click += async (s, e) => await LoadDataAsync();
        _ = LoadDataAsync();

        btnAiInsights.Click += async (s, e) =>
        {
            try
            {
                btnAiInsights.Text = "⏳ Đang phân tích...";
                btnAiInsights.Enabled = false;

                using var http = new System.Net.Http.HttpClient();
                var resp = await http.PostAsync("http://localhost:5137/api/v1/ai/expiry-markdown", null);
                
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var jsonObj = System.Text.Json.JsonDocument.Parse(json);
                    string aiText = jsonObj.RootElement.GetProperty("data").GetProperty("content").GetString() ?? "";

                    MessageBox.Show(aiText, "🤖 Báo Cáo AI Gợi Ý Cận Date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi gọi AI", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                btnAiInsights.Text = "🤖 Phân tích AI Gợi Ý Cận Date";
                btnAiInsights.Enabled = true;
            }
        };

        tabExpiry.Controls.Add(dgv);
        tabExpiry.Controls.Add(pnlTop);
    }

    private void BuildTransferTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.SurfaceWhite, Padding = new Padding(24) };
        var lblInfo = new Label
        {
            Text = "🔄 Chức Năng Điều Chuyển Hàng Nội Bộ (Kho Tổng → Kho Bán Lẻ Quầy):\n\n" +
                   "• Hỗ trợ tạo phiếu điều chuyển hàng hóa từ Kho lưu trữ tầng hầm lên quầy kệ trưng bày POS.\n" +
                   "• Cập nhật số liệu tức thì giữa các phân khu trong siêu thị.\n\n" +
                   "Hiện tại kho quầy đang ở trạng thái ổn định.",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill
        };
        pnl.Controls.Add(lblInfo);
        tabTransfer.Controls.Add(pnl);
    }

    private void LoadStockData()
    {
    }
}
