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
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    private Label lblProductName = null!;
    private Label lblCurrentQty = null!;
    private NumericUpDown numQuantityChange = null!;
    private TextBox txtNote = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public InventoryAdjustForm(int productId, string productName, int currentQty, HttpClient httpClient, string apiBaseUrl)
    {
        _productId = productId;
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;

        InitializeComponent(productName, currentQty);
    }

    private void InitializeComponent(string productName, int currentQty)
    {
        this.Text = "Điều chỉnh Tồn kho";
        this.Size = new Size(400, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;

        lblProductName = new Label
        {
            Text = $"Sản phẩm: {productName}",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        lblCurrentQty = new Label
        {
            Text = $"Tồn kho hiện tại: {currentQty}",
            Font = new Font("Segoe UI", 10f),
            Location = new Point(20, 50),
            AutoSize = true
        };

        Label lblChange = new Label { Text = "Số lượng thay đổi (Âm/Dương):", Location = new Point(20, 90), AutoSize = true };
        numQuantityChange = new NumericUpDown
        {
            Location = new Point(20, 115),
            Size = new Size(150, 30),
            Minimum = -10000,
            Maximum = 10000,
            Value = 0
        };

        Label lblNote = new Label { Text = "Lý do điều chỉnh (Bắt buộc):", Location = new Point(20, 160), AutoSize = true };
        txtNote = new TextBox
        {
            Location = new Point(20, 185),
            Size = new Size(340, 60),
            Multiline = true
        };

        btnSave = new Button
        {
            Text = "Lưu Điều Chỉnh",
            BackColor = Color.FromArgb(9, 109, 217),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(140, 260),
            Size = new Size(110, 35)
        };
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "Hủy",
            FlatStyle = FlatStyle.Flat,
            Location = new Point(260, 260),
            Size = new Size(100, 35)
        };
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        this.Controls.Add(lblProductName);
        this.Controls.Add(lblCurrentQty);
        this.Controls.Add(lblChange);
        this.Controls.Add(numQuantityChange);
        this.Controls.Add(lblNote);
        this.Controls.Add(txtNote);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
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
            // For Demo, just close with OK
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
