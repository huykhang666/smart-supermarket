using System;
using System.Drawing;
using System.Windows.Forms;

namespace Desktop.Views;

/// <summary>
/// Hộp thoại Khởi Tạo Phiếu Kiểm Kê Mới
/// Cho phép chọn khu vực kệ, nhân viên phụ trách và ghi chú đợt kiểm kê.
/// </summary>
public class CreateStockAuditForm : Form
{
    private TextBox txtCreatedBy = null!;
    private ComboBox cboZone = null!;
    private TextBox txtNotes = null!;
    private Button btnCreate = null!;
    private Button btnCancel = null!;

    public string CreatedBy => txtCreatedBy.Text.Trim();
    public string Zone => cboZone.SelectedItem?.ToString() ?? "Tất cả kệ";
    public string Notes => txtNotes.Text.Trim();

    public CreateStockAuditForm(string defaultCreator)
    {
        InitializeComponent(defaultCreator);
    }

    private void InitializeComponent(string defaultCreator)
    {
        this.Text = "Khởi Tạo Đợt Kiểm Kê Kho Mới";
        this.Size = new Size(480, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        // Header Title
        var lblHeader = new Label
        {
            Text = "📋 Thiết Lập Đợt Kiểm Kê Mới",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(24, 18),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Chọn khu vực kệ hàng và phân công nhân viên kiểm đếm",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(26, 46),
            AutoSize = true
        };

        // Form Fields
        var lblCreatedBy = new Label { Text = "Nhân viên kiểm kê (*):", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(24, 82), AutoSize = true };
        txtCreatedBy = new TextBox
        {
            Location = new Point(24, 106),
            Size = new Size(415, 32),
            Font = AppTheme.FontBody,
            Text = string.IsNullOrWhiteSpace(defaultCreator) ? CurrentSession.FullName : defaultCreator,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };

        var lblZone = new Label { Text = "Khu vực / Kệ hàng kiểm (*):", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(24, 146), AutoSize = true };
        cboZone = new ComboBox
        {
            Location = new Point(24, 170),
            Size = new Size(415, 32),
            Font = AppTheme.FontBody,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };
        cboZone.Items.AddRange(new object[]
        {
            "Tất cả kệ hàng",
            "Kệ A1 - Nước giải khát & Đồ uống",
            "Kệ B2 - Bánh kẹo & Ăn vặt",
            "Kệ C1 - Sữa & Sản phẩm từ sữa",
            "Kệ D3 - Gia vị & Đồ khô",
            "Kệ E1 - Hóa phẩm & Giặt xả",
            "Kho lưu trữ nội bộ (Kho phụ)"
        });
        cboZone.SelectedIndex = 0;

        var lblNotes = new Label { Text = "Mục đích / Ghi chú đợt kiểm:", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(24, 212), AutoSize = true };
        txtNotes = new TextBox
        {
            Location = new Point(24, 236),
            Size = new Size(415, 60),
            Multiline = true,
            Font = AppTheme.FontBody,
            PlaceholderText = "Ví dụ: Kiểm kê định kỳ tuần 3, đối soát quầy nước...",
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };

        // Buttons
        btnCreate = CreateTactileButton("🚀 Bắt Đầu Kiểm Kê", AppTheme.Primary, AppTheme.PrimaryHover, AppTheme.PrimaryPressed, Color.White, 160, 36);
        btnCreate.Location = new Point(279, 310);
        btnCreate.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtCreatedBy.Text))
            {
                MessageBox.Show("Vui lòng nhập tên nhân viên kiểm kê!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCreatedBy.Focus();
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        btnCancel = CreateTactileButton("Hủy", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 90, 36, true);
        btnCancel.Location = new Point(175, 310);
        btnCancel.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };

        this.Controls.Add(lblHeader);
        this.Controls.Add(lblSub);
        this.Controls.Add(lblCreatedBy);
        this.Controls.Add(txtCreatedBy);
        this.Controls.Add(lblZone);
        this.Controls.Add(cboZone);
        this.Controls.Add(lblNotes);
        this.Controls.Add(txtNotes);
        this.Controls.Add(btnCancel);
        this.Controls.Add(btnCreate);
    }

    private Button CreateTactileButton(string text, Color backColor, Color hoverColor, Color pressedColor, Color textColor, int width, int height, bool isOutline = false)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            Font = AppTheme.FontBodyBold,
            ForeColor = textColor,
            BackColor = backColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = isOutline ? 1 : 0;
        btn.FlatAppearance.BorderColor = AppTheme.BorderLight;
        btn.FlatAppearance.MouseOverBackColor = hoverColor;
        btn.FlatAppearance.MouseDownBackColor = pressedColor;
        btn.MouseDown += (s, e) => btn.Padding = new Padding(1, 2, 0, 0);
        btn.MouseUp += (s, e) => btn.Padding = new Padding(0);
        return btn;
    }
}
