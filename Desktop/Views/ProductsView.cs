using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class ProductsView : UserControl
{
    private Panel pnlTopBar = null!;
    private TextBox txtSearch = null!;
    private ComboBox cbCategory = null!;
    private ComboBox cbStatus = null!;
    private Button btnSearch = null!;
    private Button btnAddProduct = null!;
    private Button btnScanBarcode = null!;
    private DataGridView dgvProducts = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public ProductsView()
    {
        InitializeComponent();
        LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        // --- Top Bar (Search & Actions Card) ---
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlTopBar);

        txtSearch = new TextBox
        {
            PlaceholderText = "🔍 Tìm theo Tên sản phẩm hoặc Barcode...",
            Font = ThemeManager.BodyFont,
            Size = new Size(300, 32),
            Location = new Point(15, 16),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadDataAsync();
            }
        };

        cbCategory = new ComboBox
        {
            Font = ThemeManager.BodyFont,
            Size = new Size(180, 32),
            Location = new Point(325, 16),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbCategory.Items.AddRange(new object[] { "Tất cả Danh mục", "Nước giải khát", "Sữa & Chế phẩm", "Rau củ quả tươi", "Bánh kẹo & Snack" });
        cbCategory.SelectedIndex = 0;

        cbStatus = new ComboBox
        {
            Font = ThemeManager.BodyFont,
            Size = new Size(150, 32),
            Location = new Point(515, 16),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbStatus.Items.AddRange(new object[] { "Tất cả Trạng thái", "Đang bán (Active)", "Ngừng bán (Inactive)" });
        cbStatus.SelectedIndex = 0;

        btnSearch = new Button
        {
            Text = "Tìm kiếm",
            Size = new Size(95, 32),
            Location = new Point(675, 16)
        };
        ThemeManager.ApplyPrimaryButton(btnSearch);
        btnSearch.Click += (s, e) => LoadDataAsync();

        btnAddProduct = new Button
        {
            Text = "➕ Thêm Sản Phẩm (F1)",
            Size = new Size(170, 32),
            Location = new Point(780, 16)
        };
        ThemeManager.ApplySecondaryButton(btnAddProduct);
        btnAddProduct.Click += BtnAddProduct_Click;

        btnScanBarcode = new Button
        {
            Text = "📷 Quét Barcode",
            Size = new Size(130, 32),
            Location = new Point(960, 16)
        };
        ThemeManager.ApplySecondaryButton(btnScanBarcode);
        btnScanBarcode.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng quét barcode sản phẩm...");

        pnlTopBar.Controls.Add(txtSearch);
        pnlTopBar.Controls.Add(cbCategory);
        pnlTopBar.Controls.Add(cbStatus);
        pnlTopBar.Controls.Add(btnSearch);
        pnlTopBar.Controls.Add(btnAddProduct);
        pnlTopBar.Controls.Add(btnScanBarcode);

        // --- DataGrid Products Table ---
        dgvProducts = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvProducts);

        dgvProducts.Columns.Add("ProductId", "ID");
        dgvProducts.Columns.Add("Barcode", "Mã Vạch");
        dgvProducts.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvProducts.Columns.Add("CategoryName", "Danh Mục");
        dgvProducts.Columns.Add("Price", "Giá Bán (VNĐ)");
        dgvProducts.Columns.Add("CostPrice", "Giá Vốn (VNĐ)");
        dgvProducts.Columns.Add("Margin", "Biên Lợi Nhuận");
        dgvProducts.Columns.Add("Unit", "Đơn Vị");
        dgvProducts.Columns.Add("Status", "Trạng Thái");

        this.Controls.Add(dgvProducts);
        this.Controls.Add(pnlTopBar);
    }

    private async void LoadDataAsync()
    {
        dgvProducts.Rows.Clear();

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
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        int id = item.GetProperty("productId").GetInt32();
                        string barcode = item.GetProperty("barcode").GetString() ?? "";
                        string name = item.GetProperty("productName").GetString() ?? "";
                        string category = item.TryGetProperty("categoryName", out var pCat) && pCat.ValueKind != JsonValueKind.Null ? (pCat.GetString() ?? "Chưa phân loại") : "Chưa phân loại";
                        decimal price = item.GetProperty("price").GetDecimal();
                        decimal cost = item.TryGetProperty("costPrice", out var pCost) && pCost.ValueKind != JsonValueKind.Null ? pCost.GetDecimal() : 0;
                        string unit = item.GetProperty("unit").GetString() ?? "";

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
                    }
                }
            }
        }
        catch
        {
            // API Connection offline or loading error - keep empty without fake mock data
        }
    }

    private void BtnAddProduct_Click(object? sender, EventArgs e)
    {
        var form = new AddProductForm(_httpClient, _apiBaseUrl);
        if (form.ShowDialog() == DialogResult.OK)
        {
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Thêm sản phẩm mới thành công!");
            LoadDataAsync();
        }
    }
}
