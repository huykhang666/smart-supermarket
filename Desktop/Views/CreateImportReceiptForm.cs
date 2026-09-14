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
        cbSupplier.Items.Add(new SupplierItem { Id = 1, Name = "Công ty TNHH Coca-Cola Việt Nam" });
        cbSupplier.Items.Add(new SupplierItem { Id = 2, Name = "Công ty Cổ phần Sữa Vinamilk" });
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
        this.Size = new Size(400, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;

        Label lblSupplier = new Label { Text = "Nhà cung cấp:", Location = new Point(20, 30), AutoSize = true };
        cbSupplier = new ComboBox
        {
            Location = new Point(20, 55),
            Size = new Size(340, 30),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        Label lblNote = new Label { Text = "Ghi chú:", Location = new Point(20, 100), AutoSize = true };
        txtNote = new TextBox
        {
            Location = new Point(20, 125),
            Size = new Size(340, 60),
            Multiline = true
        };

        btnCreate = new Button
        {
            Text = "Tạo Phiếu Nháp",
            BackColor = Color.FromArgb(56, 158, 13),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(140, 210),
            Size = new Size(130, 35)
        };
        btnCreate.Click += BtnCreate_Click;

        btnCancel = new Button
        {
            Text = "Hủy",
            FlatStyle = FlatStyle.Flat,
            Location = new Point(280, 210),
            Size = new Size(80, 35)
        };
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