using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Forms;

public class ManagerApprovalDialog : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _actionTitle;
    private readonly string _actionDetails;

    private TextBox txtManagerPin = null!;
    private ComboBox cbCancelReason = null!;
    private TextBox txtNote = null!;
    private Button btnConfirm = null!;
    private Button btnCancel = null!;

    public string SelectedReason { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public string ManagerPin { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; } = false;

    public ManagerApprovalDialog(string actionTitle, string actionDetails, HttpClient? httpClient = null, string? apiBaseUrl = null)
    {
        _actionTitle = actionTitle;
        _actionDetails = actionDetails;
        _httpClient = httpClient ?? new HttpClient();
        _apiBaseUrl = apiBaseUrl ?? AppTheme.ApiBaseUrl;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Yêu Cầu Phê Duyệt Quản Lý";
        this.Size = new Size(520, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        // --- 1. Header Card ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.FromArgb(254, 242, 242), // Light red/danger alert tint
            Padding = new Padding(20, 15, 20, 15)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(Color.FromArgb(254, 202, 202), 1);
            e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var picIcon = new IconPictureBox
        {
            IconChar = IconChar.ShieldHalved,
            IconColor = Color.FromArgb(220, 38, 38),
            IconSize = 36,
            Size = new Size(36, 36),
            Location = new Point(18, 24),
            BackColor = Color.Transparent
        };

        var lblHeaderTitle = new Label
        {
            Text = "🔒 " + _actionTitle,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 28, 28),
            Location = new Point(62, 18),
            AutoSize = true
        };

        var lblHeaderDesc = new Label
        {
            Text = _actionDetails,
            Font = AppTheme.FontCaption,
            ForeColor = Color.FromArgb(127, 29, 29),
            Location = new Point(64, 46),
            Size = new Size(420, 32)
        };

        pnlHeader.Controls.Add(picIcon);
        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(lblHeaderDesc);

        // --- 2. Body Inputs ---
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 16),
            BackColor = Color.Transparent
        };

        int currentY = 100;
        int gapY = 65;

        // Row 1: Manager PIN
        var lblPin = new Label
        {
            Text = "Mã PIN Quản Lý (Hoặc Quản Trị Viên): *",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, currentY),
            AutoSize = true
        };
        txtManagerPin = new TextBox
        {
            Location = new Point(24, currentY + 24),
            Size = new Size(456, 34),
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            PasswordChar = '●',
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập mã PIN Quản lý (Mặc định: 123456)"
        };
        txtManagerPin.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnConfirm_Click(s, e); };

        currentY += gapY;

        // Row 2: Lý do hủy
        var lblReason = new Label
        {
            Text = "Lý do hủy / thao tác: *",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, currentY),
            AutoSize = true
        };
        cbCancelReason = new ComboBox
        {
            Location = new Point(24, currentY + 24),
            Size = new Size(456, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontBody
        };
        cbCancelReason.Items.AddRange(new object[]
        {
            "Khách hàng đổi ý, không mua món này nữa",
            "Thu ngân quét nhầm / trùng mã Barcode",
            "Bao bì sản phẩm bị rách, xước hoặc hư hỏng",
            "Khách không đủ tiền thanh toán",
            "Món hàng hết date / cận date không đạt chuẩn",
            "Nhập sai số lượng sản phẩm",
            "Lý do khác (ghi rõ ở phần ghi chú bên dưới)"
        });
        cbCancelReason.SelectedIndex = 0;

        currentY += gapY;

        // Row 3: Ghi chú giải trình thêm
        var lblNote = new Label
        {
            Text = "Ghi chú giải trình bổ sung:",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, currentY),
            AutoSize = true
        };
        txtNote = new TextBox
        {
            Location = new Point(24, currentY + 22),
            Size = new Size(456, 52),
            Multiline = true,
            Font = AppTheme.FontBody,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập thêm thông tin giải trình nếu cần..."
        };

        currentY += 86;

        // Row 4: Buttons
        var pnlButtons = new FlowLayoutPanel
        {
            Location = new Point(24, currentY),
            Size = new Size(456, 42),
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };

        btnConfirm = new Button
        {
            Text = "✓ Duyệt Phê Chuẩn",
            Size = new Size(160, 38),
            Font = AppTheme.FontBodyBold,
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnConfirm, radius: 6);
        btnConfirm.Click += BtnConfirm_Click;

        btnCancel = new Button
        {
            Text = "✕ Hủy Bỏ",
            Size = new Size(110, 38),
            Font = AppTheme.FontBodyBold,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        AppTheme.ApplySecondaryButton(btnCancel, radius: 6);
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        pnlButtons.Controls.Add(btnConfirm);
        pnlButtons.Controls.Add(btnCancel);

        this.Controls.Add(pnlButtons);
        this.Controls.Add(txtNote);
        this.Controls.Add(lblNote);
        this.Controls.Add(cbCancelReason);
        this.Controls.Add(lblReason);
        this.Controls.Add(txtManagerPin);
        this.Controls.Add(lblPin);
        this.Controls.Add(pnlHeader);
    }

    private async void BtnConfirm_Click(object? sender, EventArgs e)
    {
        string pin = txtManagerPin.Text.Trim();
        if (string.IsNullOrEmpty(pin))
        {
            MessageBox.Show("Vui lòng nhập Mã PIN Quản lý để phê duyệt!", "Chưa nhập mã PIN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtManagerPin.Focus();
            return;
        }

        btnConfirm.Enabled = false;
        btnConfirm.Text = "Đang xác thực...";

        try
        {
            // Kiểm tra mã PIN qua API backend
            bool isValid = false;
            try
            {
                var payload = new { pin = pin };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/shifts/sessions/verify-manager-pin", content);
                if (res.IsSuccessStatusCode)
                {
                    isValid = true;
                }
            }
            catch
            {
                // Fallback nếu API offline: chấp nhận mã PIN chuẩn 123456 hoặc 888888
                if (pin == "123456" || pin == "888888")
                {
                    isValid = true;
                }
            }

            if (isValid)
            {
                SelectedReason = cbCancelReason.SelectedItem?.ToString() ?? "Khách hàng đổi ý";
                Note = txtNote.Text.Trim();
                ManagerPin = pin;
                IsApproved = true;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Mã PIN Quản lý không đúng hoặc không có thẩm quyền duyệt!\n(Gợi ý: Mặc định là 123456)", "Từ chối phê duyệt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtManagerPin.SelectAll();
                txtManagerPin.Focus();
            }
        }
        finally
        {
            btnConfirm.Enabled = true;
            btnConfirm.Text = "✓ Duyệt Phê Chuẩn";
        }
    }
}
