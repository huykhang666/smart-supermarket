using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop.Views;

public class InventoryView : UserControl
{
    private Panel pnlTopBar = null!;
    private TextBox txtSearch = null!;
    private ComboBox cbStatus = null!;
    private Button btnSearch = null!;
    private Button btnAdjustStock = null!;
    private DataGridView dgvInventory = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5000";

    public InventoryView()
    {
        InitializeComponent();
        LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        // --- Top Bar ---
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        txtSearch = new TextBox
        {
            PlaceholderText = "🔍 Tìm theo tên sản phẩm...",
            Font = new Font("Segoe UI", 10f),
            Size = new Size(300, 32),
            Location = new Point(15, 18),
            BorderStyle = BorderStyle.FixedSingle
        };

        cbStatus = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Size = new Size(180, 32),
            Location = new Point(330, 18),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbStatus.Items.AddRange(new object[] { "Tất cả", "Sắp hết hàng (Low)", "Hết hàng (Out)" });
        cbStatus.SelectedIndex = 0;

        btnSearch = new Button
        {
            Text = "Lọc & Tìm kiếm",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 32),
            Location = new Point(530, 18),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => LoadDataAsync();

        btnAdjustStock = new Button
        {
            Text = "⚖️ Điều chỉnh Tồn kho",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(250, 140, 22),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 32),
            Location = new Point(680, 18),
            Cursor = Cursors.Hand
        };
        btnAdjustStock.FlatAppearance.BorderSize = 0;
        btnAdjustStock.Click += BtnAdjustStock_Click;

        pnlTopBar.Controls.Add(txtSearch);
        pnlTopBar.Controls.Add(cbStatus);
        pnlTopBar.Controls.Add(btnSearch);
        pnlTopBar.Controls.Add(btnAdjustStock);

        // --- DataGrid ---
        dgvInventory = new DataGridView
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

        dgvInventory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvInventory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvInventory.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 70, 80);
        dgvInventory.EnableHeadersVisualStyles = false;

        dgvInventory.Columns.Add("InventoryId", "Mã Tồn Kho");
        dgvInventory.Columns.Add("ProductId", "Mã SP");
        dgvInventory.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvInventory.Columns.Add("Quantity", "Số lượng Tồn");
        dgvInventory.Columns.Add("MinStock", "Mức Tối Thiểu");
        dgvInventory.Columns.Add("Status", "Tình trạng");

        this.Controls.Add(dgvInventory);
        this.Controls.Add(pnlTopBar);
    }

    private async void LoadDataAsync()
    {
        dgvInventory.Rows.Clear();
        try
        {
            string status = cbStatus.SelectedIndex switch { 1 => "low", 2 => "out", _ => "" };
            string search = txtSearch.Text;
            string url = $"{_apiBaseUrl}/api/inventory?page=1&pageSize=50&status={status}&search={search}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        int invId = item.GetProperty("inventoryId").GetInt32();
                        int pId = item.GetProperty("productId").GetInt32();
                        string pName = item.GetProperty("product").GetProperty("productName").GetString() ?? "";
                        int qty = item.GetProperty("quantityOnHand").GetInt32();
                        int minQty = item.GetProperty("minStockLevel").GetInt32();

                        string stockStatus = qty == 0 ? "🔴 Hết hàng" : (qty <= minQty ? "⚠️ Sắp hết" : "🟢 Ổn định");

                        dgvInventory.Rows.Add(invId, pId, pName, qty, minQty, stockStatus);
                    }
                    return;
                }
            }
        }
        catch { }

        // Mock data
        dgvInventory.Rows.Add(1, 1, "Nước ngọt Coca-Cola Lon 330ml", 50, 10, "🟢 Ổn định");
        dgvInventory.Rows.Add(2, 2, "Sữa tươi Vinamilk Có đường 1L", 5, 10, "⚠️ Sắp hết");
        dgvInventory.Rows.Add(3, 3, "Bánh mì tươi Kinh Đô 80g", 0, 5, "🔴 Hết hàng");
    }

    private void BtnAdjustStock_Click(object? sender, EventArgs e)
    {
        if (dgvInventory.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn một sản phẩm để điều chỉnh kho.", "Thông báo");
            return;
        }

        int productId = (int)dgvInventory.SelectedRows[0].Cells["ProductId"].Value;
        string productName = dgvInventory.SelectedRows[0].Cells["ProductName"].Value.ToString() ?? "";
        int currentQty = (int)dgvInventory.SelectedRows[0].Cells["Quantity"].Value;

        var form = new InventoryAdjustForm(productId, productName, currentQty, _httpClient, _apiBaseUrl);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDataAsync();
        }
    }
}
