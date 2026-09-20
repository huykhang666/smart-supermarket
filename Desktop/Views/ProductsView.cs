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
            Size = new Size(140, 32),
            Margin = new Padding(0, 0, 6, 0)
        };
        AppTheme.ApplyComboBoxStyle(cbCategory, 32);
        cbCategory.Items.AddRange(new object[] { "Tất cả Danh mục", "Nước giải khát", "Sữa & Chế phẩm", "Rau củ quả tươi", "Bánh kẹo & Snack" });
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
        dgvProducts.Columns.Add("Status", "Trạng Thái");

        // FlowLayoutPanel Card Grid View
        flpCardView = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            WrapContents = true,
            Padding = new Padding(4),
            Visible = false
        };

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
            string url = $"{_apiBaseUrl}/api/products?page=1&pageSize=100";
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

                        string statusText = "🟢 Đang bán";
                        if (item.TryGetProperty("status", out var pStatus))
                        {
                            if (pStatus.ValueKind == JsonValueKind.Number && pStatus.GetInt32() != 1)
                                statusText = "🔴 Ngừng bán";
                            else if (pStatus.ValueKind == JsonValueKind.String && pStatus.GetString()?.Equals("Active", StringComparison.OrdinalIgnoreCase) == false)
                                statusText = "🔴 Ngừng bán";
                        }

                        decimal marginPercent = price > 0 ? Math.Round(((price - cost) / price) * 100, 1) : 0;
                        string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";

                        dgvProducts.Rows.Add(id, barcode, name, category, $"{price:N0} đ", $"{cost:N0} đ", marginText, unit, statusText);

                        // Save to DataStore
                        DataStore.AddProduct(new ProductDto
                        {
                            ProductId = id,
                            Barcode = barcode,
                            ProductName = name,
                            CategoryName = category,
                            Price = price,
                            CostPrice = cost,
                            Unit = unit
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
                dgvProducts.Rows.Add(p.ProductId, p.Barcode, p.ProductName, p.CategoryName ?? "Chưa phân loại", $"{p.Price:N0} đ", $"{cost:N0} đ", marginText, p.Unit, "🟢 Đang bán");
            }
        }

        if (_currentViewMode == ViewMode.Card)
        {
            RenderCardView();
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

        var list = products.ToList();
        if (list.Count == 0)
        {
            var pnlEmpty = new Panel
            {
                Size = new Size(flpCardView.Width - 40, 120),
                BackColor = AppTheme.SurfaceWhite,
                Margin = new Padding(10)
            };
            AppTheme.ApplyCardPanel(pnlEmpty);
            var lblEmpty = new Label
            {
                Text = "📦 Chưa có sản phẩm nào để hiển thị dạng thẻ.\n(Hãy bấm '+ Thêm Sản Phẩm' hoặc quét Barcode để thêm mới)",
                Font = AppTheme.FontBodyBold,
                ForeColor = AppTheme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmpty.Controls.Add(lblEmpty);
            flpCardView.Controls.Add(pnlEmpty);
            return;
        }

        foreach (var p in list)
        {
            var card = CreateProductCard(p);
            flpCardView.Controls.Add(card);
        }
    }

    private Panel CreateProductCard(ProductDto p)
    {
        var card = new Panel
        {
            Size = new Size(225, 335),
            BackColor = AppTheme.SurfaceWhite,
            Margin = new Padding(0, 0, 16, 16)
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = AppTheme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            using var bgBrush = new SolidBrush(AppTheme.SurfaceWhite);
            e.Graphics.FillPath(bgBrush, path);
            using var borderPen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawPath(borderPen, path);
        };

        // 1. Image Box Container (Top)
        var picProduct = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(205, 140),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };

        if (!string.IsNullOrWhiteSpace(p.ImageUrl) && File.Exists(p.ImageUrl))
        {
            try
            {
                using var stream = new FileStream(p.ImageUrl, FileMode.Open, FileAccess.Read);
                picProduct.Image = Image.FromStream(stream);
            }
            catch { }
        }
        if (picProduct.Image == null)
        {
            // Fallback product card icon drawing
            var bmp = new Bitmap(205, 140);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(245, 248, 252));
            using var font = new Font("Segoe UI", 32f);
            using var brush = new SolidBrush(AppTheme.Primary);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("📦", font, brush, new RectangleF(0, 0, 205, 140), sf);
            picProduct.Image = bmp;
        }

        decimal cost = p.CostPrice ?? 0m;
        decimal marginPercent = p.Price > 0 ? Math.Round(((p.Price - cost) / p.Price) * 100, 1) : 0;
        bool isProfitable = marginPercent >= 0;

        Color bannerBgColor = isProfitable ? AppTheme.Success : AppTheme.Danger;
        string marginTextLabel = isProfitable ? $"Lãi {marginPercent}%" : $"Lỗ {Math.Abs(marginPercent)}%";

        // 2. Middle Banner (Category / Lãi Badge) - Dynamic Green/Red status
        var pnlBanner = new Panel
        {
            Location = new Point(0, 155),
            Size = new Size(225, 30),
            BackColor = bannerBgColor
        };

        var lblTagPill = new Label
        {
            Text = marginTextLabel,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = bannerBgColor,
            BackColor = Color.White,
            Location = new Point(6, 4),
            Padding = new Padding(4, 2, 4, 2),
            AutoSize = true
        };

        var lblBannerText = new Label
        {
            Text = $"{p.CategoryName ?? "Sản Phẩm"} • {p.Unit}",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        pnlBanner.Controls.Add(lblTagPill);
        pnlBanner.Controls.Add(lblBannerText);

        // Calculate location after adding to control hierarchy to ensure PreferredSize is computed
        pnlBanner.Layout += (s, e) =>
        {
            lblBannerText.Location = new Point(lblTagPill.Right + 6, 5);
        };
        lblBannerText.Location = new Point(70, 5); // Initial fallback

        // 3. Product Title
        var lblName = new Label
        {
            Text = p.ProductName,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(10, 192),
            Size = new Size(205, 42)
        };

        // 4. Selling Price
        var lblPrice = new Label
        {
            Text = $"{p.Price:N0}đ / {p.Unit}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            Location = new Point(10, 238),
            AutoSize = true
        };

        // 5. Product ID & Barcode Badge Footer (Slate Blue Text)
        var lblIdBarcode = new Label
        {
            Text = $"ID: #{p.ProductId} • Barcode: {p.Barcode}",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(10, 298),
            AutoSize = true
        };

        var lblCostDetail = new Label
        {
            Text = $"Giá vốn: {cost:N0} đ",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(10, 274),
            AutoSize = true
        };

        card.Controls.Add(picProduct);
        card.Controls.Add(pnlBanner);
        card.Controls.Add(lblName);
        card.Controls.Add(lblPrice);
        card.Controls.Add(lblCostDetail);
        card.Controls.Add(lblIdBarcode);

        return card;
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
