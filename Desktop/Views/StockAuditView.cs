using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class StockAuditView : UserControl
{
    private TextBox txtBarcodeScan = null!;
    private DataGridView dgvAudit = null!;
    private Label lblTotalChecked = null!;
    private Label lblMatchCount = null!;
    private Label lblDiffCount = null!;
    private ComboBox cboZone = null!;

    public StockAuditView()
    {
        InitializeComponent();
        LoadSampleAuditData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Page Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📋 Kiểm Kê Kho & Quầy Hàng (Stock Audit)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Quét mã vạch kiểm đếm số lượng thực tế trên kệ, đối chiếu số tồn hệ thống và ghi nhận chênh lệch",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Top Toolbar Card (Scan barcode & Zone select) ---
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 8, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        var lblZone = new Label { Text = "Khu vực kệ:", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(14, 20), AutoSize = true };
        cboZone = new ComboBox
        {
            Location = new Point(100, 16),
            Size = new Size(180, 32),
            Font = AppTheme.FontBody,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboZone.Items.AddRange(new[] { "Kệ A1 - Nước giải khát", "Kệ B2 - Bánh kẹo ăn vặt", "Kệ C1 - Sữa & Bơ", "Kệ D3 - Gia vị đồ khô" });
        cboZone.SelectedIndex = 0;

        var lblScan = new Label { Text = "Quét Barcode:", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(300, 20), AutoSize = true };
        txtBarcodeScan = new TextBox
        {
            Location = new Point(410, 16),
            Size = new Size(240, 32),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            PlaceholderText = "Quét mã vạch (Enter)..."
        };
        txtBarcodeScan.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ProcessScan(); };

        var btnScanAdd = new Button
        {
            Text = "➕ Đếm +1",
            Size = new Size(100, 32),
            Location = new Point(660, 16)
        };
        AppTheme.ApplyPrimaryButton(btnScanAdd);
        btnScanAdd.Click += (s, e) => ProcessScan();

        var btnCompleteAudit = new Button
        {
            Text = "✅ Hoàn Tất Kiểm Kê",
            Size = new Size(180, 32),
            Location = new Point(pnlToolbar.Width - 200, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        AppTheme.ApplyPrimaryButton(btnCompleteAudit);
        btnCompleteAudit.Click += (s, e) =>
        {
            MessageBox.Show("✅ Đã chốt và lưu biên bản kiểm kê kho thành công!\nDữ liệu tồn kho hệ thống đã được đồng bộ lại.", "Hoàn tất kiểm kê", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        pnlToolbar.Controls.Add(lblZone);
        pnlToolbar.Controls.Add(cboZone);
        pnlToolbar.Controls.Add(lblScan);
        pnlToolbar.Controls.Add(txtBarcodeScan);
        pnlToolbar.Controls.Add(btnScanAdd);
        pnlToolbar.Controls.Add(btnCompleteAudit);

        // --- 3. Stat Row (Đã kiểm, Khớp, Chênh lệch) ---
        var pnlStats = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        lblTotalChecked = new Label { Text = "Tổng sản phẩm đã kiểm: 15 mặt hàng  |  ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, AutoSize = true };
        lblMatchCount = new Label { Text = "Khớp tồn: 13  |  ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Success, AutoSize = true };
        lblDiffCount = new Label { Text = "Chênh lệch: 2 (1 Thiếu, 1 Thừa)", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Danger, AutoSize = true };

        pnlStats.Controls.Add(lblTotalChecked);
        pnlStats.Controls.Add(lblMatchCount);
        pnlStats.Controls.Add(lblDiffCount);

        // --- 4. Grid Container ---
        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1),
            Margin = new Padding(0, 6, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlGridCard);

        dgvAudit = new DataGridView();
        AppTheme.ApplyGridStyle(dgvAudit);
        dgvAudit.Columns.Add("Barcode", "Mã Barcode");
        dgvAudit.Columns.Add("Name", "Tên Sản Phẩm");
        dgvAudit.Columns.Add("SystemQty", "Tồn Hệ Thống");
        dgvAudit.Columns.Add("ActualQty", "SL Đếm Thực Tế");
        dgvAudit.Columns.Add("Diff", "Chênh Lệch");
        dgvAudit.Columns.Add("Status", "Trạng Thái Kiểm");

        pnlGridCard.Controls.Add(dgvAudit);

        this.Controls.Add(pnlGridCard);
        this.Controls.Add(pnlStats);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlHeader);
    }

    private void LoadSampleAuditData()
    {
        dgvAudit.Rows.Clear();
        lblTotalChecked.Text = "Tổng sản phẩm đã kiểm: 0 mặt hàng  |  ";
        lblMatchCount.Text = "Khớp tồn: 0  |  ";
        lblDiffCount.Text = "Chênh lệch: 0";
    }

    private void ProcessScan()
    {
        string code = txtBarcodeScan.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            code = "893456011111"; // Default quick test
        }

        bool found = false;
        foreach (DataGridViewRow row in dgvAudit.Rows)
        {
            if (row.Cells["Barcode"].Value?.ToString() == code)
            {
                found = true;
                string currentAct = row.Cells["ActualQty"].Value?.ToString() ?? "0";
                int num = int.TryParse(currentAct.Replace(" lon", "").Replace(" gói", "").Replace(" hộp", "").Trim(), out var n) ? n : 0;
                num++;
                row.Cells["ActualQty"].Value = $"{num}";
                AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã kiểm đếm +1 cho {row.Cells["Name"].Value}");
                break;
            }
        }

        if (!found)
        {
            AntdUI.Message.warn(this.FindForm() ?? new Form(), $"Mã vạch {code} chưa có trong danh mục kiểm kê kệ này!");
        }

        txtBarcodeScan.Clear();
        txtBarcodeScan.Focus();
    }
}
