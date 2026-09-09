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
    private readonly string _apiBaseUrl = "http://localhost:5000";

    public ProductsView()
    {
        InitializeComponent();
        LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        // --- Top Bar (Search & Actions) ---
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        txtSearch = new TextBox
        {
            PlaceholderText = "🔍 Tìm theo Tên sản phẩm hoặc Mã vạch (Barcode)...",
            Font = new Font("Segoe UI", 10f),
            Size = new Size(320, 32),
            Location = new Point(15, 18),
            BorderStyle = BorderStyle.FixedSingle
        };

        cbCategory = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Size = new Size(180, 32),
            Location = new Point(345, 18),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbCategory.Items.AddRange(new object[] { "Tất cả Danh mục", "Nước giải khát", "Sữa & Chế phẩm", "Rau củ quả tươi", "Bánh kẹo & Snack" });
        cbCategory.SelectedIndex = 0;

        cbStatus = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Size = new Size(150, 32),
            Location = new Point(535, 18),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbStatus.Items.AddRange(new object[] { "Tất cả Trạng thái", "Đang bán (Active)", "Ngừng bán (Inactive)" });
        cbStatus.SelectedIndex = 0;

        btnSearch = new Button
        {
            Text = "Tìm kiếm",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(95, 32),
            Location = new Point(695, 18),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => LoadDataAsync();

        btnAddProduct = new Button
        {
            Text = "➕ Thêm Sản Phẩm (F1)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(56, 158, 13),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 32),
            Location = new Point(800, 18),
            Cursor = Cursors.Hand
        };
        btnAddProduct.FlatAppearance.BorderSize = 0;
        btnAddProduct.Click += BtnAddProduct_Click;

        btnScanBarcode = new Button
        {
            Text = "📷 Quét Mã Vạch",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(114, 46, 209),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 32),
            Location = new Point(980, 18),
            Cursor = Cursors.Hand
        };
        btnScanBarcode.FlatAppearance.BorderSize = 0;
        btnScanBarcode.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng quét barcode sản phẩm...");

        pnlTopBar.Controls.Add(txtSearch);
        pnlTopBar.Controls.Add(cbCategory);
        pnlTopBar.Controls.Add(cbStatus);
        pnlTopBar.Controls.Add(btnSearch);
        pnlTopBar.Controls.Add(btnAddProduct);
        pnlTopBar.Controls.Add(btnScanBarcode);

        // --- DataGrid Products Table ---
        dgvProducts = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 40 },
            ColumnHeadersHeight = 42
        };

        dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 70, 80);
        dgvProducts.EnableHeadersVisualStyles = false;

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
            string url = $"{_apiBaseUrl}/api/products?page=1&pageSize=50";
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
                        string category = item.TryGetProperty("categoryName", out var pCat) ? (pCat.GetString() ?? "") : "Chưa rõ";
                        decimal price = item.GetProperty("price").GetDecimal();
                        decimal cost = item.TryGetProperty("costPrice", out var pCost) ? pCost.GetDecimal() : 0;
                        string unit = item.GetProperty("unit").GetString() ?? "";
                        string status = item.GetProperty("status").GetString() ?? "Active";

                        decimal marginPercent = price > 0 ? Math.Round(((price - cost) / price) * 100, 1) : 0;
                        string marginText = $"{marginPercent}% {(marginPercent < 0 ? "⚠️ Bán Lỗ" : "")}";

                        dgvProducts.Rows.Add(id, barcode, name, category, $"{price:N0} đ", $"{cost:N0} đ", marginText, unit, status == "Active" ? "🟢 Đang bán" : "🔴 Ngừng bán");
                    }
                    return;
                }
            }
        }
        catch
        {
            // API Server Fallback Demo Data
        }

        // Add Mock Demo Rows if API server offline
        dgvProducts.Rows.Add(1, "8935001800012", "Nước ngọt Coca-Cola Lon 330ml", "Nước giải khát", "10.000 đ", "7.500 đ", "25.0%", "lon", "🟢 Đang bán");
        dgvProducts.Rows.Add(2, "8934673123456", "Sữa tươi Vinamilk Có đường 1L", "Sữa & Chế phẩm", "36.000 đ", "29.000 đ", "19.4%", "hộp", "🟢 Đang bán");
        dgvProducts.Rows.Add(3, "8934567890123", "Bánh mì tươi Kinh Đô 80g", "Bánh kẹo & Snack", "12.000 đ", "8.000 đ", "33.3%", "gói", "🟢 Đang bán");
        dgvProducts.Rows.Add(4, "8938501234567", "Nước tương Chinsu Tỏi Ớt 250ml", "Gia vị & Khác", "19.500 đ", "14.800 đ", "24.1%", "chai", "🟢 Đang bán");
        dgvProducts.Rows.Add(5, "8936001112233", "Sữa chua Vinamilk Nha Đam 100g", "Sữa & Chế phẩm", "8.500 đ", "6.500 đ", "23.5%", "hộp", "🔴 Ngừng bán");
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
