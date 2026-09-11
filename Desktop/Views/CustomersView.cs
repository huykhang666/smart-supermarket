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

    public CustomersView()
    {
        InitializeComponent();
        LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(241, 245, 249); // Modern Slate background (#F1F5F9)
        this.Padding = new Padding(20);

        // --- 1. Header Panel ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "👥 QUẢN LÝ KHÁCH HÀNG THÂN THIẾT & TÍCH ĐIỂM LOYALTY",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(0, 10),
            AutoSize = true
        };

        var btnAddCustomer = new IconButton
        {
            Text = " + Thêm Khách Hàng Mới",
            IconChar = IconChar.UserPlus,
            IconColor = Color.White,
            IconSize = 16,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(14, 165, 233), // Sky Blue Primary (#0EA5E9)
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 36),
            Location = new Point(pnlHeader.Width - 200, 5),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnAddCustomer.FlatAppearance.BorderSize = 0;
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
            Height = 90,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 15)
        };
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlKpiContainer.Controls.Add(CreateKpiCard("TỔNG KHÁCH HÀNG", "5,632", "👥 +128 tuần này", Color.FromArgb(14, 165, 233)), 0, 0);
        pnlKpiContainer.Controls.Add(CreateKpiCard("THÀNH VIÊN VIP/GOLD", "1,280", "👑 22.7% tổng số", Color.FromArgb(245, 158, 11)), 1, 0);
        pnlKpiContainer.Controls.Add(CreateKpiCard("ĐIỂM TÍCH LŨY DƯ", "145,800", "⭐ Quyết đổi quà", Color.FromArgb(16, 185, 129)), 2, 0);
        pnlKpiContainer.Controls.Add(CreateKpiCard("VOUCHER ĐÃ PHÁT", "892 mã", "🎁 Còn 120 voucher", Color.FromArgb(139, 92, 246)), 3, 0);

        // --- 3. Filter & Search Card ---
        pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(15, 12, 15, 12),
            Margin = new Padding(0, 15, 0, 15)
        };

        var lblSearch = new Label { Text = "Tìm kiếm:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        txtSearch = new TextBox
        {
            Text = "Nhập Họ tên, Số điện thoại hoặc Mã KH...",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.Gray,
            Size = new Size(300, 30),
            Location = new Point(90, 14)
        };

        var lblTier = new Label { Text = "Hạng thẻ:", Location = new Point(420, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        cboTierFilter = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Size = new Size(160, 30),
            Location = new Point(490, 14),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboTierFilter.Items.AddRange(new[] { "-- Tất cả hạng thẻ --", "👑 Platinum VIP", "🥇 VIP Gold", "🥈 Silver", "🥉 Bronze" });
        cboTierFilter.SelectedIndex = 0;

        var btnSearch = new IconButton
        {
            Text = " Tìm Kiếm",
            IconChar = IconChar.Search,
            IconColor = Color.White,
            IconSize = 14,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(15, 23, 42),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 32),
            Location = new Point(670, 13),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;

        pnlFilter.Controls.Add(lblSearch);
        pnlFilter.Controls.Add(txtSearch);
        pnlFilter.Controls.Add(lblTier);
        pnlFilter.Controls.Add(cboTierFilter);
        pnlFilter.Controls.Add(btnSearch);

        // --- 4. DataGrid Container Card ---
        pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        dgvCustomers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 9.5f),
            RowTemplate = { Height = 45 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42); // Dark Navy Header
        dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        dgvCustomers.ColumnHeadersHeight = 42;
        dgvCustomers.EnableHeadersVisualStyles = false;
        dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
        dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

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

    private Panel CreateKpiCard(string title, string value, string subtext, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(15)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var accentBrush = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, pnl.Height);
        };

        var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(12, 10), AutoSize = true };
        var lblVal = new Label { Text = value, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, 28), AutoSize = true };
        var lblSub = new Label { Text = subtext, Font = new Font("Segoe UI", 8f, FontStyle.Regular), ForeColor = accentColor, Location = new Point(12, 60), AutoSize = true };

        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblVal);
        pnl.Controls.Add(lblSub);
        return pnl;
    }

    private async void LoadSampleData()
    {
        dgvCustomers.Rows.Clear();
        using var client = new System.Net.Http.HttpClient();
        try
        {
            var response = await client.GetAsync("http://localhost:5137/api/customers");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        string code = item.GetProperty("customerCode").GetString() ?? "";
                        string name = item.GetProperty("fullName").GetString() ?? "";
                        string phone = item.GetProperty("phone").GetString() ?? "";
                        int points = item.GetProperty("points").GetInt32();
                        string tier = item.GetProperty("tier").GetString() ?? "";
                        int vouchers = item.GetProperty("vouchersCount").GetInt32();
                        string spent = item.GetProperty("totalSpent").GetString() ?? "0";

                        dgvCustomers.Rows.Add(code, name, phone, $"{points:N0} điểm", tier, $"{vouchers} mã", spent);
                    }
                    ApplyRowColors();
                    return;
                }
            }
        }
        catch
        {
            // API Offline Fallback
        }

        dgvCustomers.Rows.Add("KH-0001", "Lê Văn Khách Hàng", "0988776655", "150 điểm", "🥈 Silver", "1 mã", "1,500,000");
        ApplyRowColors();
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
