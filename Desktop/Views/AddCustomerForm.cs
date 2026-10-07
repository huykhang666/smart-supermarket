using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop.Views;

public class AddCustomerForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    private TextBox txtFullName = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtAddress = null!;
    private ComboBox cbGender = null!;

    private Button btnSave = null!;
    private Button btnCancel = null!;

    public AddCustomerForm(HttpClient? httpClient = null, string? apiBaseUrl = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiBaseUrl = apiBaseUrl ?? AppTheme.ApiBaseUrl;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Thêm Khách Hàng Thành Viên Mới";
        this.Size = new Size(500, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        var lblTitle = new Label
        {
            Text = "Đăng Ký Khách Hàng Thành Viên",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(30, 20),
            AutoSize = true
        };
        this.Controls.Add(lblTitle);

        int startY = 65;
        int gapY = 56;
        int currentY = startY;

        // Họ tên
        CreateField("Họ và Tên (*):", currentY, out txtFullName);
        currentY += gapY;

        // Số điện thoại
        CreateField("Số Điện Thoại (*):", currentY, out txtPhone);
        currentY += gapY;

        // Email
        CreateField("Địa Chỉ Email:", currentY, out txtEmail);
        currentY += gapY;

        // Địa chỉ
        CreateField("Địa Chỉ:", currentY, out txtAddress);
        currentY += gapY;

        // Giới tính
        var lblGender = new Label
        {
            Text = "Giới Tính:",
            Location = new Point(30, currentY + 4),
            AutoSize = true,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextSecondary
        };
        cbGender = new ComboBox
        {
            Location = new Point(170, currentY),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbGender.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
        cbGender.SelectedIndex = 0;
        this.Controls.Add(lblGender);
        this.Controls.Add(cbGender);
        currentY += gapY + 10;

        // Buttons
        btnSave = new Button
        {
            Text = "💾 Lưu Khách Hàng",
            Location = new Point(170, currentY),
            Size = new Size(160, 36)
        };
        AppTheme.ApplyPrimaryButton(btnSave);
        btnSave.Click += async (s, e) => await SaveCustomerAsync();

        btnCancel = new Button
        {
            Text = "Đóng",
            Location = new Point(345, currentY),
            Size = new Size(105, 36)
        };
        AppTheme.ApplySecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => this.Close();

        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
    }

    private void CreateField(string labelText, int y, out TextBox textBox)
    {
        var lbl = new Label
        {
            Text = labelText,
            Location = new Point(30, y + 4),
            AutoSize = true,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextSecondary
        };
        textBox = new TextBox
        {
            Location = new Point(170, y),
            Width = 280,
            Font = AppTheme.FontBody,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(lbl);
        this.Controls.Add(textBox);
    }

    private async Task SaveCustomerAsync()
    {
        string fullName = txtFullName.Text.Trim();
        string phone = txtPhone.Text.Trim();
        string email = txtEmail.Text.Trim();
        string address = txtAddress.Text.Trim();
        byte gender = (byte)(cbGender.SelectedIndex + 1);

        if (string.IsNullOrEmpty(fullName))
        {
            MessageBox.Show("Vui lòng nhập họ và tên khách hàng.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtFullName.Focus();
            return;
        }

        if (string.IsNullOrEmpty(phone))
        {
            MessageBox.Show("Vui lòng nhập số điện thoại khách hàng.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPhone.Focus();
            return;
        }

        btnSave.Enabled = false;
        btnSave.Text = "Đang lưu...";

        try
        {
            var payload = new
            {
                fullName = fullName,
                phone = phone,
                email = string.IsNullOrEmpty(email) ? null : email,
                address = string.IsNullOrEmpty(address) ? null : address,
                gender = gender
            };

            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/customers", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Đăng ký khách hàng '{fullName}' thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                string errorMsg = "Không thể tạo khách hàng.";
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        errorMsg = msg.GetString() ?? errorMsg;
                    }
                }
                catch { }

                MessageBox.Show($"Thất bại: {errorMsg}", "Lỗi tạo khách hàng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối máy chủ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSave.Enabled = true;
            btnSave.Text = "💾 Lưu Khách Hàng";
        }
    }
}
