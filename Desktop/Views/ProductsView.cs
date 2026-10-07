using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace Desktop.Views;

public class ProductsView : UserControl
{
    private enum ViewMode { Table, Card }
    private ViewMode _currentViewMode = ViewMode.Table;

    private Panel pnlHeader = null!;
    private Panel pnlTopBar = null!;
    private TextBox txtSearch = null!;
    private ComboBox cbCategory = null!;
    private ComboBox cbStatus = null!;
    private Button btnSearch = null!;
    private Button btnAddProduct = null!;
    private Button btnScanBarcode = null!;

    private Button btnViewTable = null!;
    private Button btnViewCard = null!;

    private DataGridView dgvProducts = null!;
    private FlowLayoutPanel flpCardView = null!;
    private Panel pnlContentContainer = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public ProductsView()
    {
        InitializeComponent();
        DataStore.ProductAdded += DataStore_ProductAdded;
        LoadDataAsync();
    }

    private void DataStore_ProductAdded(object? sender, ProductDto p)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => DataStore_ProductAdded(sender, p)));
            return;
        }

        // 1. Update Table View
        bool existsInTable = false;
        foreach (DataGridViewRow row in dgvProducts.Rows)
        {
            if (row.Cells["Barcode"].Value?.ToString() == p.Barcode)
            {
                existsInTable = true;
                break;
            }
        }

        if (!existsInTable)
        {
            decimal cost = p.CostPrice ?? 0m;
            decimal marginPercent = p.Price > 0 ? Math.Round(((p.Price - cost) / p.Price) * 100, 1) : 0;
            string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";
            dgvProducts.Rows.Insert(0, p.ProductId, p.Barcode, p.ProductName, p.CategoryName ?? "Chưa phân loại", $"{p.Price:N0} đ", $"{cost:N0} đ", marginText, p.Unit, "🟢 Đang bán");
        }

        // 2. Refresh Card View if active
        if (_currentViewMode == ViewMode.Card)
        {
            RenderCardView();
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Page Header ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Danh Sách Sản Phẩm (Products)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        // --- 2. Top Bar (Search & Actions Card) ---
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(15, 12, 15, 12),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlTopBar);

        // Left Filters Group (FlowLayoutPanel for clean 32px height vertical alignment)
        var pnlFiltersLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        txtSearch = new TextBox
        {
            PlaceholderText = "🔍 Tìm Tên / Barcode...",
            Font = AppTheme.FontBody,
            Size = new Size(180, 32),
            Margin = new Padding(0, 0, 6, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ApplyFilters();
            }
        };

        cbCategory = new ComboBox
        {
            Size = new Size(160, 32),
            DropDownWidth = 240,
            Margin = new Padding(0, 0, 6, 0)
        };
        AppTheme.ApplyComboBoxStyle(cbCategory, 32);
        cbCategory.Items.AddRange(new object[]
        {
            "Tất cả Danh mục",
            "Nước giải khát & Đồ uống",
            "Sữa & Sản phẩm từ sữa",
            "Bánh kẹo & Ăn vặt",
            "Rau củ quả tươi",
            "Gia vị & Đồ khô",
            "Đồ dùng gia đình & Nhà bếp",
            "Hóa phẩm & Giặt xả",
            "Chăm sóc cá nhân",
            "Thịt, Thủy hải sản & Trứng",
            "Đồ ăn liền & Đóng hộp"
        });
        cbCategory.SelectedIndex = 0;
        cbCategory.SelectedIndexChanged += (s, e) => ApplyFilters();

        cbStatus = new ComboBox
        {
            Size = new Size(130, 32),
            Margin = new Padding(0, 0, 6, 0)
        };
        AppTheme.ApplyComboBoxStyle(cbStatus, 32);
        cbStatus.Items.AddRange(new object[] { "Tất cả Trạng thái", "Đang bán (Active)", "Ngừng bán (Inactive)" });
        cbStatus.SelectedIndex = 0;
        cbStatus.SelectedIndexChanged += (s, e) => ApplyFilters();

        btnSearch = new Button
        {
            Text = "Tìm kiếm",
            Size = new Size(75, 32),
            Margin = new Padding(0)
        };
        AppTheme.ApplySecondaryButton(btnSearch);
        btnSearch.Click += (s, e) => ApplyFilters();

        pnlFiltersLeft.Controls.Add(txtSearch);
        pnlFiltersLeft.Controls.Add(cbCategory);
        pnlFiltersLeft.Controls.Add(cbStatus);
        pnlFiltersLeft.Controls.Add(btnSearch);

        // Right Actions Group (Docked to Right Edge for Perfect Alignment)
        var pnlActionsRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        // Mode Switch Buttons
        btnViewTable = new Button
        {
            Text = "📋 Bảng",
            Size = new Size(78, 32),
            Margin = new Padding(0, 0, 5, 0),
            Font = AppTheme.FontBodyBold,
            Cursor = Cursors.Hand
        };

        btnViewCard = new Button
        {
            Text = "🎴 Dạng Thẻ",
            Size = new Size(110, 32),
            Margin = new Padding(0, 0, 10, 0),
            Font = AppTheme.FontBodyBold,
            Cursor = Cursors.Hand
        };

        btnViewTable.Click += (s, e) => SwitchViewMode(ViewMode.Table);
        btnViewCard.Click += (s, e) => SwitchViewMode(ViewMode.Card);

        btnAddProduct = new Button
        {
            Text = "➕ Thêm Sản Phẩm",
            Size = new Size(160, 32),
            Margin = new Padding(0, 0, 5, 0)
        };
        AppTheme.ApplyPrimaryButton(btnAddProduct);
        btnAddProduct.Click += BtnAddProduct_Click;

        btnScanBarcode = new Button
        {
            Text = "📷 Quét",
            Size = new Size(80, 32),
            Margin = new Padding(0)
        };
        AppTheme.ApplySecondaryButton(btnScanBarcode);
        btnScanBarcode.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng quét barcode sản phẩm...");

        pnlActionsRight.Controls.Add(btnViewTable);
        pnlActionsRight.Controls.Add(btnViewCard);
        pnlActionsRight.Controls.Add(btnAddProduct);
        pnlActionsRight.Controls.Add(btnScanBarcode);

        pnlTopBar.Controls.Add(pnlActionsRight);
        pnlTopBar.Controls.Add(pnlFiltersLeft);

        // --- 3. Content Container (Table or Card Grid) ---
        pnlContentContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        // DataGrid Table View
        dgvProducts = new DataGridView();
        AppTheme.ApplyGridStyle(dgvProducts);
        dgvProducts.Columns.Add("ProductId", "ID");
        dgvProducts.Columns.Add("Barcode", "Mã Vạch");
        dgvProducts.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvProducts.Columns.Add("CategoryName", "Danh Mục");
        dgvProducts.Columns.Add("Price", "Giá Bán (VNĐ)");
        dgvProducts.Columns.Add("CostPrice", "Giá Vốn (VNĐ)");
        dgvProducts.Columns.Add("Margin", "Biên Lợi Nhuận");
        dgvProducts.Columns.Add("Unit", "Đơn Vị");
        dgvProducts.Columns.Add("MfgDate", "Ngày SX");
        dgvProducts.Columns.Add("ExpDate", "Hạn Sử Dụng");
        dgvProducts.Columns.Add("Status", "Trạng Thái");

        // FlowLayoutPanel Card Grid View (Responsive Web-like Grid)
        flpCardView = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            WrapContents = true,
            Padding = new Padding(8, 8, 8, 16),
            Visible = false
        };
        flpCardView.Resize += (s, e) => UpdateResponsiveCardLayout();

        pnlContentContainer.Controls.Add(dgvProducts);
        pnlContentContainer.Controls.Add(flpCardView);

        this.Controls.Add(pnlContentContainer);
        this.Controls.Add(pnlTopBar);
        this.Controls.Add(pnlHeader);

        UpdateModeButtonsUI();
    }

    private void SwitchViewMode(ViewMode mode)
    {
        _currentViewMode = mode;
        UpdateModeButtonsUI();

        if (mode == ViewMode.Table)
        {
            flpCardView.Visible = false;
            dgvProducts.Visible = true;
        }
        else
        {
            dgvProducts.Visible = false;
            flpCardView.Visible = true;
            RenderCardView();
        }
    }

    private void UpdateModeButtonsUI()
    {
        if (_currentViewMode == ViewMode.Table)
        {
            AppTheme.ApplyPrimaryButton(btnViewTable);
            AppTheme.ApplyOutlineButton(btnViewCard);
        }
        else
        {
            AppTheme.ApplyOutlineButton(btnViewTable);
            AppTheme.ApplyPrimaryButton(btnViewCard);
        }
    }

    private void ApplyFilters()
    {
        if (_currentViewMode == ViewMode.Card)
        {
            RenderCardView();
        }
        else
        {
            LoadDataAsync();
        }
    }

    private async void LoadDataAsync()
    {
        try
        {
            string searchParam = Uri.EscapeDataString(txtSearch.Text.Trim());
            string url = $"{_apiBaseUrl}/api/products?page=1&pageSize=500";
            if (!string.IsNullOrEmpty(searchParam))
            {
                url += $"&search={searchParam}";
            }

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                JsonElement items = default;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    items = root;
                }
                else if (root.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Array)
                        items = data;
                    else if (data.TryGetProperty("items", out var dataItems))
                        items = dataItems;
                }

                if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                {
                    dgvProducts.Rows.Clear();
                    foreach (var item in items.EnumerateArray())
                    {
                        int id = item.TryGetProperty("productId", out var pId) ? pId.GetInt32() : (item.TryGetProperty("id", out var id2) ? id2.GetInt32() : 0);
                        string barcode = item.TryGetProperty("barcode", out var pBc) ? (pBc.GetString() ?? "") : "";
                        string name = item.TryGetProperty("productName", out var pNm) ? (pNm.GetString() ?? "") : (item.TryGetProperty("name", out var n2) ? (n2.GetString() ?? "") : "");
                        string category = item.TryGetProperty("categoryName", out var pCat) && pCat.ValueKind != JsonValueKind.Null ? (pCat.GetString() ?? "Chưa phân loại") : "Chưa phân loại";
                        decimal price = item.TryGetProperty("price", out var pPr) ? pPr.GetDecimal() : (item.TryGetProperty("sellingPrice", out var sp) ? sp.GetDecimal() : 0);
                        decimal cost = item.TryGetProperty("costPrice", out var pCost) && pCost.ValueKind != JsonValueKind.Null ? pCost.GetDecimal() : 0;
                        string unit = item.TryGetProperty("unit", out var pUn) ? (pUn.GetString() ?? "") : "";
                        string imageUrl = item.TryGetProperty("imageUrl", out var pImg) && pImg.ValueKind != JsonValueKind.Null ? (pImg.GetString() ?? "") : "";

                        string statusText = "🟢 Đang bán";
                        if (item.TryGetProperty("status", out var pStatus))
                        {
                            if (pStatus.ValueKind == JsonValueKind.Number && pStatus.GetInt32() != 1)
                                statusText = "🔴 Ngừng bán";
                            else if (pStatus.ValueKind == JsonValueKind.String && pStatus.GetString()?.Equals("Active", StringComparison.OrdinalIgnoreCase) == false)
                                statusText = "🔴 Ngừng bán";
                        }

                        DateTime? mfgDate = null;
                        if (item.TryGetProperty("manufacturingDate", out var pMfg) && pMfg.ValueKind == JsonValueKind.String && DateTime.TryParse(pMfg.GetString(), out var parsedMfg))
                        {
                            mfgDate = parsedMfg;
                        }

                        DateTime? expDate = null;
                        if (item.TryGetProperty("expiryDate", out var pExp) && pExp.ValueKind == JsonValueKind.String && DateTime.TryParse(pExp.GetString(), out var parsedExp))
                        {
                            expDate = parsedExp;
                        }

                        string mfgText = mfgDate.HasValue ? mfgDate.Value.ToString("dd/MM/yyyy") : "-";
                        string expText = expDate.HasValue ? expDate.Value.ToString("dd/MM/yyyy") : "-";
                        if (expDate.HasValue)
                        {
                            if (expDate.Value < DateTime.UtcNow) expText += " ❌ Hết hạn";
                            else if ((expDate.Value - DateTime.UtcNow).TotalDays <= 30) expText += " ⚠️ Cận date";
                        }

                        decimal marginPercent = price > 0 ? Math.Round(((price - cost) / price) * 100, 1) : 0;
                        string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";

                        dgvProducts.Rows.Add(id, barcode, name, category, $"{price:N0} đ", $"{cost:N0} đ", marginText, unit, mfgText, expText, statusText);

                        // Save to DataStore
                        DataStore.AddProduct(new ProductDto
                        {
                            ProductId = id,
                            Barcode = barcode,
                            ProductName = name,
                            CategoryName = category,
                            Price = price,
                            CostPrice = cost,
                            Unit = unit,
                            ImageUrl = imageUrl,
                            ManufacturingDate = mfgDate,
                            ExpiryDate = expDate
                        });
                    }
                    if (_currentViewMode == ViewMode.Card) RenderCardView();
                    return;
                }
            }
        }
        catch
        {
            // API Connection offline or loading error
        }

        // Load stored products from DataStore if grid is empty
        if (dgvProducts.Rows.Count == 0 && DataStore.Products.Count > 0)
        {
            foreach (var p in DataStore.Products)
            {
                decimal cost = p.CostPrice ?? 0m;
                decimal marginPercent = p.Price > 0 ? Math.Round(((p.Price - cost) / p.Price) * 100, 1) : 0;
                string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";
                string mfg = p.ManufacturingDate.HasValue ? p.ManufacturingDate.Value.ToString("dd/MM/yyyy") : "-";
                string exp = p.ExpiryDate.HasValue ? p.ExpiryDate.Value.ToString("dd/MM/yyyy") : "-";
                dgvProducts.Rows.Add(p.ProductId, p.Barcode, p.ProductName, p.CategoryName ?? "Chưa phân loại", $"{p.Price:N0} đ", $"{cost:N0} đ", marginText, p.Unit, mfg, exp, "🟢 Đang bán");
            }
        }

        if (_currentViewMode == ViewMode.Card)
        {
            RenderCardView();
        }
    }

    private int _lastCalculatedCardWidth = 0;

    private int CalculateResponsiveCardWidth(out int columns, out int gap)
    {
        gap = 14;
        int scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 8;
        int availableWidth = flpCardView.ClientSize.Width - flpCardView.Padding.Horizontal - scrollbarAllowance;
        if (availableWidth < 260) availableWidth = 260;

        int minCardWidth = 220;
        columns = Math.Max(1, (availableWidth + gap) / (minCardWidth + gap));

        int totalGaps = (columns - 1) * gap;
        int cardWidth = Math.Max(minCardWidth, (availableWidth - totalGaps) / columns);
        return cardWidth;
    }

    private void UpdateResponsiveCardLayout()
    {
        if (_currentViewMode != ViewMode.Card || flpCardView.Controls.Count == 0) return;

        int cardWidth = CalculateResponsiveCardWidth(out int columns, out int gap);
        if (cardWidth == _lastCalculatedCardWidth) return;
        _lastCalculatedCardWidth = cardWidth;

        flpCardView.SuspendLayout();
        try
        {
            int index = 0;
            foreach (Control ctrl in flpCardView.Controls)
            {
                if (ctrl is Panel card && card.Tag is ProductDto)
                {
                    int colIndex = index % columns;
                    int rightMargin = (colIndex == columns - 1) ? 0 : gap;
                    card.Margin = new Padding(0, 0, rightMargin, gap);

                    if (card.Width != cardWidth)
                    {
                        card.Width = cardWidth;
                        AdjustCardInnerControls(card, cardWidth);
                    }
                    index++;
                }
            }
        }
        finally
        {
            flpCardView.ResumeLayout(true);
        }
    }

    private void AdjustCardInnerControls(Panel card, int cardWidth)
    {
        foreach (Control c in card.Controls)
        {
            switch (c.Name)
            {
                case "picProduct":
                    c.Width = cardWidth - 16;
                    break;
                case "pnlCategoryBar":
                    c.Width = cardWidth - 16;
                    break;
                case "lblName":
                case "lblPrice":
                case "lblCostMargin":
                case "pnlDivider":
                case "lblIdBarcode":
                    c.Width = cardWidth - 20;
                    break;
            }
        }
    }

    private void RenderCardView()
    {
        flpCardView.Controls.Clear();
        string kw = txtSearch.Text.Trim();

        var products = DataStore.Products.AsEnumerable();
        if (!string.IsNullOrEmpty(kw))
        {
            products = products.Where(p => p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase) || p.Barcode.Contains(kw));
        }

        if (cbCategory.SelectedIndex > 0)
        {
            string selectedCat = cbCategory.SelectedItem?.ToString() ?? "";
            products = products.Where(p => !string.IsNullOrEmpty(p.CategoryName) && 
                (p.CategoryName.Contains(selectedCat, StringComparison.OrdinalIgnoreCase) || 
                 selectedCat.Contains(p.CategoryName, StringComparison.OrdinalIgnoreCase)));
        }

        if (cbStatus.SelectedIndex == 1)
        {
            products = products.Where(p => p.Status == SmartSupermarket.Backend.Domain.Enums.ProductStatus.Active);
        }
        else if (cbStatus.SelectedIndex == 2)
        {
            products = products.Where(p => p.Status == SmartSupermarket.Backend.Domain.Enums.ProductStatus.Inactive);
        }

        var list = products.ToList();
        if (list.Count == 0)
        {
            var pnlEmpty = new Panel
            {
                Size = new Size(flpCardView.Width - 40, 140),
                BackColor = AppTheme.SurfaceWhite,
                Margin = new Padding(10)
            };
            AppTheme.ApplyCardPanel(pnlEmpty);
            var lblEmpty = new Label
            {
                Text = "📦 Không tìm thấy sản phẩm nào phù hợp.\n(Hãy thử tìm từ khóa khác hoặc bấm '➕ Thêm Sản Phẩm')",
                Font = AppTheme.FontBodyBold,
                ForeColor = AppTheme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmpty.Controls.Add(lblEmpty);
            flpCardView.Controls.Add(pnlEmpty);
            return;
        }

        int cardWidth = CalculateResponsiveCardWidth(out int columns, out int gap);
        _lastCalculatedCardWidth = cardWidth;

        flpCardView.SuspendLayout();
        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                int colIndex = i % columns;
                int rightMargin = (colIndex == columns - 1) ? 0 : gap;
                var card = CreateProductCard(p, cardWidth, rightMargin, gap);
                flpCardView.Controls.Add(card);
            }
        }
        finally
        {
            flpCardView.ResumeLayout(true);
        }
    }

    private Panel CreateProductCard(ProductDto p, int cardWidth, int rightMargin, int gap)
    {
        var card = new Panel
        {
            Width = cardWidth,
            Height = 318,
            BackColor = AppTheme.SurfaceWhite,
            Margin = new Padding(0, 0, rightMargin, gap),
            Cursor = Cursors.Hand,
            Tag = p
        };

        bool isHovered = false;

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = AppTheme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            using var bgBrush = new SolidBrush(AppTheme.SurfaceWhite);
            e.Graphics.FillPath(bgBrush, path);

            Color borderColor = isHovered ? AppTheme.Primary : Color.FromArgb(226, 232, 240);
            float borderWidth = isHovered ? 1.5f : 1f;
            using var borderPen = new Pen(borderColor, borderWidth);
            e.Graphics.DrawPath(borderPen, path);
        };

        card.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
        card.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };

        // 1. Image Box Container (Top)
        var picProduct = new PictureBox
        {
            Name = "picProduct",
            Location = new Point(8, 8),
            Size = new Size(cardWidth - 16, 140),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        ImageHelper.LoadProductImageAsync(picProduct, p.ImageUrl, p.CategoryName, p.ProductName);

        picProduct.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
        picProduct.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };

        // Floating Profit Badge (Góc trên bên trái của ảnh)
        decimal cost = p.CostPrice ?? 0m;
        decimal marginPercent = p.Price > 0 ? Math.Round(((p.Price - cost) / p.Price) * 100, 1) : 0;
        bool isProfitable = marginPercent >= 0;

        Color profitBg = isProfitable ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 226, 226);
        Color profitText = isProfitable ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28);
        string profitLabel = isProfitable ? $"▲ Lãi {marginPercent}%" : $"▼ Lỗ {Math.Abs(marginPercent)}%";

        var lblProfitBadge = new Label
        {
            Text = profitLabel,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = profitText,
            BackColor = profitBg,
            Location = new Point(14, 14),
            Padding = new Padding(5, 2, 5, 2),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        // Near Expiry or Expired Badge (Góc trên bên phải của ảnh nếu có)
        Label? lblExpiryBadge = null;
        if (p.IsExpired)
        {
            lblExpiryBadge = new Label
            {
                Text = "❌ Hết hạn",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
                Padding = new Padding(5, 2, 5, 2),
                AutoSize = true
            };
        }
        else if (p.IsNearExpiry)
        {
            lblExpiryBadge = new Label
            {
                Text = "⚠️ Cận date",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(217, 119, 6),
                Padding = new Padding(5, 2, 5, 2),
                AutoSize = true
            };
        }

        if (lblExpiryBadge != null)
        {
            lblExpiryBadge.Location = new Point(cardWidth - lblExpiryBadge.PreferredWidth - 14, 14);
            lblExpiryBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }

        // 2. Category & Unit Chip Bar (ngay dưới ảnh, không che chữ)
        var pnlCategoryBar = new FlowLayoutPanel
        {
            Name = "pnlCategoryBar",
            Location = new Point(8, 154),
            Size = new Size(cardWidth - 16, 24),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var lblCategoryChip = new Label
        {
            Text = p.CategoryName ?? "Sản phẩm",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(6, 2, 6, 2),
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 0)
        };

        var lblUnitChip = new Label
        {
            Text = p.Unit,
            Font = new Font("Segoe UI", 8f, FontStyle.Regular),
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(6, 2, 6, 2),
            AutoSize = true,
            Margin = Padding.Empty
        };

        pnlCategoryBar.Controls.Add(lblCategoryChip);
        pnlCategoryBar.Controls.Add(lblUnitChip);

        // 3. Product Title (2 dòng, gọn gàng, có AutoEllipsis)
        var lblName = new Label
        {
            Name = "lblName",
            Text = p.ProductName,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(10, 182),
            Size = new Size(cardWidth - 20, 38),
            AutoEllipsis = true
        };

        var toolTip = new ToolTip();
        toolTip.SetToolTip(lblName, p.ProductName);

        // 4. Selling Price
        var lblPrice = new Label
        {
            Name = "lblPrice",
            Text = $"{p.Price:N0} đ",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            Location = new Point(10, 224),
            Size = new Size(cardWidth - 20, 24),
            AutoEllipsis = true
        };

        // 5. Cost price & Gross profit line
        string diffText = isProfitable ? $"+{p.GrossProfit:N0} đ" : $"-{Math.Abs(p.GrossProfit):N0} đ";
        var lblCostMargin = new Label
        {
            Name = "lblCostMargin",
            Text = $"Vốn: {cost:N0} đ • Chênh: {diffText}",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(10, 252),
            Size = new Size(cardWidth - 20, 18),
            AutoEllipsis = true
        };

        // 6. Subtle Divider Line
        var pnlDivider = new Panel
        {
            Name = "pnlDivider",
            Location = new Point(10, 276),
            Size = new Size(cardWidth - 20, 1),
            BackColor = Color.FromArgb(241, 245, 249)
        };

        // 7. Footer: ID & Barcode
        var lblIdBarcode = new Label
        {
            Name = "lblIdBarcode",
            Text = $"#{p.ProductId} • 📟 {p.Barcode}",
            Font = new Font("Segoe UI", 8f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(10, 284),
            Size = new Size(cardWidth - 20, 18),
            AutoEllipsis = true
        };

        card.Controls.Add(lblProfitBadge);
        if (lblExpiryBadge != null) card.Controls.Add(lblExpiryBadge);
        lblProfitBadge.BringToFront();
        if (lblExpiryBadge != null) lblExpiryBadge.BringToFront();

        card.Controls.Add(picProduct);
        card.Controls.Add(pnlCategoryBar);
        card.Controls.Add(lblName);
        card.Controls.Add(lblPrice);
        card.Controls.Add(lblCostMargin);
        card.Controls.Add(pnlDivider);
        card.Controls.Add(lblIdBarcode);

        // Click / Double click to view details
        card.DoubleClick += (s, e) => ShowProductDetailQuick(p);
        lblName.DoubleClick += (s, e) => ShowProductDetailQuick(p);

        return card;
    }

    private void ShowProductDetailQuick(ProductDto p)
    {
        string mfg = p.ManufacturingDate.HasValue ? p.ManufacturingDate.Value.ToString("dd/MM/yyyy") : "N/A";
        string exp = p.ExpiryDate.HasValue ? p.ExpiryDate.Value.ToString("dd/MM/yyyy") : "N/A";
        string msg = $"📦 {p.ProductName}\n" +
                     $"• Barcode: {p.Barcode}\n" +
                     $"• Danh mục: {p.CategoryName}\n" +
                     $"• Giá bán: {p.Price:N0} đ / {p.Unit}\n" +
                     $"• Giá vốn: {p.CostPrice:N0} đ (Lợi nhuận: {p.GrossProfit:N0} đ)\n" +
                     $"• Ngày SX: {mfg} | HSD: {exp}\n" +
                     $"• Trạng thái: {p.StatusName}";
        MessageBox.Show(msg, "Chi tiết sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnAddProduct_Click(object? sender, EventArgs e)
    {
        var form = new AddProductForm(_httpClient, _apiBaseUrl);
        if (form.ShowDialog(this.FindForm()) == DialogResult.OK)
        {
            if (form.CreatedProduct != null)
            {
                var p = form.CreatedProduct;
                decimal cost = p.CostPrice ?? 0m;
                decimal marginPercent = p.Price > 0 ? Math.Round(((p.Price - cost) / p.Price) * 100, 1) : 0;
                string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";
                dgvProducts.Rows.Insert(0, p.ProductId, p.Barcode, p.ProductName, p.CategoryName, $"{p.Price:N0} đ", $"{cost:N0} đ", marginText, p.Unit, "🟢 Đang bán");

                if (_currentViewMode == ViewMode.Card)
                {
                    RenderCardView();
                }
            }
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Thêm sản phẩm mới thành công!");
        }
    }
}
