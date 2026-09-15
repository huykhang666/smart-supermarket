using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop.Views;

public class InventoryAdjustForm : Form
{
    private readonly int _productId;
    private readonly HttpClient? _httpClient;
    private readonly string _apiBaseUrl = string.Empty;

    private Label lblProductName = null!;
    private Label lblCurrentQty = null!;
    private Label lblChange = null!;
    private NumericUpDown numQuantityChange = null!;
    private Label lblNote = null!;
    private TextBox txtNote = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public InventoryAdjustForm()
    {
        InitializeComponent();
    }

    public InventoryAdjustForm(int productId, string productName, int currentQty, HttpClient httpClient, string apiBaseUrl) : this()
    {
        _productId = productId;
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;

        lblProductName.Text = $"Sản phẩm: {productName}";
        lblCurrentQty.Text = $"Tồn kho hiện tại: {currentQty}";
    }

    private void InitializeComponent()
    {
        this.Text = "Điều chỉnh Tồn kho";
        this.Size = new Size(420, 360);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        lblProductName = new Label
        {
            Text = "Sản phẩm: --",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, 18),
            AutoSize = true
        };

        lblCurrentQty = new Label
        {
            Text = "Tồn kho hiện tại: --",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, 46),
            AutoSize = true
        };

        lblChange = new Label
        {
            Text = "Số lượng thay đổi (Âm/Dương):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, 80),
            AutoSize = true
        };

        numQuantityChange = new NumericUpDown
        {
            Location = new Point(24, 104),
            Size = new Size(160, 32),
            Font = AppTheme.FontBody,
            Minimum = -10000,
            Maximum = 10000,
            Value = 0
        };

        lblNote = new Label
        {
            Text = "Lý do điều chỉnh (Bắt buộc):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, 150),
            AutoSize = true
        };

        txtNote = new TextBox
        {
            Location = new Point(24, 174),
            Size = new Size(355, 65),
            Font = AppTheme.FontBody,
            Multiline = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnSave = new Button
        {
            Text = "Lưu Điều Chỉnh",
            Location = new Point(165, 260),
            Size = new Size(130, 32)
        };
        AppTheme.ApplyPrimaryButton(btnSave);
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(305, 260),
            Size = new Size(74, 32)
        };
        AppTheme.ApplySecondaryButton(btnCancel);
        btnCancel.Click += BtnCancel_Click;

        this.Controls.Add(lblProductName);
        this.Controls.Add(lblCurrentQty);
        this.Controls.Add(lblChange);
        this.Controls.Add(numQuantityChange);
        this.Controls.Add(lblNote);
        this.Controls.Add(txtNote);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (numQuantityChange.Value == 0)
        {
            MessageBox.Show("Số lượng điều chỉnh phải khác 0.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtNote.Text))
        {
            MessageBox.Show("Vui lòng nhập lý do điều chỉnh.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_httpClient == null)
        {
            MessageBox.Show("Chế độ Design hoặc HttpClient chưa được khởi tạo.", "Thông báo");
            return;
        }

        var payload = new
        {
            quantityChange = (int)numQuantityChange.Value,
            note = txtNote.Text
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/api/inventory/{_productId}/adjust?branchId=1", content);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Điều chỉnh kho thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Lỗi khi điều chỉnh tồn kho.", "Lỗi");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể kết nối API: {ex.Message}", "Lỗi");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}