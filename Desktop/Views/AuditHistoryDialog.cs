using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

/// <summary>
/// Hộp thoại Danh Sách & Lịch Sử Các Đợt Kiểm Kê Kho
/// Cho phép quản lý và nhân viên xem lại các đợt kiểm kê cũ, kiểm tra trạng thái và mở lại vào giao diện làm việc.
/// </summary>
public class AuditHistoryDialog : Form
{
    private readonly HttpClient _httpClient;
    private DataGridView dgvHistory = null!;
    private ComboBox cboFilterStatus = null!;
    private Label lblSummary = null!;
    private Button btnOpen = null!;
    private Button btnRefresh = null!;
    private Button btnClose = null!;
    private List<StockAuditDTO> _allAudits = new();

    public Guid? SelectedAuditId { get; private set; }

    public AuditHistoryDialog(HttpClient httpClient)
    {
        _httpClient = httpClient;
        InitializeComponent();
        this.Load += async (s, e) => await LoadAuditsAsync();
    }

    private void InitializeComponent()
    {
        this.Text = "Lịch Sử Các Đợt Kiểm Kê Kho";
        this.Size = new Size(950, 600);
        this.MinimumSize = new Size(800, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.BackColor = AppTheme.BackgroundGray;
        this.Font = AppTheme.FontBody;

        // ==============================================================
        // 1. TOP HEADER & FILTER
        // ==============================================================
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(24, 12, 24, 12)
        };
        pnlTop.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        var pnlTitle = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📋 Lịch Sử Các Đợt Kiểm Kê",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            UseMnemonic = false,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        var lblSub = new Label
        {
            Text = "Danh sách tất cả các phiên kiểm kê, trạng thái đối soát và giá trị hao hụt",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            UseMnemonic = false,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 0)
        };

        pnlTitle.Controls.Add(lblTitle);
        pnlTitle.Controls.Add(lblSub);
        pnlTop.Controls.Add(pnlTitle);

        // Filter Group (Right)
        var pnlFilter = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        var lblFilter = new Label
        {
            Text = "Trạng thái:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0)
        };

        cboFilterStatus = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontBody,
            Width = 160,
            Height = 32
        };
        cboFilterStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Đang kiểm đếm", "Chờ cân bằng kho", "Đã cân bằng kho" });
        cboFilterStatus.SelectedIndex = 0;
        cboFilterStatus.SelectedIndexChanged += (s, e) => FilterAudits();

        btnRefresh = CreateTactileButton("🔄 Làm mới", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 100, 32, true);
        btnRefresh.Margin = new Padding(12, 0, 0, 0);
        btnRefresh.Click += async (s, e) => await LoadAuditsAsync();

        pnlFilter.Controls.Add(lblFilter);
        pnlFilter.Controls.Add(cboFilterStatus);
        pnlFilter.Controls.Add(btnRefresh);
        pnlTop.Controls.Add(pnlFilter);

        this.Controls.Add(pnlTop);

        // ==============================================================
        // 2. BOTTOM ACTION PANEL
        // ==============================================================
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(24, 12, 24, 12)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        lblSummary = new Label
        {
            Text = "Đang tải dữ liệu...",
            Dock = DockStyle.Left,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        btnClose = CreateTactileButton("✖️ Đóng", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 90, 36, true);
        btnClose.Margin = new Padding(0, 0, 10, 0);
        btnClose.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };

        btnOpen = CreateTactileButton("📂 Mở đợt này", AppTheme.Primary, AppTheme.PrimaryHover, AppTheme.PrimaryPressed, Color.White, 125, 36);
        btnOpen.Click += (s, e) => SelectAndOpenAudit();

        pnlButtons.Controls.Add(btnClose);
        pnlButtons.Controls.Add(btnOpen);

        pnlBottom.Controls.Add(lblSummary);
        pnlBottom.Controls.Add(pnlButtons);
        this.Controls.Add(pnlBottom);

        // ==============================================================
        // 3. CENTER GRID PANEL
        // ==============================================================
        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 16),
            BackColor = AppTheme.BackgroundGray
        };

        dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.SurfaceWhite,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = AppTheme.BorderLight,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 42 },
            Font = AppTheme.FontBody
        };

        dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xF8, 0xFA, 0xFC);
        dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        dgvHistory.ColumnHeadersDefaultCellStyle.Font = AppTheme.FontBodyBold;
        dgvHistory.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        dgvHistory.ColumnHeadersHeight = 44;
        dgvHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0xEB, 0xF6, 0xFF);
        dgvHistory.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditId", HeaderText = "ID Phiếu", Width = 95 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditDate", HeaderText = "Ngày Kiểm", Width = 140 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Zone", HeaderText = "Khu Vực", Width = 110 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "Người Tạo", Width = 110 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", Width = 145 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalItems", HeaderText = "Số Mặt Hàng", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalDiscrepancyQty", HeaderText = "Tổng Lệch", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalLossAmount", HeaderText = "Tiền Hao Hụt", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Ghi Chú", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        dgvHistory.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) SelectAndOpenAudit();
        };

        pnlGrid.Controls.Add(dgvHistory);
        this.Controls.Add(pnlGrid);
        pnlGrid.BringToFront();
    }

    private async Task LoadAuditsAsync()
    {
        try
        {
            lblSummary.Text = "Đang tải danh sách đợt kiểm kê...";
            var resp = await _httpClient.GetAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit");
            if (resp.IsSuccessStatusCode)
            {
                var jsonStr = await resp.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<StockAuditDTO>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _allAudits = list ?? new List<StockAuditDTO>();
                FilterAudits();
            }
            else
            {
                lblSummary.Text = "Không thể tải danh sách kiểm kê.";
            }
        }
        catch (Exception ex)
        {
            lblSummary.Text = $"Lỗi: {ex.Message}";
        }
    }

    private void FilterAudits()
    {
        dgvHistory.Rows.Clear();
        string filter = cboFilterStatus.SelectedItem?.ToString() ?? "Tất cả trạng thái";

        var filtered = _allAudits;
        if (filter == "Đang kiểm đếm") filtered = _allAudits.FindAll(a => a.Status == "Draft");
        else if (filter == "Chờ cân bằng kho") filtered = _allAudits.FindAll(a => a.Status == "Completed");
        else if (filter == "Đã cân bằng kho") filtered = _allAudits.FindAll(a => a.Status == "Reconciled");

        foreach (var audit in filtered)
        {
            string statusDisplay = audit.Status switch
            {
                "Reconciled" => "🟢 Đã cân bằng",
                "Completed" => "🟠 Chờ cân bằng",
                _ => "🟡 Đang kiểm đếm"
            };

            int rowIdx = dgvHistory.Rows.Add(
                $"#{audit.Id.ToString()[..8].ToUpper()}",
                audit.AuditDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                audit.Zone,
                audit.CreatedBy,
                statusDisplay,
                audit.TotalItems.ToString("N0"),
                audit.TotalDiscrepancyQty.ToString("N0"),
                audit.TotalLossAmount.ToString("#,##0 đ"),
                audit.Notes
            );

            dgvHistory.Rows[rowIdx].Tag = audit.Id;

            // Highlight reconciled vs pending
            if (audit.Status == "Reconciled")
            {
                dgvHistory.Rows[rowIdx].Cells["Status"].Style.ForeColor = Color.FromArgb(0x0E, 0x68, 0x0E);
                dgvHistory.Rows[rowIdx].Cells["Status"].Style.Font = AppTheme.FontBodyBold;
            }
            else if (audit.Status == "Completed")
            {
                dgvHistory.Rows[rowIdx].Cells["Status"].Style.ForeColor = Color.FromArgb(0x9A, 0x3E, 0x00);
                dgvHistory.Rows[rowIdx].Cells["Status"].Style.Font = AppTheme.FontBodyBold;
            }
            else
            {
                dgvHistory.Rows[rowIdx].Cells["Status"].Style.ForeColor = Color.FromArgb(0x00, 0x5F, 0xB4);
            }
        }

        lblSummary.Text = $"Hiển thị {filtered.Count} / {_allAudits.Count} đợt kiểm kê";
    }

    private void SelectAndOpenAudit()
    {
        if (dgvHistory.SelectedRows.Count == 0)
        {
            MessageBox.Show(this, "Vui lòng chọn một đợt kiểm kê trong bảng để mở.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tag = dgvHistory.SelectedRows[0].Tag;
        if (tag is Guid id)
        {
            SelectedAuditId = id;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
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
