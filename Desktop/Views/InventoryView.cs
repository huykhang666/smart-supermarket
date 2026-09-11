using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class InventoryView : UserControl
{
    private Panel pnlHeader = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboWarehouse = null!;
    private DataGridView dgvInventory = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public InventoryView()
    {
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        // Header Panel
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlHeader);

        var lblTitle = new Label
        {
            Text = "🏬 QUẢN LÝ TỒN KHO & LÔ HÀNG (INVENTORY & BATCHES)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 12),
            AutoSize = true
        };

        cboWarehouse = new ComboBox
        {
            Font = ThemeManager.BodyFont,
            Size = new Size(180, 32),
            Location = new Point(15, 38),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboWarehouse.Items.AddRange(new[] { "Kho Tổng Siêu Thị", "Kệ Hàng A1 - A5", "Kho Lạnh Thực Phẩm" });
        cboWarehouse.SelectedIndex = 0;

        txtSearch = new TextBox
        {
            Font = ThemeManager.BodyFont,
            Size = new Size(280, 32),
            Location = new Point(210, 38),
            PlaceholderText = "🔍 Tìm tên sản phẩm / Mã lô...",
            BorderStyle = BorderStyle.FixedSingle
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(cboWarehouse);
        pnlHeader.Controls.Add(txtSearch);

        // DataGridView
        dgvInventory = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvInventory);

        dgvInventory.Columns.Add("BatchCode", "Mã Lô Hàng");
        dgvInventory.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvInventory.Columns.Add("Quantity", "Số Lượng Tồn");
        dgvInventory.Columns.Add("ExpiryDate", "Hạn Sử Dụng");
        dgvInventory.Columns.Add("Location", "Vị Trí Kệ");
        dgvInventory.Columns.Add("Status", "Trạng Thái Cảnh Báo");

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGridContainer);
        pnlGridContainer.Controls.Add(dgvInventory);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlHeader);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            dgvInventory.Rows.Clear();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/inventory/batches");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    foreach (var item in dataElem.EnumerateArray())
                    {
                        string code = item.TryGetProperty("batchCode", out var c) ? c.GetString() ?? "" : "";
                        string name = item.TryGetProperty("productName", out var n) ? n.GetString() ?? "" : "";
                        string qty = item.TryGetProperty("quantity", out var q) ? q.GetString() ?? "" : "";
                        string exp = item.TryGetProperty("expiryDate", out var e) ? e.GetString() ?? "" : "";
                        string loc = item.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "";
                        string st = item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";

                        dgvInventory.Rows.Add(code, name, qty, exp, loc, st);
                    }
                }
            }

        }
        catch
        {
            // Graceful fallback
        }
    }
}
