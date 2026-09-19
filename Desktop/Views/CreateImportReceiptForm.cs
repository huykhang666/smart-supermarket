using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class CreateImportReceiptForm : Form
{
    private readonly HttpClient? _httpClient;
    private readonly string _apiBaseUrl = string.Empty;

    private ComboBox cbSupplier = null!;
    private TextBox txtNote = null!;
    private TextBox txtBarcode = null!;
    private Button btnCreate = null!;
    private Button btnCancel = null!;
    private DataGridView dgvItems = null!;
    private Label lblTotalItems = null!;

    private readonly List<ImportItemModel> _items = new();

    public CreateImportReceiptForm()
    {
        InitializeComponent();
    }

    public CreateImportReceiptForm(HttpClient httpClient, string apiBaseUrl) : this()
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;
        LoadSuppliers();
    }

    private void LoadSuppliers()
    {
        cbSupplier.Items.Clear();
        foreach (var s in DataStore.Suppliers)
        {
            cbSupplier.Items.Add(new SupplierItem { Id = s.SupplierId, Name = s.SupplierName });
        }
        cbSupplier.DisplayMember = "Name";
        cbSupplier.ValueMember = "Id";
        if (cbSupplier.Items.Count > 0) cbSupplier.SelectedIndex = 0;
    }

    private void InitializeComponent()
    {
        this.Text = "Tạo Phiếu Nhập Hàng (Có Quét Mã)";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        // --- Header Panel ---
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(16) };
        AppTheme.ApplyCardPanel(pnlHeader);

        Label lblSupplier = new Label { Text = "Nhà cung cấp:", Font = AppTheme.FontBodyBold, Location = new Point(16, 16), AutoSize = true };
        cbSupplier = new ComboBox { Location = new Point(16, 40), Size = new Size(250, 32), Font = AppTheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList };

        Label lblNote = new Label { Text = "Ghi chú phiếu nhập:", Font = AppTheme.FontBodyBold, Location = new Point(280, 16), AutoSize = true };
        txtNote = new TextBox { Location = new Point(280, 40), Size = new Size(480, 32), Font = AppTheme.FontBody };

        Label lblBarcode = new Label { Text = "📷 Quét mã vạch (Barcode):", Font = AppTheme.FontBodyBold, Location = new Point(16, 85), AutoSize = true, ForeColor = AppTheme.Primary };
        txtBarcode = new TextBox { Location = new Point(200, 80), Size = new Size(300, 32), Font = new Font("Segoe UI", 12f, FontStyle.Bold), PlaceholderText = "Tít mã vạch rồi Enter..." };
        txtBarcode.KeyDown += TxtBarcode_KeyDown;

        pnlHeader.Controls.Add(lblSupplier);
        pnlHeader.Controls.Add(cbSupplier);
        pnlHeader.Controls.Add(lblNote);
        pnlHeader.Controls.Add(txtNote);
        pnlHeader.Controls.Add(lblBarcode);
        pnlHeader.Controls.Add(txtBarcode);

        // --- Grid Panel ---
        var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        dgvItems = new DataGridView();
        AppTheme.ApplyGridStyle(dgvItems);
        dgvItems.Columns.Add("Barcode", "Mã SP");
        dgvItems.Columns.Add("Name", "Tên Sản Phẩm");
        dgvItems.Columns.Add("Qty", "Số Lượng");
        dgvItems.Columns.Add("Cost", "Giá Nhập (VNĐ)");
        dgvItems.Columns.Add("Expiry", "Hạn Sử Dụng");
        pnlGrid.Controls.Add(dgvItems);

        // --- Footer Panel ---
        var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = AppTheme.BackgroundGray, Padding = new Padding(16, 10, 16, 10) };
        
        lblTotalItems = new Label { Text = "Tổng mặt hàng: 0", Font = AppTheme.FontBodyBold, Location = new Point(16, 20), AutoSize = true };
        
        btnCancel = new Button { Text = "Hủy bỏ", Size = new Size(100, 36), Location = new Point(550, 12) };
        AppTheme.ApplySecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        btnCreate = new Button { Text = "✅ Lưu Phiếu Nhập", Size = new Size(180, 36), Location = new Point(660, 12) };
        AppTheme.ApplyPrimaryButton(btnCreate);
        btnCreate.Click += BtnCreate_Click;

        pnlFooter.Controls.Add(lblTotalItems);
        pnlFooter.Controls.Add(btnCancel);
        pnlFooter.Controls.Add(btnCreate);

        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            string code = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            var dsProd = DataStore.FindProductByBarcode(code);
            if (dsProd != null)
            {
                // Add to list
                var existing = _items.Find(x => x.ProductId == dsProd.ProductId);
                if (existing != null)
                {
                    existing.Quantity += 1;
                }
                else
                {
                    _items.Add(new ImportItemModel 
                    { 
                        ProductId = dsProd.ProductId, 
                        Barcode = code, 
                        Name = dsProd.ProductName, 
                        Quantity = 1, 
                        CostPrice = 10000, // Default cost
                        ExpiryDate = DateTime.Now.AddMonths(6)
                    });
                }
                RefreshGrid();
                AntdUI.Message.success(this, $"Đã thêm {dsProd.ProductName} (+1)");
            }
            else
            {
                AntdUI.Message.error(this, $"Không tìm thấy sản phẩm mã {code}");
            }
            
            txtBarcode.Clear();
            txtBarcode.Focus();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void RefreshGrid()
    {
        dgvItems.Rows.Clear();
        foreach (var it in _items)
        {
            dgvItems.Rows.Add(it.Barcode, it.Name, it.Quantity, it.CostPrice, it.ExpiryDate.ToString("dd/MM/yyyy"));
        }
        lblTotalItems.Text = $"Tổng mặt hàng: {_items.Count}";
    }

    private async void BtnCreate_Click(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Vui lòng quét ít nhất 1 sản phẩm để nhập hàng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int supplierId = (cbSupplier.SelectedItem as SupplierItem)?.Id ?? 1;

        if (_httpClient == null)
        {
            MessageBox.Show("Chế độ thiết kế. Phiếu đã được tạo ảo.", "Thành công");
            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        }

        try
        {
            btnCreate.Enabled = false;
            btnCreate.Text = "Đang lưu...";

            // 1. Tạo Phiếu Nhập
            var payload = new { supplierId = supplierId, branchId = 1, note = txtNote.Text };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync($"{_apiBaseUrl}/api/import-receipts", content);
            
            if (!resp.IsSuccessStatusCode)
            {
                MessageBox.Show("Có lỗi khi tạo header phiếu nhập.", "Lỗi");
                return;
            }

            var respStr = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(respStr);
            int receiptId = doc.RootElement.GetProperty("data").GetProperty("importReceiptId").GetInt32();

            // 2. Thêm từng Detail
            foreach(var item in _items)
            {
                var detPayload = new 
                { 
                    productId = item.ProductId, 
                    quantity = item.Quantity, 
                    costPrice = item.CostPrice, 
                    expiryDate = item.ExpiryDate 
                };
                var detContent = new StringContent(JsonSerializer.Serialize(detPayload), Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{_apiBaseUrl}/api/import-receipts/{receiptId}/details", detContent);
            }

            MessageBox.Show($"Đã tạo Phiếu Nhập kho thành công (ID: {receiptId}) với {_items.Count} mặt hàng!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi");
        }
        finally
        {
            btnCreate.Enabled = true;
            btnCreate.Text = "✅ Lưu Phiếu Nhập";
        }
    }

    private class SupplierItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ImportItemModel
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}