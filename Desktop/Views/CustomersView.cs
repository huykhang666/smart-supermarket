using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class CustomersView : UserControl
{
    private Panel pnlHeader = null!;
    private TableLayoutPanel pnlKpiContainer = null!;
    private Panel pnlFilter = null!;
    private Panel pnlGridContainer = null!;
    private DataGridView dgvCustomers = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboTierFilter = null!;
    private Label lblKpiTotalCustomers = null!;
    private Label lblKpiTotalVip = null!;
    private Label lblKpiPoints = null!;
    private Label lblKpiVouchers = null!;
    private Label lblNoteTotalCustomers = null!;
    private Label lblNoteTotalVip = null!;
    private Label lblNotePoints = null!;
    private Label lblNoteVouchers = null!;

    public CustomersView()
    {
        InitializeComponent();
        LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Header Panel ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Khách Hàng & Điểm Tích Lũy (Loyalty)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 6),
            AutoSize = true
        };

        var btnAddCustomer = new Button
        {
            Text = "➕ Thêm Khách Hàng",
            Size = new Size(180, 32),
            Location = new Point(pnlHeader.Width - 180, 8),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        AppTheme.ApplyPrimaryButton(btnAddCustomer);
        btnAddCustomer.Click += (s, e) => {
            var form = this.FindForm();
            if (form != null) AntdUI.Message.info(form, "Chức năng thêm hồ sơ Khách hàng mới.");
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnAddCustomer);

        // --- 2. KPI Stat Cards (4 Cards) ---
        pnlKpiContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlKpiContainer.Controls.Add(AppTheme.CreateKpiCard("TỔNG KHÁCH HÀNG", "0", "Hệ thống Loyalty", AppTheme.Primary, out lblKpiTotalCustomers, out lblNoteTotalCustomers), 0, 0);
        pnlKpiContainer.Controls.Add(AppTheme.CreateKpiCard("THÀNH VIÊN VIP/GOLD", "0", "0% tổng số", AppTheme.Warning, out lblKpiTotalVip, out lblNoteTotalVip), 1, 0);
        pnlKpiContainer.Controls.Add(AppTheme.CreateKpiCard("ĐIỂM TÍCH LŨY DƯ", "0", "Điểm đang lưu hành", AppTheme.Success, out lblKpiPoints, out lblNotePoints), 2, 0);
        pnlKpiContainer.Controls.Add(AppTheme.CreateKpiCard("VOUCHER ĐÃ PHÁT", "0 mã", "Ví khách hàng", AppTheme.Primary, out lblKpiVouchers, out lblNoteVouchers), 3, 0);

        // --- 3. Filter & Search Card ---
        pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(15, 10, 15, 10),
            Margin = new Padding(0, 6, 0, 6)
        };
        AppTheme.ApplyCardPanel(pnlFilter);

        var lblSearch = new Label { Text = "Tìm kiếm:", Location = new Point(15, 16), AutoSize = true, Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary };
        txtSearch = new TextBox
        {
            PlaceholderText = "Nhập Họ tên, Số điện thoại hoặc Mã KH...",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Size = new Size(320, 32),
            Location = new Point(90, 13),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTier = new Label { Text = "Hạng thẻ:", Location = new Point(430, 16), AutoSize = true, Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary };
        cboTierFilter = new ComboBox
        {
            Font = AppTheme.FontBody,
            Size = new Size(180, 32),
            Location = new Point(505, 13),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboTierFilter.Items.AddRange(new[] { "-- Tất cả hạng thẻ --", "👑 Platinum VIP", "🥇 VIP Gold", "🥈 Silver", "🥉 Bronze" });
        cboTierFilter.SelectedIndex = 0;

        var btnSearch = new Button
        {
            Text = "Tìm Kiếm",
            Size = new Size(100, 32),
            Location = new Point(700, 12)
        };
        AppTheme.ApplySecondaryButton(btnSearch);

        pnlFilter.Controls.Add(lblSearch);
        pnlFilter.Controls.Add(txtSearch);
        pnlFilter.Controls.Add(lblTier);
        pnlFilter.Controls.Add(cboTierFilter);
        pnlFilter.Controls.Add(btnSearch);

        // --- 4. DataGrid Container Card ---
        pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlGridContainer);

        dgvCustomers = new DataGridView();
        AppTheme.ApplyGridStyle(dgvCustomers);

        dgvCustomers.Columns.Add("Code", "Mã Khách Hàng");
        dgvCustomers.Columns.Add("FullName", "Họ & Tên");
        dgvCustomers.Columns.Add("Phone", "Số Điện Thoại");
        dgvCustomers.Columns.Add("Points", "Điểm Tích Lũy");
        dgvCustomers.Columns.Add("Tier", "Hạng Thành Viên");
        dgvCustomers.Columns.Add("Vouchers", "Ví Voucher KH");
        dgvCustomers.Columns.Add("TotalSpent", "Tổng Chi Tiêu (VNĐ)");

        pnlGridContainer.Controls.Add(dgvCustomers);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlFilter);
        this.Controls.Add(pnlKpiContainer);
        this.Controls.Add(pnlHeader);
    }

    private async void LoadSampleData()
    {
        dgvCustomers.Rows.Clear();
        using var client = new System.Net.Http.HttpClient();
        try
        {
            var response = await client.GetAsync($"{AppTheme.ApiBaseUrl}/api/customers");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    int totalCust = 0;
                    int totalVip = 0;
                    long totalPoints = 0;
                    int totalVouchers = 0;

                    foreach (var item in data.EnumerateArray())
                    {
                        totalCust++;
                        string code = item.GetProperty("customerCode").GetString() ?? "";
                        string name = item.GetProperty("fullName").GetString() ?? "";
                        string phone = item.GetProperty("phone").GetString() ?? "";
                        int points = item.GetProperty("points").GetInt32();
                        string tier = item.GetProperty("tier").GetString() ?? "";
                        int vouchers = item.GetProperty("vouchersCount").GetInt32();
                        string spent = item.GetProperty("totalSpent").GetString() ?? "0";

                        totalPoints += points;
                        totalVouchers += vouchers;
                        if (tier.Contains("VIP") || tier.Contains("Platinum") || tier.Contains("Gold"))
                        {
                            totalVip++;
                        }

                        dgvCustomers.Rows.Add(code, name, phone, $"{points:N0} điểm", tier, $"{vouchers} mã", spent);
                    }

                    lblKpiTotalCustomers.Text = $"{totalCust:N0}";
                    lblKpiTotalVip.Text = $"{totalVip:N0}";
                    lblKpiPoints.Text = $"{totalPoints:N0}";
                    lblKpiVouchers.Text = $"{totalVouchers:N0} mã";

                    double vipPercent = totalCust > 0 ? (double)totalVip / totalCust * 100.0 : 0;
                    lblNoteTotalVip.Text = $"👑 {vipPercent:F1}% tổng số";

                    ApplyRowColors();
                    return;
                }
            }
        }
        catch
        {
            // API Offline Fallback - keep real 0 values without fake mock data
        }

        lblKpiTotalCustomers.Text = "0";
        lblKpiTotalVip.Text = "0";
        lblKpiPoints.Text = "0";
        lblKpiVouchers.Text = "0 mã";
        lblNoteTotalVip.Text = "0% tổng số";
    }

    private void ApplyRowColors()
    {
        foreach (DataGridViewRow row in dgvCustomers.Rows)
        {
            string tier = row.Cells["Tier"].Value?.ToString() ?? "";
            if (tier.Contains("Platinum"))
            {
                row.Cells["Tier"].Style.BackColor = Color.FromArgb(238, 242, 255);
                row.Cells["Tier"].Style.ForeColor = Color.FromArgb(67, 56, 202);
            }
            else if (tier.Contains("Gold"))
            {
                row.Cells["Tier"].Style.BackColor = Color.FromArgb(254, 243, 199);
                row.Cells["Tier"].Style.ForeColor = Color.FromArgb(180, 83, 9);
            }
        }
    }
}
