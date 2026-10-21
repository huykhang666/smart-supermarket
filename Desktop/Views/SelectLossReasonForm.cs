using System;
using System.Drawing;
using System.Windows.Forms;
using SmartSupermarket.Backend.Domain.Enums;

namespace Desktop.Views;

/// <summary>
/// Hộp thoại chọn Nguyên nhân hao hụt (Shrinkage / Loss Reason)
/// Thiết kế đơn giản, tương phản cao, chống trùng màu chữ với nền, hiệu ứng click tactile.
/// </summary>
public class SelectLossReasonForm : Form
{
    private NumericUpDown numActualQty = null!;
    private ComboBox cboReason = null!;
    private TextBox txtNotes = null!;
    private Button btnConfirm = null!;
    private Button btnCancel = null!;

    public int ActualQty => (int)numActualQty.Value;
    public string SelectedReason { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    public SelectLossReasonForm(string barcode, string productName, int systemQty, int actualQty, int varianceQty, string currentReason, string currentNotes)
    {
        InitializeComponent(barcode, productName, systemQty, actualQty, varianceQty, currentReason, currentNotes);
    }

    private void InitializeComponent(string barcode, string productName, int systemQty, int actualQty, int varianceQty, string currentReason, string currentNotes)
    {
        this.Text = "Phân Loại Nguyên Nhân Hao Hụt (Shrinkage Reason)";
        this.Size = new Size(520, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AppTheme.SurfaceWhite;
        this.Font = AppTheme.FontBody;

        // --- 1. Info Card (Tên sản phẩm & Số chênh lệch) ---
        var pnlInfo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.FromArgb(0xFA, 0xFA, 0xFA),
            Padding = new Padding(20, 14, 20, 14)
        };
        pnlInfo.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, pnlInfo.Height - 1, pnlInfo.Width, pnlInfo.Height - 1);
        };

        var lblName = new Label
        {
            Text = $"📦 {productName}",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(18, 14),
            AutoSize = true,
            MaximumSize = new Size(460, 26),
            AutoEllipsis = true
        };

        var lblBarcode = new Label
        {
            Text = $"Mã Barcode: {barcode}",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(20, 42),
            AutoSize = true
        };

        string diffText = varianceQty < 0 ? $"❌ Thiếu: {Math.Abs(varianceQty)} sản phẩm" : (varianceQty > 0 ? $"⚠️ Thừa: +{varianceQty} sản phẩm" : "✅ Khớp tồn");
        Color diffColor = varianceQty < 0 ? AppTheme.Danger : (varianceQty > 0 ? AppTheme.Warning : AppTheme.Success);

        var lblDiff = new Label
        {
            Text = $"Sổ sách: {systemQty}  |  Thực tế: {actualQty}  ➔  {diffText}",
            Font = AppTheme.FontBodyBold,
            ForeColor = diffColor,
            Location = new Point(20, 70),
            AutoSize = true
        };

        pnlInfo.Controls.Add(lblName);
        pnlInfo.Controls.Add(lblBarcode);
        pnlInfo.Controls.Add(lblDiff);

        // --- 2. Input Container ---
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 14, 24, 14),
            BackColor = AppTheme.SurfaceWhite
        };

        var lblQtyTitle = new Label
        {
            Text = "Số lượng thực tế kiểm đếm được (*):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 8),
            AutoSize = true
        };

        numActualQty = new NumericUpDown
        {
            Location = new Point(20, 32),
            Size = new Size(130, 32),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Minimum = 0,
            Maximum = 99999,
            Value = Math.Max(0, actualQty),
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };
        numActualQty.ValueChanged += (s, e) =>
        {
            int newAct = (int)numActualQty.Value;
            int newVar = newAct - systemQty;
            string newDiffText = newVar < 0 ? $"❌ Thiếu: {Math.Abs(newVar)} sản phẩm" : (newVar > 0 ? $"⚠️ Thừa: +{newVar} sản phẩm" : "✅ Khớp tồn");
            Color newDiffColor = newVar < 0 ? AppTheme.Danger : (newVar > 0 ? AppTheme.Warning : AppTheme.Success);
            lblDiff.Text = $"Sổ sách: {systemQty}  |  Thực tế: {newAct}  ➔  {newDiffText}";
            lblDiff.ForeColor = newDiffColor;
        };

        var lblReasonTitle = new Label
        {
            Text = "Nguyên nhân hao hụt / chênh lệch (*):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 72),
            AutoSize = true
        };

        cboReason = new ComboBox
        {
            Location = new Point(20, 96),
            Size = new Size(460, 34),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };

        // 4 nhóm lý do chuẩn thực tế siêu thị + thừa hàng + khác
        cboReason.Items.AddRange(new object[]
        {
            "Hàng dập nát (Hư hỏng bảo quản/vận chuyển)",
            "Hết date tiêu hủy (Hết hạn sử dụng)",
            "Vỡ hỏng trong siêu thị (Rơi vỡ tại quầy/kho)",
            "Thất thoát chưa rõ nguyên nhân (Nghi vấn mất trộm)",
            "Thừa hàng (Nhập sót phiếu / Nhầm mã)",
            "Lý do khác"
        });

        // Set giá trị khởi tạo
        if (!string.IsNullOrWhiteSpace(currentReason))
        {
            int foundIdx = -1;
            for (int i = 0; i < cboReason.Items.Count; i++)
            {
                string? itemText = cboReason.Items[i]?.ToString();
                if (itemText != null && itemText.StartsWith(currentReason, StringComparison.OrdinalIgnoreCase))
                {
                    foundIdx = i;
                    break;
                }
            }
            cboReason.SelectedIndex = foundIdx >= 0 ? foundIdx : 0;
        }
        else
        {
            cboReason.SelectedIndex = varianceQty < 0 ? 0 : (varianceQty > 0 ? 4 : 5);
        }

        var lblNotesTitle = new Label
        {
            Text = "Ghi chú chi tiết:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 138),
            AutoSize = true
        };

        txtNotes = new TextBox
        {
            Location = new Point(20, 162),
            Size = new Size(460, 65),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = AppTheme.FontBody,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary,
            Text = currentNotes,
            PlaceholderText = "Nhập vị trí cụ thể, tình trạng bao bì, người phát hiện..."
        };

        pnlBody.Controls.Add(lblQtyTitle);
        pnlBody.Controls.Add(numActualQty);
        pnlBody.Controls.Add(lblReasonTitle);
        pnlBody.Controls.Add(cboReason);
        pnlBody.Controls.Add(lblNotesTitle);
        pnlBody.Controls.Add(txtNotes);

        // --- 3. Bottom Actions ---
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = Color.FromArgb(0xF8, 0xF9, 0xFA),
            Padding = new Padding(20, 14, 20, 14)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        btnConfirm = CreateTactileButton("✅ Lưu Phân Loại", AppTheme.Primary, AppTheme.PrimaryHover, AppTheme.PrimaryPressed, Color.White, 150, 36);
        btnConfirm.Location = new Point(pnlBottom.Width - 170, 14);
        btnConfirm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnConfirm.Click += (s, e) =>
        {
            string full = cboReason.SelectedItem?.ToString() ?? "";
            // Lấy tên ngắn gọn
            SelectedReason = full.Contains(" (") ? full.Substring(0, full.IndexOf(" (")) : full;
            Notes = txtNotes.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        btnCancel = CreateTactileButton("Đóng", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 100, 36, true);
        btnCancel.Location = new Point(pnlBottom.Width - 280, 14);
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };

        pnlBottom.Controls.Add(btnCancel);
        pnlBottom.Controls.Add(btnConfirm);

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlInfo);
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

        // Tactile click feedback: nhún nhẹ 1px khi nhấn chuột
        btn.MouseDown += (s, e) => btn.Padding = new Padding(1, 2, 0, 0);
        btn.MouseUp += (s, e) => btn.Padding = new Padding(0);

        return btn;
    }
}
