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

        var pnlActionsRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnCompleteAudit = new Button
        {
            Text = "✅ Hoàn Tất Kiểm Kê",
            Size = new Size(180, 32)
        };
        AppTheme.ApplyPrimaryButton(btnCompleteAudit);
        btnCompleteAudit.Click += async (s, e) =>
        {
            if (_currentAuditId == null) return;
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var resp = await http.PostAsync($"http://localhost:5137/api/stock-audit/{_currentAuditId}/complete", null);
                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ Đã chốt và lưu biên bản kiểm kê kho thành công!\nTrạng thái đã chuyển thành Completed.", "Hoàn tất kiểm kê", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSampleAuditData(); // Tạo draft mới
                }
            }
            catch { }
        };
        pnlActionsRight.Controls.Add(btnCompleteAudit);

        var pnlInputsLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var lblZone = new Label { Text = "Khu vực kệ:", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
        cboZone = new ComboBox
        {
            Size = new Size(170, 32),
            Font = AppTheme.FontBody,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 2, 12, 0)
        };
        cboZone.Items.AddRange(new[] { "Kệ A1 - Nước giải khát", "Kệ B2 - Bánh kẹo ăn vặt", "Kệ C1 - Sữa & Bơ", "Kệ D3 - Gia vị đồ khô" });
        cboZone.SelectedIndex = 0;

        var lblScan = new Label { Text = "Quét Barcode:", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
        txtBarcodeScan = new TextBox
        {
            Size = new Size(220, 32),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            PlaceholderText = "Quét mã vạch (Enter)...",
            Margin = new Padding(0, 2, 8, 0)
        };
        txtBarcodeScan.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ProcessScan(); };

        var btnScanAdd = new Button
        {
            Text = "➕ Đếm +1",
            Size = new Size(95, 32),
            Margin = new Padding(0, 2, 0, 0)
        };
        AppTheme.ApplyPrimaryButton(btnScanAdd);
        btnScanAdd.Click += (s, e) => ProcessScan();

        pnlInputsLeft.Controls.Add(lblZone);
        pnlInputsLeft.Controls.Add(cboZone);
        pnlInputsLeft.Controls.Add(lblScan);
        pnlInputsLeft.Controls.Add(txtBarcodeScan);
        pnlInputsLeft.Controls.Add(btnScanAdd);

        pnlToolbar.Controls.Add(pnlInputsLeft);
        pnlToolbar.Controls.Add(pnlActionsRight);

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

    private Guid? _currentAuditId;

    private async void LoadSampleAuditData()
    {
        try
        {
            var req = new { CreatedBy = "NV-8821", Zone = "Default", Notes = "" };
            var json = System.Text.Json.JsonSerializer.Serialize(req);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var http = new System.Net.Http.HttpClient();
            var resp = await http.PostAsync("http://localhost:5137/api/stock-audit", content);
            
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(respStr);
                var idStr = doc.RootElement.GetProperty("id").GetString();
                _currentAuditId = Guid.Parse(idStr!);
            }
        }
        catch { }

        dgvAudit.Rows.Clear();
        lblTotalChecked.Text = "Tổng sản phẩm đã kiểm: 0 mặt hàng  |  ";
        lblMatchCount.Text = "Khớp tồn: 0  |  ";
        lblDiffCount.Text = "Chênh lệch: 0";
    }

    private async void ProcessScan()
    {
        string code = txtBarcodeScan.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            code = "893456011111"; // Default quick test
        }

        if (_currentAuditId == null) return;

        int currentAct = 1;
        foreach (DataGridViewRow row in dgvAudit.Rows)
        {
            if (row.Cells["Barcode"].Value?.ToString() == code)
            {
                string act = row.Cells["ActualQty"].Value?.ToString() ?? "0";
                int.TryParse(act, out int n);
                currentAct = n + 1;
                break;
            }
        }

        try
        {
            var req = new { Barcode = code, ActualQty = currentAct };
            var json = System.Text.Json.JsonSerializer.Serialize(req);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var http = new System.Net.Http.HttpClient();
            var resp = await http.PostAsync($"http://localhost:5137/api/stock-audit/{_currentAuditId}/details", content);
            
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(respStr);
                var details = doc.RootElement.GetProperty("details").EnumerateArray();
                
                dgvAudit.Rows.Clear();
                int match = 0, diff = 0;

                foreach(var d in details)
                {
                    string bar = d.GetProperty("barcode").GetString() ?? "";
                    string name = d.GetProperty("productName").GetString() ?? "";
                    int sys = d.GetProperty("systemQty").GetInt32();
                    int act = d.GetProperty("actualQty").GetInt32();
                    int dif = d.GetProperty("varianceQty").GetInt32();
                    string stat = dif == 0 ? "✅ Khớp" : (dif > 0 ? "⚠️ Thừa" : "❌ Thiếu");

                    dgvAudit.Rows.Add(bar, name, sys, act, dif, stat);

                    if (dif == 0) match++; else diff++;
                }

                lblTotalChecked.Text = $"Tổng sản phẩm đã kiểm: {dgvAudit.Rows.Count} mặt hàng  |  ";
                lblMatchCount.Text = $"Khớp tồn: {match}  |  ";
                lblDiffCount.Text = $"Chênh lệch: {diff}";

                AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã kiểm đếm +1 cho mã {code}");
            }
            else
            {
                AntdUI.Message.error(this.FindForm() ?? new Form(), $"Mã vạch {code} chưa có trong hệ thống!");
            }
        }
        catch { }

        txtBarcodeScan.Clear();
        txtBarcodeScan.Focus();
    }
}
