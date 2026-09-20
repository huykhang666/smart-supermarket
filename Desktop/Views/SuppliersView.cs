using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class SuppliersView : UserControl
{
    private DataGridView dgvSuppliers = null!;
    private Button btnAddSupplier = null!;
    private Button btnImportSupplier = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public SuppliersView()
    {
        InitializeComponent();
        DataStore.SupplierAdded += DataStore_SupplierAdded;
        _ = LoadDataAsync();
    }

    private void DataStore_SupplierAdded(object? sender, SupplierItem s)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => DataStore_SupplierAdded(sender, s)));
            return;
        }

        // Check if row already exists
        foreach (DataGridViewRow row in dgvSuppliers.Rows)
        {
            if (row.Cells["SupplierName"].Value?.ToString() == s.SupplierName)
            {
                return;
            }
        }

        dgvSuppliers.Rows.Insert(0, s.SupplierId, s.SupplierCode, s.SupplierName, s.ContactPerson, s.Phone, s.Email, "⭐ 5.0 / 5.0", "🟢 Hoạt động");
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlTop);

        var lblHeader = new Label
        {
            Text = "🏢 Nhà Cung Cấp & Đối Tác",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var pnlActionsRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        btnAddSupplier = new Button
        {
            Text = "➕ Thêm Nhà Cung Cấp",
            Size = new Size(180, 32),
            Margin = new Padding(0, 0, 8, 0)
        };
        AppTheme.ApplyPrimaryButton(btnAddSupplier);
        btnAddSupplier.Click += (s, e) =>
        {
            using var dlg = new AddSupplierForm(_httpClient, _apiBaseUrl);
            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                var sData = dlg.CreatedSupplierData;
                if (!string.IsNullOrEmpty(sData.name))
                {
                    dgvSuppliers.Rows.Insert(0, sData.id, sData.code, sData.name, sData.contact, sData.phone, sData.email, "⭐ 5.0 / 5.0", "🟢 Hoạt động");
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã lưu nhà cung cấp '{sData.name}' thành công!");
                }
            }
        };

        btnImportSupplier = new Button
        {
            Text = "📥 Import CSV/JSON",
            Size = new Size(160, 32),
            Margin = new Padding(0)
        };
        AppTheme.ApplySecondaryButton(btnImportSupplier);
        btnImportSupplier.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Chọn file CSV để import nhà cung cấp...");

        pnlActionsRight.Controls.Add(btnAddSupplier);
        pnlActionsRight.Controls.Add(btnImportSupplier);

        pnlTop.Controls.Add(lblHeader);
        pnlTop.Controls.Add(pnlActionsRight);

        dgvSuppliers = new DataGridView();
        AppTheme.ApplyGridStyle(dgvSuppliers);

        dgvSuppliers.Columns.Add("SupplierId", "ID");
        dgvSuppliers.Columns.Add("SupplierCode", "Mã NCC");
        dgvSuppliers.Columns.Add("SupplierName", "Tên Nhà Cung Cấp");
        dgvSuppliers.Columns.Add("ContactPerson", "Người Liên Hệ");
        dgvSuppliers.Columns.Add("PhoneNumber", "Số Điện Thoại");
        dgvSuppliers.Columns.Add("Email", "Email");
        dgvSuppliers.Columns.Add("Rating", "Đánh Giá (Rating)");
        dgvSuppliers.Columns.Add("Status", "Trạng Thái");

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlGridContainer);
        ThemeManager.ApplyCardPanel(pnlGridContainer);
        pnlGridContainer.Controls.Add(dgvSuppliers);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlTop);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/suppliers");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                JsonElement itemsElem = default;

                if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.TryGetProperty("items", out var subItems))
                {
                    itemsElem = subItems;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    itemsElem = doc.RootElement;
                }

                if (itemsElem.ValueKind == JsonValueKind.Array && itemsElem.GetArrayLength() > 0)
                {
                    dgvSuppliers.Rows.Clear();
                    foreach (var item in itemsElem.EnumerateArray())
                    {
                        int id = item.TryGetProperty("supplierId", out var idProp) ? idProp.GetInt32() : (item.TryGetProperty("id", out var id2) ? id2.GetInt32() : 0);
                        string code = item.TryGetProperty("supplierCode", out var c) ? c.GetString() ?? "" : $"SUP{id:D5}";
                        string name = item.TryGetProperty("supplierName", out var n) ? n.GetString() ?? "" : (item.TryGetProperty("name", out var n2) ? n2.GetString() ?? "" : "");
                        string contact = item.TryGetProperty("contactPerson", out var cp) ? cp.GetString() ?? "N/A" : "N/A";
                        string phone = item.TryGetProperty("phone", out var p) ? p.GetString() ?? "" : "";
                        string email = item.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";

                        dgvSuppliers.Rows.Add(id, code, name, contact, phone, email, "⭐ 4.8 / 5.0", "🟢 Hoạt động");
                    }
                    return;
                }
            }

        }
        catch
        {
            // Graceful fallback
        }

        // Load stored suppliers from DataStore if grid is empty
        if (dgvSuppliers.Rows.Count == 0 && DataStore.Suppliers.Count > 0)
        {
            foreach (var s in DataStore.Suppliers)
            {
                dgvSuppliers.Rows.Add(s.SupplierId, s.SupplierCode, s.SupplierName, s.ContactPerson, s.Phone, s.Email, "⭐ 5.0 / 5.0", "🟢 Hoạt động");
            }
        }
    }
}
