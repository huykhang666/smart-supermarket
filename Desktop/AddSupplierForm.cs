using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop;

public class AddSupplierForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    private TextBox txtSupplierName = null!;
    private TextBox txtContactPerson = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtAddress = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public string CreatedSupplierName { get; private set; } = "";
    public (int id, string code, string name, string contact, string phone, string email) CreatedSupplierData { get; private set; }

    public AddSupplierForm(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;

        InitializeComponentLayout();
    }

    private void InitializeComponentLayout()
    {
        Text = "➕ Thêm Nhà Cung Cấp Mới";
        Size = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.BackgroundGray;

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = AppTheme.SurfaceWhite
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "🏢 Khai Báo Thông Tin Nhà Cung Cấp Mới",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(20, 16)
        };
        pnlHeader.Controls.Add(lblTitle);
        Controls.Add(pnlHeader);

        // Content
        var pnlContent = new Panel
        {
            Location = new Point(20, 70),
            Size = new Size(464, 350),
            BackColor = AppTheme.SurfaceWhite
        };
        AppTheme.ApplyCardPanel(pnlContent);

        int curY = 20;

        // 1. Supplier Name
        var lblName = new Label
        {
            Text = "Tên Nhà Cung Cấp *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, curY + 4),
            AutoSize = true
        };
        txtSupplierName = new TextBox
        {
            Location = new Point(170, curY),
            Size = new Size(270, 29),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Ví dụ: Công ty TNHH Nước Giải Khát..."
        };
        pnlContent.Controls.AddRange(new Control[] { lblName, txtSupplierName });
        curY += 45;

        // 2. Contact Person
        var lblContact = new Label
        {
            Text = "Người liên hệ:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, curY + 4),
            AutoSize = true
        };
        txtContactPerson = new TextBox
        {
            Location = new Point(170, curY),
            Size = new Size(270, 29),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Tên đại diện kinh doanh / giao dịch"
        };
        pnlContent.Controls.AddRange(new Control[] { lblContact, txtContactPerson });
        curY += 45;

        // 3. Phone Number
        var lblPhone = new Label
        {
            Text = "Số điện thoại *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, curY + 4),
            AutoSize = true
        };
        txtPhone = new TextBox
        {
            Location = new Point(170, curY),
            Size = new Size(270, 29),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "0987xxx..."
        };
        pnlContent.Controls.AddRange(new Control[] { lblPhone, txtPhone });
        curY += 45;

        // 4. Email
        var lblEmail = new Label
        {
            Text = "Email liên hệ:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, curY + 4),
            AutoSize = true
        };
        txtEmail = new TextBox
        {
            Location = new Point(170, curY),
            Size = new Size(270, 29),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "contact@nhacungcap.com"
        };
        pnlContent.Controls.AddRange(new Control[] { lblEmail, txtEmail });
        curY += 45;

        // 5. Address
        var lblAddress = new Label
        {
            Text = "Địa chỉ trụ sở:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, curY + 4),
            AutoSize = true
        };
        txtAddress = new TextBox
        {
            Location = new Point(170, curY),
            Size = new Size(270, 29),
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Địa chỉ kho hàng / văn phòng"
        };
        pnlContent.Controls.AddRange(new Control[] { lblAddress, txtAddress });
        curY += 55;

        // Buttons
        btnSave = new Button
        {
            Text = "💾 Lưu Nhà Cung Cấp",
            Location = new Point(160, curY),
            Size = new Size(185, 36)
        };
        AppTheme.ApplyPrimaryButton(btnSave);
        btnSave.Click += async (s, e) => await SaveSupplierAsync();

        btnCancel = new Button
        {
            Text = "✕ Hủy",
            Location = new Point(355, curY),
            Size = new Size(85, 36)
        };
        AppTheme.ApplySecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        pnlContent.Controls.AddRange(new Control[] { btnSave, btnCancel });
        Controls.Add(pnlContent);
    }

    private async Task SaveSupplierAsync()
    {
        string name = txtSupplierName.Text.Trim();
        string phone = txtPhone.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Vui lòng nhập Tên Nhà Cung Cấp!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtSupplierName.Focus();
            return;
        }

        CreatedSupplierName = name;
        int newId = new Random().Next(100, 999);
        string code = $"SUP{newId:D5}";
        string contact = string.IsNullOrWhiteSpace(txtContactPerson.Text) ? "N/A" : txtContactPerson.Text.Trim();
        string emailVal = string.IsNullOrWhiteSpace(txtEmail.Text) ? "N/A" : txtEmail.Text.Trim();

        CreatedSupplierData = (newId, code, name, contact, phone, emailVal);

        var supplierItem = new SupplierItem
        {
            SupplierId = newId,
            SupplierCode = code,
            SupplierName = name,
            ContactPerson = contact,
            Phone = phone,
            Email = emailVal,
            Address = txtAddress.Text.Trim()
        };
        DataStore.AddSupplier(supplierItem);

        try
        {
            var request = new
            {
                SupplierName = name,
                ContactPerson = txtContactPerson.Text.Trim(),
                Phone = phone,
                Email = txtEmail.Text.Trim(),
                Address = txtAddress.Text.Trim()
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/suppliers", content);
        }
        catch
        {
            // API graceful fallback
        }

        MessageBox.Show($"✅ Đã thêm mới Nhà Cung Cấp '{name}' thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
    }
}
