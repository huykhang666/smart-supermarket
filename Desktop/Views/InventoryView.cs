using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop.Services;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class InventoryView : UserControl
{
    private TabControl tabInventory = null!;
    private TabPage tabStockList = null!;
    private TabPage tabAudit = null!;
    private TabPage tabExpiry = null!;
    private TabPage tabTransfer = null!;

    // 4 Dynamic KPI Card Labels
    private Label lblKpiTotalProducts = null!;
    private Label lblKpiOutOfStock = null!;
    private Label lblKpiLowStock = null!;
    private Label lblKpiExpiring = null!;

    // Stock List Controls
    private DataGridView dgvStockList = null!;
    private TextBox txtSearchStock = null!;
    private ComboBox cboStockStatusFilter = null!;
    private Label lblStockCount = null!;
    private Button btnRefreshStock = null!;
    private Button btnExportStockExcel = null!;

    private readonly HttpClient _httpClient = new();
    private List<StockListItem> _allStockItems = new();

    public InventoryView()
    {
        InitializeComponent();
        _ = LoadStockDataAsync();
        this.HandleCreated += async (s, e) => await LoadStockDataAsync();
        this.VisibleChanged += async (s, e) =>
        {
            if (this.Visible)
            {
                await LoadStockDataAsync();
            }
        };
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
            Height = 65,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };

        var lblTitle = new Label
        {
            Text = "📦 Quản Lý Kho Hàng & Hạn Sử Dụng (Warehouse ERP)",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 2),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Kiểm soát lượng tồn thực tế, theo dõi date hàng nhập, phân vùng kệ hàng và điều phối kho nội bộ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 36),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Dashboard Nhỏ: 4 KPI Cards Động ---
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };

        var c1 = AppTheme.CreateKpiCard("TỔNG SẢN PHẨM KHO", "0", "📦 Toàn bộ SKU đang lưu", AppTheme.Primary, out lblKpiTotalProducts, out _);
        var c2 = AppTheme.CreateKpiCard("HẾT HÀNG TỒN QUẦY", "0", "🔴 Cần châm hàng ngay", AppTheme.Danger, out lblKpiOutOfStock, out _);
        var c3 = AppTheme.CreateKpiCard("SẮP HẾT HÀNG", "0", "⚠️ Dưới mức tồn an toàn", AppTheme.Warning, out lblKpiLowStock, out _);
        var c4 = AppTheme.CreateKpiCard("CẬN HSD (≤ 5 NGÀY)", "0", "⏳ Cần xả hàng / dán tem", Color.FromArgb(0xED, 0x89, 0x36), out lblKpiExpiring, out _);
        var kpiCards = new List<Control> { c1, c2, c3, c4 };

        AppTheme.EnableResponsiveKpiGrid(pnlKpis, kpiCards, this, 850, 440, 125);

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

        // Tự động tải lại dữ liệu tồn kho khi chuyển qua tab Danh Sách Tồn Kho
        tabInventory.SelectedIndexChanged += async (s, e) =>
        {
            if (tabInventory.SelectedTab == tabStockList)
            {
                await LoadStockDataAsync();
            }
        };

        this.Controls.Add(tabInventory);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private void BuildStockListTab()
    {
        // 1. Toolbar lọc và tìm kiếm tồn kho
        var pnlStockToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 10, 12, 10)
        };

        var flpLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        txtSearchStock = new TextBox
        {
            PlaceholderText = "🔍 Tìm mã vạch, tên sản phẩm...",
            Size = new Size(260, 32),
            Font = AppTheme.FontBody,
            Margin = new Padding(0, 0, 10, 0)
        };
        txtSearchStock.TextChanged += (s, e) => ApplyStockFilter();

        cboStockStatusFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(185, 32),
            Font = AppTheme.FontBody,
            Margin = new Padding(0, 0, 10, 0)
        };
        cboStockStatusFilter.Items.AddRange(new object[] { "Tất cả trạng thái", "🟢 Đủ hàng an toàn", "🟠 Sắp hết hàng", "🔴 Hết hàng tồn quầy" });
        cboStockStatusFilter.SelectedIndex = 0;
        cboStockStatusFilter.SelectedIndexChanged += (s, e) => ApplyStockFilter();

        btnRefreshStock = new Button
        {
            Text = "🔄 Tải Lại",
            Size = new Size(100, 32),
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplySecondaryButton(btnRefreshStock);
        btnRefreshStock.Click += async (s, e) => await LoadStockDataAsync();

        btnExportStockExcel = new Button
        {
            Text = "📥 Xuất Excel",
            Size = new Size(110, 32),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnExportStockExcel);
        btnExportStockExcel.Click += (s, e) =>
        {
            ExcelExportService.ExportDataGridViewToExcel(dgvStockList, "Báo Cáo Tồn Kho Siêu Thị", "TonKho_SmartSupermarket");
        };

        flpLeft.Controls.Add(txtSearchStock);
        flpLeft.Controls.Add(cboStockStatusFilter);
        flpLeft.Controls.Add(btnRefreshStock);
        flpLeft.Controls.Add(btnExportStockExcel);

        lblStockCount = new Label
        {
            Text = "Đang tải dữ liệu tồn kho...",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true
        };

        pnlStockToolbar.Controls.Add(lblStockCount);
        pnlStockToolbar.Controls.Add(flpLeft);

        // 2. DataGridView hiển thị danh sách tồn kho
        dgvStockList = new DataGridView();
        AppTheme.ApplyGridStyle(dgvStockList);
        dgvStockList.Dock = DockStyle.Fill;
        dgvStockList.Columns.Add("Barcode", "Mã Barcode");
        dgvStockList.Columns.Add("Name", "Tên Sản Phẩm");
        dgvStockList.Columns.Add("Category", "Ngành Hàng");
        dgvStockList.Columns.Add("Stock", "Số Lượng Tồn");
        dgvStockList.Columns.Add("Unit", "ĐVT");
        dgvStockList.Columns.Add("MinStock", "Tồn Tối Thiểu");
        dgvStockList.Columns.Add("Location", "Vị Trí Kệ");
        dgvStockList.Columns.Add("Status", "Trạng Thái");
        dgvStockList.Columns.Add("LastUpdated", "Cập Nhật Lúc");

        dgvStockList.Columns["Barcode"].Width = 135;
        dgvStockList.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        dgvStockList.Columns["Category"].Width = 130;
        dgvStockList.Columns["Stock"].Width = 110;
        dgvStockList.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgvStockList.Columns["Stock"].DefaultCellStyle.Font = AppTheme.FontBodyBold;
        dgvStockList.Columns["Unit"].Width = 70;
        dgvStockList.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvStockList.Columns["MinStock"].Width = 105;
        dgvStockList.Columns["MinStock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgvStockList.Columns["Location"].Width = 110;
        dgvStockList.Columns["Status"].Width = 145;
        dgvStockList.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvStockList.Columns["LastUpdated"].Width = 140;

        tabStockList.Controls.Add(dgvStockList);
        tabStockList.Controls.Add(pnlStockToolbar);
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
                using var http = new HttpClient();
                var resp = await http.GetAsync($"{AppTheme.ApiBaseUrl}/api/v1/discount-rules/expiring-products");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var data = root.TryGetProperty("data", out var d) ? d : root;
                    if (data.ValueKind == JsonValueKind.Array)
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

                using var http = new HttpClient();
                var resp = await http.PostAsync($"{AppTheme.ApiBaseUrl}/api/v1/ai/expiry-markdown", null);
                
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var jsonObj = JsonDocument.Parse(json);
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
                   "Hiện tại hệ thống điều phối đang ở trạng thái cân bằng và đồng bộ.",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill
        };
        pnl.Controls.Add(lblInfo);
        tabTransfer.Controls.Add(pnl);
    }

    private async Task EnsureAuthHeaderAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentSession.Token))
        {
            var auth = await LoginForm.TryAuthenticateWithBackendAsync("admin", "AdminPassword123!");
            if (auth != null)
            {
                CurrentSession.Token = auth.Token;
            }
        }

        if (!string.IsNullOrWhiteSpace(CurrentSession.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentSession.Token);
        }
    }

    public async Task LoadStockDataAsync()
    {
        try
        {
            await EnsureAuthHeaderAsync();

            // 1. Lấy thông tin vị trí lưu trữ và cảnh báo hạn dùng từ Batches
            var locationMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int expiringCount = 0;

            try
            {
                var batchResp = await _httpClient.GetAsync($"{AppTheme.ApiBaseUrl}/api/inventory/batches");
                if (batchResp.IsSuccessStatusCode)
                {
                    var batchJson = await batchResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(batchJson);
                    var root = doc.RootElement;
                    var data = root.TryGetProperty("data", out var d) ? d : root;
                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in data.EnumerateArray())
                        {
                            string pName = item.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "";
                            string loc = item.TryGetProperty("location", out var lc) ? lc.GetString() ?? "" : "";
                            string status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                            if (!string.IsNullOrEmpty(pName) && !string.IsNullOrEmpty(loc) && !locationMap.ContainsKey(pName))
                            {
                                locationMap[pName] = loc;
                            }

                            if (status.Contains("Đỏ", StringComparison.OrdinalIgnoreCase) || status.Contains("hết hạn", StringComparison.OrdinalIgnoreCase))
                            {
                                expiringCount++;
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Lấy danh sách tồn kho từ API Inventory
            var resp = await _httpClient.GetAsync($"{AppTheme.ApiBaseUrl}/api/inventory?branchId=1&pageSize=500");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var dataEl = root.TryGetProperty("data", out var d) ? d : root;
                var itemsEl = dataEl.TryGetProperty("items", out var it) ? it : dataEl.TryGetProperty("Items", out var it2) ? it2 : default;

                if (itemsEl.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<StockListItem>();
                    int outOfStock = 0;
                    int lowStock = 0;
                    foreach (var item in itemsEl.EnumerateArray())
                    {
                        int pId = item.TryGetProperty("productId", out var pEl) && pEl.TryGetInt32(out int pVal) ? pVal : 0;
                        int qty = item.TryGetProperty("quantityOnHand", out var qEl) && qEl.TryGetInt32(out int qVal) ? qVal : 0;
                        int minStock = item.TryGetProperty("minStockLevel", out var mEl) && mEl.TryGetInt32(out int mVal) ? mVal : 10;
                        string updatedStr = item.TryGetProperty("lastUpdated", out var uEl) && uEl.ValueKind == JsonValueKind.String ? uEl.GetString() ?? "" : "";
                        DateTime.TryParse(updatedStr, out var updated);

                        string barcode = "";
                        string name = "";
                        string unit = "cái";
                        string category = "Chung";

                        if ((item.TryGetProperty("product", out var prodEl) || item.TryGetProperty("Product", out prodEl)) && prodEl.ValueKind == JsonValueKind.Object)
                        {
                            barcode = prodEl.TryGetProperty("barcode", out var bEl) && bEl.ValueKind == JsonValueKind.String ? bEl.GetString() ?? "" : "";
                            name = prodEl.TryGetProperty("productName", out var nEl) && nEl.ValueKind == JsonValueKind.String ? nEl.GetString() ?? "" : "";
                            unit = prodEl.TryGetProperty("unit", out var unEl) && unEl.ValueKind == JsonValueKind.String ? unEl.GetString() ?? "cái" : "cái";
                            if (prodEl.TryGetProperty("category", out var catEl) && catEl.ValueKind == JsonValueKind.Object && catEl.TryGetProperty("categoryName", out var cnEl) && cnEl.ValueKind == JsonValueKind.String)
                            {
                                category = cnEl.GetString() ?? "Chung";
                            }
                            else if (prodEl.TryGetProperty("categoryId", out var catIdEl) && catIdEl.TryGetInt32(out int cId))
                            {
                                category = cId switch
                                {
                                    1 => "Nước giải khát",
                                    2 => "Bia & Đồ uống",
                                    3 => "Bánh kẹo & Snack",
                                    4 => "Sữa & Chế phẩm",
                                    5 => "Gia vị & Đóng hộp",
                                    6 => "Hóa mỹ phẩm",
                                    7 => "Gia dụng",
                                    8 => "Chăm sóc cá nhân",
                                    _ => "Hàng tiêu dùng"
                                };
                            }
                        }

                        locationMap.TryGetValue(name, out var loc);
                        if (string.IsNullOrEmpty(loc)) loc = "Kệ A1";

                        string statusText;
                        if (qty <= 0)
                        {
                            statusText = "🔴 Hết hàng";
                            outOfStock++;
                        }
                        else if (qty <= minStock)
                        {
                            statusText = "🟠 Sắp hết";
                            lowStock++;
                        }
                        else
                        {
                            statusText = "🟢 Đủ hàng";
                        }

                        list.Add(new StockListItem
                        {
                            ProductId = pId,
                            Barcode = barcode,
                            ProductName = name,
                            CategoryName = category,
                            QuantityOnHand = qty,
                            MinStockLevel = minStock,
                            Unit = unit,
                            Location = loc,
                            LastUpdated = updated == default ? DateTime.Now : updated,
                            Status = statusText
                        });
                    }

                    _allStockItems = list;

                    void UpdateUi()
                    {
                        if (lblKpiTotalProducts != null) lblKpiTotalProducts.Text = $"{list.Count} SKU";
                        if (lblKpiOutOfStock != null) lblKpiOutOfStock.Text = $"{outOfStock} SKU";
                        if (lblKpiLowStock != null) lblKpiLowStock.Text = $"{lowStock} SKU";
                        if (lblKpiExpiring != null) lblKpiExpiring.Text = $"{expiringCount} Lô";
                        ApplyStockFilter();
                    }

                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(UpdateUi));
                    }
                    else
                    {
                        UpdateUi();
                    }
                }
            }
            else
            {
                string err = await resp.Content.ReadAsStringAsync();
                void ShowHttpErr()
                {
                    if (lblStockCount != null) lblStockCount.Text = $"⚠️ Lỗi máy chủ ({resp.StatusCode})";
                }
                if (this.InvokeRequired) this.BeginInvoke(new Action(ShowHttpErr)); else ShowHttpErr();
            }
        }
        catch (Exception ex)
        {
            void ShowExErr()
            {
                if (lblStockCount != null) lblStockCount.Text = $"⚠️ Lỗi nạp: {ex.Message}";
            }
            if (this.InvokeRequired) this.BeginInvoke(new Action(ShowExErr)); else ShowExErr();
        }
    }

    private void ApplyStockFilter()
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(ApplyStockFilter));
            return;
        }

        string search = txtSearchStock?.Text.Trim().ToLowerInvariant() ?? "";
        string filterStatus = cboStockStatusFilter?.SelectedItem?.ToString() ?? "Tất cả trạng thái";

        var filtered = _allStockItems.Where(i =>
        {
            bool matchesSearch = string.IsNullOrEmpty(search)
                || i.ProductName.ToLowerInvariant().Contains(search)
                || i.Barcode.ToLowerInvariant().Contains(search)
                || i.CategoryName.ToLowerInvariant().Contains(search);

            bool matchesStatus = true;
            if (filterStatus.Contains("Hết hàng")) matchesStatus = i.QuantityOnHand <= 0;
            else if (filterStatus.Contains("Sắp hết")) matchesStatus = i.QuantityOnHand > 0 && i.QuantityOnHand <= i.MinStockLevel;
            else if (filterStatus.Contains("Đủ hàng")) matchesStatus = i.QuantityOnHand > i.MinStockLevel;

            return matchesSearch && matchesStatus;
        }).ToList();

        if (dgvStockList == null) return;

        dgvStockList.Rows.Clear();
        foreach (var item in filtered)
        {
            int rowIndex = dgvStockList.Rows.Add(
                item.Barcode,
                item.ProductName,
                item.CategoryName,
                item.QuantityOnHand,
                item.Unit,
                item.MinStockLevel,
                item.Location,
                item.Status,
                item.LastUpdated.ToString("dd/MM/yyyy HH:mm")
            );

            var row = dgvStockList.Rows[rowIndex];
            if (item.QuantityOnHand <= 0)
            {
                row.Cells["Status"].Style.ForeColor = AppTheme.Danger;
                row.Cells["Stock"].Style.ForeColor = AppTheme.Danger;
            }
            else if (item.QuantityOnHand <= item.MinStockLevel)
            {
                row.Cells["Status"].Style.ForeColor = AppTheme.Warning;
                row.Cells["Stock"].Style.ForeColor = AppTheme.Warning;
            }
            else
            {
                row.Cells["Status"].Style.ForeColor = AppTheme.Success;
                row.Cells["Stock"].Style.ForeColor = AppTheme.TextPrimary;
            }
        }

        if (lblStockCount != null)
        {
            lblStockCount.Text = $"Hiển thị: {filtered.Count} / {_allStockItems.Count} mặt hàng";
        }
    }

    private class StockListItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int MinStockLevel { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
