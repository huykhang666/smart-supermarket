using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop.Views;

public class CreateImportReceiptForm : Form
{
    private readonly HttpClient? _httpClient;
    private readonly string _apiBaseUrl = string.Empty;

    private ComboBox cbSupplier = null!;
    private TextBox txtNote = null!;
    private Button btnCreate = null!;
    private Button btnCancel = null!;

    // 1. Constructor mặc định bắt buộc để mở Designer
    public CreateImportReceiptForm()
    {
        InitializeComponent();
    }

    // 2. Constructor nhận tham số khi chạy thực tế
    public CreateImportReceiptForm(HttpClient httpClient, string apiBaseUrl) : this()
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;

        // Dữ liệu mẫu khởi tạo ở constructor thay vì nhét vào Designer
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
        if (cbSupplier.Items.Count > 0)
        {
            cbSupplier.SelectedIndex = 0;
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Tạo Phiếu Nhập Hàng Mới";
        this.Size = new Size(420, 310);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        Label lblSupplier = new Label { Text = "Nhà cung cấp:", Font = AppTheme.FontBodyBold, Location = new Point(24, 20), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        cbSupplier = new ComboBox
        {
            Location = new Point(24, 46),
            Size = new Size(355, 32),
            Font = AppTheme.FontBody,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblNote = new Label { Text = "Ghi chú:", Font = AppTheme.FontBodyBold, Location = new Point(24, 90), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        txtNote = new TextBox
        {
            Location = new Point(24, 114),
            Size = new Size(355, 65),
            Font = AppTheme.FontBody,
            Multiline = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnCreate = new Button
        {
            Text = "Tạo Phiếu Nháp",
            Location = new Point(165, 200),
            Size = new Size(130, 32)
        };
        AppTheme.ApplyPrimaryButton(btnCreate);
        btnCreate.Click += BtnCreate_Click;

        btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(305, 200),
            Size = new Size(74, 32)
        };
        AppTheme.ApplySecondaryButton(btnCancel);
        btnCancel.Click += BtnCancel_Click;

        this.Controls.Add(lblSupplier);
        this.Controls.Add(cbSupplier);
        this.Controls.Add(lblNote);
        this.Controls.Add(txtNote);
        this.Controls.Add(btnCreate);
        this.Controls.Add(btnCancel);
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private async void BtnCreate_Click(object? sender, EventArgs e)
    {
        int supplierId = 1;
        if (cbSupplier.SelectedItem is SupplierItem item)
        {
            supplierId = item.Id;
        }

        var payload = new
        {
            supplierId = supplierId,
            branchId = 1,
            note = txtNote.Text
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        if (_httpClient == null)
        {
            MessageBox.Show("Đang ở chế độ xem thiết kế (Designer).", "Thông báo");
            return;
        }

        try
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/import-receipts", content);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Đã tạo phiếu nháp thành công! (Chưa cộng kho)", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Có lỗi khi tạo phiếu.", "Lỗi");
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Giả lập: Đã tạo phiếu nháp thành công! (Do API offline)", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // Class phụ trợ để chứa dữ liệu Combobox
    private class SupplierItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}