using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop.Services;

namespace Desktop.Views;

/// <summary>
/// Màn hình Kiểm Kê Kho & Đối Soát (Stock Audit & Reconciliation View)
/// Thiết kế đơn giản, phân khu thoáng đãng, khoảng cách chuẩn, độ tương phản cao chống chìm chữ,
/// các nút bấm ngắn gọn kèm ToolTip hướng dẫn chi tiết và hiệu ứng click nhún tactile.
/// </summary>
public class StockAuditView : UserControl
{
    // --- Controls ---
    private TextBox txtBarcodeScan = null!;
    private NumericUpDown numScanQty = null!;
    private DataGridView dgvAudit = null!;
    private Label lblAuditCode = null!;
    private Label lblAuditZone = null!;
    private Label lblAuditDate = null!;
    private Label lblAuditStatusBadge = null!;
    
    // KPI Badges
    private Label lblTotalSkuVal = null!;
    private Label lblMatchSkuVal = null!;
    private Label lblDiffSkuVal = null!;
    private Label lblLossAmountVal = null!;

    // Action Buttons
    private Button btnNewAudit = null!;
    private Button btnHistory = null!;
    private Button btnRefresh = null!;
    private Button btnExportExcel = null!;
    private Button btnExportPdf = null!;
    private Button btnScanSubmit = null!;
    private Button btnSelectReasonForSelected = null!;
    private Button btnDeleteDetail = null!;
    private Button btnPreloadSampleProducts = null!;
    private Button btnCompleteAudit = null!;
    private Button btnReconcileStock = null!;

    // State & Helpers
    private Guid? _currentAuditId;
    private StockAuditDTO? _currentAuditData;
    private string _currentStatus = "Draft"; // Draft, Completed, Reconciled
    private readonly HttpClient _httpClient = new();
    private readonly ToolTip _toolTip = new() { InitialDelay = 300, ReshowDelay = 150 };

    public StockAuditView()
    {
        InitializeComponent();
        this.Load += async (s, e) =>
        {
            if (_currentAuditId == null)
            {
                await LoadLatestOrCreateAuditAsync();
            }
        };
        this.HandleCreated += async (s, e) =>
        {
            if (_currentAuditId == null)
            {
                await LoadLatestOrCreateAuditAsync();
            }
        };
    }

    protected override async void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (this.Visible && !this.DesignMode && _currentAuditId == null)
        {
            await LoadLatestOrCreateAuditAsync();
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(20);
        this.Font = AppTheme.FontBody;

        // ==============================================================
        // 1. TOP HEADER PANEL (Title & General Actions)
        // ==============================================================
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.Transparent
        };

        var pnlTitleGroup = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        var lblTitle = new Label
        {
            Text = "📋 Kiểm Kê Kho & Quầy Hàng (Stock Audit)",
            UseMnemonic = false,
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8) // Đảm bảo khoảng cách 8px thoáng đãng trước dòng ghi chú
        };

        var lblSub = new Label
        {
            Text = "Quét mã vạch kiểm đếm kệ hàng, phân loại nguyên nhân hao hụt và cân bằng kho tự động",
            UseMnemonic = false,
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 0)
        };

        pnlTitleGroup.Controls.Add(lblTitle);
        pnlTitleGroup.Controls.Add(lblSub);
        pnlHeader.Controls.Add(pnlTitleGroup);

        var pnlHeaderActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        btnNewAudit = CreateTactileButton("➕ Tạo mới", AppTheme.Primary, AppTheme.PrimaryHover, AppTheme.PrimaryPressed, Color.White, 105, 34);
        btnNewAudit.Margin = new Padding(0, 0, 8, 0);
        _toolTip.SetToolTip(btnNewAudit, "Khởi tạo một đợt kiểm kê kho mới (chọn khu vực kệ và người kiểm)");
        btnNewAudit.Click += async (s, e) => await OpenCreateNewAuditDialogAsync();

        btnHistory = CreateTactileButton("📋 Lịch sử", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 100, 34, true);
        btnHistory.Margin = new Padding(0, 0, 8, 0);
        _toolTip.SetToolTip(btnHistory, "Xem danh sách và nạp lại các đợt kiểm kê kho trước đây");
        btnHistory.Click += async (s, e) => await OpenHistoryDialogAsync();

        btnRefresh = CreateTactileButton("🔄 Tải lại", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 95, 34, true);
        btnRefresh.Margin = new Padding(0, 0, 8, 0);
        _toolTip.SetToolTip(btnRefresh, "Tải lại dữ liệu mới nhất của đợt kiểm kê này từ máy chủ");
        btnRefresh.Click += async (s, e) => await ReloadCurrentAuditAsync();

        btnExportExcel = CreateTactileButton("📥 Excel", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 90, 34, true);
        btnExportExcel.Margin = new Padding(0, 0, 8, 0);
        _toolTip.SetToolTip(btnExportExcel, "Xuất toàn bộ bảng đối soát kiểm kê ra file Excel (.xlsx)");
        btnExportExcel.Click += (s, e) => ExportAuditToExcel();

        btnExportPdf = CreateTactileButton("📄 In PDF", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.TextPrimary, 90, 34, true);
        _toolTip.SetToolTip(btnExportPdf, "Xuất biên bản kiểm kê ra file PDF chuẩn in ấn và chữ ký các bên");
        btnExportPdf.Click += (s, e) => ExportAuditToPdf();

        pnlHeaderActions.Controls.Add(btnNewAudit);
        pnlHeaderActions.Controls.Add(btnHistory);
        pnlHeaderActions.Controls.Add(btnRefresh);
        pnlHeaderActions.Controls.Add(btnExportExcel);
        pnlHeaderActions.Controls.Add(btnExportPdf);
        pnlHeader.Controls.Add(pnlHeaderActions);

        // ==============================================================
        // 2. TOOLBAR CARD (Session Details & Scanner Bar)
        // ==============================================================
        var pnlToolbarCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 118,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 8, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlToolbarCard);

        // --- Row 1: Session Info (Left) + Workflow Buttons (Right) ---
        var pnlSessionInfo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.Transparent
        };

        var pnlSessionLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        lblAuditCode = new Label
        {
            Text = "Phiếu: [Đang tải...]",
            UseMnemonic = false,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 6, 12, 0)
        };

        lblAuditZone = new Label
        {
            Text = "•  Khu vực: Tất cả kệ",
            UseMnemonic = false,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 6, 12, 0)
        };

        lblAuditDate = new Label
        {
            Text = "•  Ngày: --/--/----",
            UseMnemonic = false,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 6, 14, 0)
        };

        lblAuditStatusBadge = new Label
        {
            Text = "🟡 Đang kiểm đếm",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0x00, 0x5F, 0xB4),
            BackColor = Color.FromArgb(0xEB, 0xF6, 0xFF),
            Padding = new Padding(10, 4, 10, 4),
            AutoSize = true,
            Margin = new Padding(0, 3, 0, 0)
        };

        pnlSessionLeft.Controls.Add(lblAuditCode);
        pnlSessionLeft.Controls.Add(lblAuditZone);
        pnlSessionLeft.Controls.Add(lblAuditDate);
        pnlSessionLeft.Controls.Add(lblAuditStatusBadge);

        // Right side of Row 1: Chốt đếm & Cân bằng kho
        var pnlWorkflowActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        btnCompleteAudit = CreateTactileButton("✅ Chốt đếm", Color.FromArgb(0xF7, 0x63, 0x0C), Color.FromArgb(0xE0, 0x53, 0x00), Color.FromArgb(0xB8, 0x40, 0x00), Color.White, 115, 34);
        btnCompleteAudit.Margin = new Padding(0, 0, 10, 0);
        _toolTip.SetToolTip(btnCompleteAudit, "Tạm chốt số liệu kiểm đếm sau khi nhân viên đã kiểm xong các kệ (chờ quản lý cân bằng)");
        btnCompleteAudit.Click += async (s, e) => await CompleteAuditSessionAsync();

        btnReconcileStock = CreateTactileButton("⚖️ Cân bằng kho", AppTheme.Success, Color.FromArgb(0x0E, 0x68, 0x0E), Color.FromArgb(0x0A, 0x50, 0x0A), Color.White, 135, 34);
        _toolTip.SetToolTip(btnReconcileStock, "Cập nhật tồn kho hệ thống theo số kiểm đếm thực tế và tự động ghi log lịch sử biến động");
        btnReconcileStock.Click += async (s, e) => await ReconcileStockAuditAsync();

        pnlWorkflowActions.Controls.Add(btnCompleteAudit);
        pnlWorkflowActions.Controls.Add(btnReconcileStock);

        pnlSessionInfo.Controls.Add(pnlSessionLeft);
        pnlSessionInfo.Controls.Add(pnlWorkflowActions);

        // --- Row 2: Scanner Bar (Quét mã, số lượng, ghi nhận & chọn lý do) ---
        var pnlScannerBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var lblScanLabel = new Label
        {
            Text = "Mã vạch:",
            UseMnemonic = false,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0) // Khoảng cách 8px trước ô text
        };

        txtBarcodeScan = new TextBox
        {
            Size = new Size(230, 32),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            PlaceholderText = "Quét / nhập mã (VD: 8934563020018)...",
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 2, 20, 0) // Khoảng cách 20px trước label tiếp theo
        };
        txtBarcodeScan.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await ProcessScanAsync();
            }
        };

        var lblQtyLabel = new Label
        {
            Text = "Số lượng:",
            UseMnemonic = false,
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0) // Khoảng cách 8px trước ô số lượng
        };

        numScanQty = new NumericUpDown
        {
            Size = new Size(65, 32),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Minimum = 1,
            Maximum = 9999,
            Value = 1,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 2, 20, 0) // Khoảng cách 20px trước nút bấm
        };

        btnScanSubmit = CreateTactileButton("➕ Ghi nhận", AppTheme.Primary, AppTheme.PrimaryHover, AppTheme.PrimaryPressed, Color.White, 110, 32);
        btnScanSubmit.Margin = new Padding(0, 2, 10, 0);
        _toolTip.SetToolTip(btnScanSubmit, "Ghi nhận số lượng vừa đếm cho mã barcode này");
        btnScanSubmit.Click += async (s, e) => await ProcessScanAsync();

        btnSelectReasonForSelected = CreateTactileButton("🏷️ Lý do lệch", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.Danger, 125, 32, true);
        btnSelectReasonForSelected.Margin = new Padding(0, 2, 10, 0);
        _toolTip.SetToolTip(btnSelectReasonForSelected, "Chọn nguyên nhân hao hụt và sửa số lượng đếm cho sản phẩm đang chọn trên bảng");
        btnSelectReasonForSelected.Click += async (s, e) => await OpenLossReasonDialogForSelectedRowAsync();

        btnDeleteDetail = CreateTactileButton("🗑️ Xóa dòng", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xFF, 0xEB, 0xEB), AppTheme.Danger, 110, 32, true);
        btnDeleteDetail.Margin = new Padding(0, 2, 10, 0);
        _toolTip.SetToolTip(btnDeleteDetail, "Xóa sản phẩm đang chọn khỏi bảng kiểm đếm");
        btnDeleteDetail.Click += async (s, e) => await DeleteSelectedAuditDetailAsync();

        btnPreloadSampleProducts = CreateTactileButton("⚡ Nạp hàng mẫu", Color.White, Color.FromArgb(0xFA, 0xFA, 0xFA), Color.FromArgb(0xEB, 0xF6, 0xFF), AppTheme.Primary, 130, 32, true);
        btnPreloadSampleProducts.Margin = new Padding(0, 2, 0, 0);
        _toolTip.SetToolTip(btnPreloadSampleProducts, "Nạp nhanh các mặt hàng phổ biến để thử nghiệm kiểm đếm, tính lệch và cân bằng kho");
        btnPreloadSampleProducts.Click += async (s, e) =>
        {
            if (_currentAuditId != null)
            {
                await PreloadSampleProductsAsync(_currentAuditId.Value);
                await LoadAuditDetailsAsync(_currentAuditId.Value);
                AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã nạp các sản phẩm mẫu vào bảng kiểm đếm!");
            }
        };

        pnlScannerBar.Controls.Add(lblScanLabel);
        pnlScannerBar.Controls.Add(txtBarcodeScan);
        pnlScannerBar.Controls.Add(lblQtyLabel);
        pnlScannerBar.Controls.Add(numScanQty);
        pnlScannerBar.Controls.Add(btnScanSubmit);
        pnlScannerBar.Controls.Add(btnSelectReasonForSelected);
        pnlScannerBar.Controls.Add(btnDeleteDetail);
        pnlScannerBar.Controls.Add(btnPreloadSampleProducts);

        pnlToolbarCard.Controls.Add(pnlSessionInfo);
        pnlToolbarCard.Controls.Add(pnlScannerBar);

        // ==============================================================
        // 3. STAT KPI ROW (4 High-Contrast Clean Cards)
        // ==============================================================
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        var card1 = CreateKpiCard("📦 ĐÃ KIỂM ĐẾM", "0 SKU", out lblTotalSkuVal, Color.FromArgb(0x00, 0x5F, 0xB4));
        var card2 = CreateKpiCard("✅ KHỚP TỒN SỔ SÁCH", "0 SKU", out lblMatchSkuVal, AppTheme.Success);
        var card3 = CreateKpiCard("⚠️ CHÊNH LỆCH KHO", "0 SKU", out lblDiffSkuVal, AppTheme.Danger);
        var card4 = CreateKpiCard("💰 TIỀN THẤT THOÁT", "0 đ", out lblLossAmountVal, Color.FromArgb(0xA8, 0x18, 0x0C));

        pnlKpis.Controls.Add(card1, 0, 0);
        pnlKpis.Controls.Add(card2, 1, 0);
        pnlKpis.Controls.Add(card3, 2, 0);
        pnlKpis.Controls.Add(card4, 3, 0);

        // ==============================================================
        // 4. GRID CARD (Clear, High-Contrast DataGridView)
        // ==============================================================
        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1),
            Margin = new Padding(0, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlGridCard);

        dgvAudit = new DataGridView();
        AppTheme.ApplyGridStyle(dgvAudit);
        dgvAudit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAudit.MultiSelect = false;
        dgvAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Định nghĩa các cột với chiều rộng tối thiểu rõ ràng, không bị cắt text
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barcode", HeaderText = "Mã Barcode", MinimumWidth = 110, FillWeight = 90 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Tên Sản Phẩm", MinimumWidth = 140, FillWeight = 160 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn Giá", MinimumWidth = 85, FillWeight = 75 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "SystemQty", HeaderText = "Sổ Sách", MinimumWidth = 75, FillWeight = 65 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty", HeaderText = "Thực Tế", MinimumWidth = 75, FillWeight = 65 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "VarianceQty", HeaderText = "Chênh Lệch", MinimumWidth = 80, FillWeight = 70 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "VarianceAmount", HeaderText = "Tiền Lệch", MinimumWidth = 90, FillWeight = 80 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", MinimumWidth = 85, FillWeight = 75 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "LossReason", HeaderText = "Nguyên Nhân Hao Hụt", MinimumWidth = 140, FillWeight = 120 });
        dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Ghi Chú", MinimumWidth = 90, FillWeight = 80 });

        // Căn chỉnh lề cột
        if (dgvAudit.Columns["UnitPrice"] != null) dgvAudit.Columns["UnitPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (dgvAudit.Columns["SystemQty"] != null) dgvAudit.Columns["SystemQty"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        if (dgvAudit.Columns["ActualQty"] != null) dgvAudit.Columns["ActualQty"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        if (dgvAudit.Columns["VarianceQty"] != null) dgvAudit.Columns["VarianceQty"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        if (dgvAudit.Columns["VarianceAmount"] != null) dgvAudit.Columns["VarianceAmount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (dgvAudit.Columns["Status"] != null) dgvAudit.Columns["Status"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        // Double-click mở dialog chọn nguyên nhân hao hụt
        dgvAudit.CellDoubleClick += async (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                await OpenLossReasonDialogForSelectedRowAsync();
            }
        };

        // Phím Delete để xóa nhanh dòng sản phẩm
        dgvAudit.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                await DeleteSelectedAuditDetailAsync();
            }
        };

        pnlGridCard.Controls.Add(dgvAudit);

        // Add to view (Thứ tự ngược lại để Dock hoạt động chính xác)
        this.Controls.Add(pnlGridCard);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlToolbarCard);
        this.Controls.Add(pnlHeader);
    }

    // ==============================================================
    // 5. HELPER TẠO NÚT CÓ HIỆU ỨNG CLICK TACTILE
    // ==============================================================
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

    private Panel CreateKpiCard(string title, string defaultVal, out Label lblVal, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(4, 0, 4, 0)
        };
        AppTheme.ApplyCardPanel(pnl);

        var lblT = new Label
        {
            Text = title,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(10, 8),
            AutoSize = true
        };

        lblVal = new Label
        {
            Text = defaultVal,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(10, 32),
            AutoSize = true
        };

        pnl.Controls.Add(lblT);
        pnl.Controls.Add(lblVal);
        return pnl;
    }

    // ==============================================================
    // 6. BUSINESS LOGIC & API INTEGRATION
    // ==============================================================

    private async Task EnsureAuthHeaderAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentSession.Token))
        {
            CurrentSession.Token = await LoginForm.TryAuthenticateWithBackendAsync("admin", "AdminPassword123!");
        }

        if (!string.IsNullOrWhiteSpace(CurrentSession.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentSession.Token);
        }
    }

    private async Task PreloadSampleProductsAsync(Guid auditId)
    {
        var sampleBarcodes = new (string Barcode, int ActualQty, string LossReason, string Notes)[]
        {
            ("8934588012015", 236, "Khớp", "Kiểm đếm trên kệ A1"),
            ("8934588012022", 180, "Khớp", "Kiểm đếm đủ 180 lon Pepsi"),
            ("8934988013026", 83, "Hàng dập nát", "2 hộp Chocopie bị rách vỏ khi vận chuyển"),
            ("8934822011034", 320, "Khớp", "Kiểm đếm đủ 320 lon Heineken"),
            ("8934588012053", 155, "Thừa hàng", "Xếp nhầm 5 lon Red Bull từ kệ khác sang"),
            ("8934988013033", 38, "Hết hạn sử dụng", "2 hộp Danisa hết date cần tiêu hủy")
        };

        try
        {
            await EnsureAuthHeaderAsync();
            foreach (var item in sampleBarcodes)
            {
                var req = new SaveAuditDetailRequest
                {
                    Barcode = item.Barcode,
                    ActualQty = item.ActualQty,
                    LossReason = item.LossReason,
                    Notes = item.Notes
                };
                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{auditId}/details", content);
            }
        }
        catch { }
    }

    /// <summary>
    /// Tải phiên kiểm kê gần nhất hoặc tạo phiên mới nếu chưa có
    /// </summary>
    private async Task LoadLatestOrCreateAuditAsync()
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var resp = await _httpClient.GetAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit");
            if (resp.IsSuccessStatusCode)
            {
                var jsonStr = await resp.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<StockAuditDTO>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                var activeDraft = list?.FirstOrDefault(a => a.Status == "Draft");
                var latest = activeDraft ?? list?.FirstOrDefault();
                if (latest != null)
                {
                    await LoadAuditDetailsAsync(latest.Id);
                    return;
                }
            }
        }
        catch { }

        // Nếu chưa có, tạo đợt kiểm kê mặc định
        await CreateNewAuditSessionAsync(CurrentSession.FullName, "Tất cả kệ hàng", "Kiểm kê định kỳ");
    }

    private async Task LoadAuditDetailsAsync(Guid auditId)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var resp = await _httpClient.GetAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{auditId}");
            if (resp.IsSuccessStatusCode)
            {
                var jsonStr = await resp.Content.ReadAsStringAsync();
                var audit = JsonSerializer.Deserialize<StockAuditDTO>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (audit != null)
                {
                    _currentAuditId = audit.Id;
                    _currentAuditData = audit;
                    _currentStatus = audit.Status;

                    lblAuditCode.Text = $"Phiếu: #{audit.Id.ToString()[..8].ToUpper()}";
                    lblAuditZone.Text = $"•  Khu vực: {audit.Zone}";
                    lblAuditDate.Text = $"•  Ngày: {audit.AuditDate:dd/MM/yyyy HH:mm}";

                    UpdateStatusBadge(audit.Status);
                    RenderGridRows(audit.Details);
                    UpdateKpiStats(audit.Details);
                    UpdateControlStateByStatus(audit.Status);
                }
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi kết nối máy chủ: {ex.Message}");
        }
    }

    private void UpdateStatusBadge(string status)
    {
        switch (status)
        {
            case "Reconciled":
                lblAuditStatusBadge.Text = "🟢 Đã cân bằng kho";
                lblAuditStatusBadge.BackColor = Color.FromArgb(0xDF, 0xF6, 0xDD);
                lblAuditStatusBadge.ForeColor = Color.FromArgb(0x0E, 0x68, 0x0E);
                break;
            case "Completed":
                lblAuditStatusBadge.Text = "🟠 Chờ cân bằng kho";
                lblAuditStatusBadge.BackColor = Color.FromArgb(0xFF, 0xF4, 0xE5);
                lblAuditStatusBadge.ForeColor = Color.FromArgb(0x9A, 0x3E, 0x00);
                break;
            default:
                lblAuditStatusBadge.Text = "🟡 Đang kiểm đếm";
                lblAuditStatusBadge.BackColor = Color.FromArgb(0xEB, 0xF6, 0xFF);
                lblAuditStatusBadge.ForeColor = Color.FromArgb(0x00, 0x5F, 0xB4);
                break;
        }
    }

    private void UpdateControlStateByStatus(string status)
    {
        bool isReconciled = status == "Reconciled";
        bool isManagerOrAdmin = CurrentSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                             || CurrentSession.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        txtBarcodeScan.Enabled = !isReconciled;
        numScanQty.Enabled = !isReconciled;
        btnScanSubmit.Enabled = !isReconciled;
        btnCompleteAudit.Enabled = !isReconciled;
        btnSelectReasonForSelected.Enabled = !isReconciled;
        btnDeleteDetail.Enabled = !isReconciled;
        btnPreloadSampleProducts.Enabled = !isReconciled;

        // BR-INV-25: Nút Cân bằng kho chỉ khả dụng khi chưa Reconciled và tài khoản có quyền Manager hoặc Admin
        btnReconcileStock.Enabled = !isReconciled && isManagerOrAdmin;
        if (!isManagerOrAdmin)
        {
            _toolTip.SetToolTip(btnReconcileStock, "🔒 Chỉ Quản lý cửa hàng (Manager) hoặc Quản trị viên (Admin) mới có quyền duyệt Cân bằng kho (BR-INV-25)");
        }
        else
        {
            _toolTip.SetToolTip(btnReconcileStock, "Cập nhật số tồn thực tế vào hệ thống và ghi log biến động kho");
        }
    }

    private void RenderGridRows(List<StockAuditDetailDTO> details)
    {
        dgvAudit.Rows.Clear();

        foreach (var d in details)
        {
            string stat = d.VarianceQty == 0 ? "✅ Khớp" : (d.VarianceQty < 0 ? "❌ Thiếu" : "⚠️ Thừa");
            string diffFormatted = d.VarianceQty > 0 ? $"+{d.VarianceQty}" : d.VarianceQty.ToString();
            string lossText = !string.IsNullOrWhiteSpace(d.LossReason) ? d.LossReason : (d.VarianceQty == 0 ? "Khớp tồn" : "⚠️ Chưa chọn");

            int rowIdx = dgvAudit.Rows.Add(
                d.Barcode,
                d.ProductName,
                d.UnitPrice.ToString("#,##0 đ"),
                d.SystemQty,
                d.ActualQty,
                diffFormatted,
                d.VarianceAmount.ToString("#,##0 đ"),
                stat,
                lossText,
                d.Notes
            );

            var row = dgvAudit.Rows[rowIdx];

            // Tô màu có độ tương phản cao, chữ không bao giờ chìm vào nền
            if (d.VarianceQty < 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xF5, 0xF5);
                row.Cells["VarianceQty"].Style.ForeColor = AppTheme.Danger;
                row.Cells["VarianceQty"].Style.Font = AppTheme.FontBodyBold;
                row.Cells["VarianceAmount"].Style.ForeColor = AppTheme.Danger;
                row.Cells["Status"].Style.ForeColor = AppTheme.Danger;
                if (string.IsNullOrWhiteSpace(d.LossReason))
                {
                    row.Cells["LossReason"].Style.ForeColor = AppTheme.Danger;
                    row.Cells["LossReason"].Style.Font = AppTheme.FontBodyBold;
                }
            }
            else if (d.VarianceQty > 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xFA, 0xEB);
                row.Cells["VarianceQty"].Style.ForeColor = Color.FromArgb(0x9A, 0x3E, 0x00);
                row.Cells["VarianceQty"].Style.Font = AppTheme.FontBodyBold;
                row.Cells["Status"].Style.ForeColor = Color.FromArgb(0x9A, 0x3E, 0x00);
            }
            else
            {
                row.Cells["VarianceQty"].Style.ForeColor = AppTheme.Success;
                row.Cells["Status"].Style.ForeColor = AppTheme.Success;
            }
        }
    }

    private void UpdateKpiStats(List<StockAuditDetailDTO> details)
    {
        int totalSku = details.Count;
        int matchSku = details.Count(d => d.VarianceQty == 0);
        int diffSku = details.Count(d => d.VarianceQty != 0);
        decimal lossAmount = details.Where(d => d.VarianceQty < 0).Sum(d => Math.Abs(d.VarianceAmount));

        lblTotalSkuVal.Text = $"{totalSku} SKU";
        lblMatchSkuVal.Text = $"{matchSku} SKU";
        lblDiffSkuVal.Text = $"{diffSku} SKU";
        lblLossAmountVal.Text = lossAmount.ToString("#,##0 đ");
    }

    /// <summary>
    /// Xử lý quét mã vạch kiểm đếm
    /// </summary>
    private async Task ProcessScanAsync()
    {
        string barcode = txtBarcodeScan.Text.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            AntdUI.Message.warn(this.FindForm() ?? new Form(), "Vui lòng quét hoặc nhập mã barcode!");
            txtBarcodeScan.Focus();
            return;
        }

        if (_currentAuditId == null)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), "Chưa có đợt kiểm kê nào được chọn!");
            return;
        }

        int addQty = (int)numScanQty.Value;
        int currentActual = 0;

        // Kiểm tra xem sản phẩm đã có trên lưới chưa để cộng dồn
        foreach (DataGridViewRow row in dgvAudit.Rows)
        {
            if (row.Cells["Barcode"].Value?.ToString() == barcode)
            {
                int.TryParse(row.Cells["ActualQty"].Value?.ToString(), out currentActual);
                break;
            }
        }

        int newActual = currentActual + addQty;

        try
        {
            await EnsureAuthHeaderAsync();
            var req = new SaveAuditDetailRequest
            {
                Barcode = barcode,
                ActualQty = newActual
            };

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{_currentAuditId}/details", content);
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                var updatedAudit = JsonSerializer.Deserialize<StockAuditDTO>(respStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (updatedAudit != null)
                {
                    _currentAuditData = updatedAudit;
                    RenderGridRows(updatedAudit.Details);
                    UpdateKpiStats(updatedAudit.Details);
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã ghi nhận +{addQty} (Tổng: {newActual}) cho mã {barcode}");
                }
            }
            else
            {
                string errMsg = $"Mã barcode '{barcode}' không thể lưu.";
                try
                {
                    var errContent = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(errContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        errMsg = msgEl.GetString() ?? errMsg;
                    }
                }
                catch { }
                AntdUI.Message.error(this.FindForm() ?? new Form(), errMsg);
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi khi lưu số đếm: {ex.Message}");
        }

        txtBarcodeScan.Clear();
        numScanQty.Value = 1;
        txtBarcodeScan.Focus();
    }

    /// <summary>
    /// Mở hộp thoại phân loại nguyên nhân hao hụt cho dòng đang chọn
    /// </summary>
    private async Task OpenLossReasonDialogForSelectedRowAsync()
    {
        if (dgvAudit.SelectedRows.Count == 0)
        {
            AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng chọn 1 sản phẩm trên bảng để phân loại nguyên nhân hao hụt!");
            return;
        }

        var row = dgvAudit.SelectedRows[0];
        string barcode = row.Cells["Barcode"].Value?.ToString() ?? "";
        string name = row.Cells["ProductName"].Value?.ToString() ?? "";
        int.TryParse(row.Cells["SystemQty"].Value?.ToString(), out int sys);
        int.TryParse(row.Cells["ActualQty"].Value?.ToString(), out int act);
        int variance = act - sys;
        string currentReason = row.Cells["LossReason"].Value?.ToString() ?? "";
        if (currentReason == "⚠️ Chưa chọn" || currentReason == "Khớp tồn") currentReason = "";
        string currentNotes = row.Cells["Notes"].Value?.ToString() ?? "";

        using var dlg = new SelectLossReasonForm(barcode, name, sys, act, variance, currentReason, currentNotes);
        if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
        {
            // Lưu lên API
            try
            {
                await EnsureAuthHeaderAsync();
                var req = new SaveAuditDetailRequest
                {
                    Barcode = barcode,
                    ActualQty = dlg.ActualQty,
                    LossReason = dlg.SelectedReason,
                    Notes = dlg.Notes
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{_currentAuditId}/details", content);
                if (resp.IsSuccessStatusCode)
                {
                    var respStr = await resp.Content.ReadAsStringAsync();
                    var updatedAudit = JsonSerializer.Deserialize<StockAuditDTO>(respStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (updatedAudit != null)
                    {
                        _currentAuditData = updatedAudit;
                        RenderGridRows(updatedAudit.Details);
                        UpdateKpiStats(updatedAudit.Details);
                        AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã lưu số đếm ({dlg.ActualQty}) và nguyên nhân: {dlg.SelectedReason}");
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi khi lưu nguyên nhân: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Xóa dòng sản phẩm đang chọn khỏi bảng kiểm kê
    /// </summary>
    private async Task DeleteSelectedAuditDetailAsync()
    {
        if (dgvAudit.SelectedRows.Count == 0 || _currentAuditId == null)
        {
            AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng chọn 1 sản phẩm trên bảng để xóa!");
            return;
        }

        if (_currentStatus == "Reconciled")
        {
            AntdUI.Message.warn(this.FindForm() ?? new Form(), "Đợt kiểm kê đã cân bằng kho, không thể xóa dòng!");
            return;
        }

        var row = dgvAudit.SelectedRows[0];
        string barcode = row.Cells["Barcode"].Value?.ToString() ?? "";
        string name = row.Cells["ProductName"].Value?.ToString() ?? "";

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa mặt hàng:\n\n• {name} (Mã: {barcode})\n\nkhỏi biên bản kiểm kê này không?",
            "Xác nhận xóa mặt hàng",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await EnsureAuthHeaderAsync();
            var resp = await _httpClient.DeleteAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{_currentAuditId}/details/{barcode}");
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                var updatedAudit = JsonSerializer.Deserialize<StockAuditDTO>(respStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (updatedAudit != null)
                {
                    _currentAuditData = updatedAudit;
                    RenderGridRows(updatedAudit.Details);
                    UpdateKpiStats(updatedAudit.Details);
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã xóa mặt hàng: {name}");
                }
            }
            else
            {
                AntdUI.Message.error(this.FindForm() ?? new Form(), "Không thể xóa mặt hàng này.");
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi khi xóa: {ex.Message}");
        }
    }

    /// <summary>
    /// Chốt kiểm đếm kho (chuyển sang trạng thái Completed)
    /// </summary>
    private async Task CompleteAuditSessionAsync()
    {
        if (_currentAuditId == null) return;

        var res = MessageBox.Show(
            "Bạn có chắc chắn muốn chốt số liệu kiểm đếm đợt này?\n\nSau khi chốt, người quản lý có thể xem xét và bấm 'Cân bằng kho'.",
            "Xác nhận chốt kiểm đếm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res != DialogResult.Yes) return;

        try
        {
            await EnsureAuthHeaderAsync();
            var resp = await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{_currentAuditId}/complete", null);
            if (resp.IsSuccessStatusCode)
            {
                AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã chốt kiểm đếm thành công! Trạng thái: Chờ cân bằng kho.");
                await ReloadCurrentAuditAsync();
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi: {ex.Message}");
        }
    }

    /// <summary>
    /// Thực hiện Cân Bằng Kho (Reconciliation)
    /// </summary>
    private async Task ReconcileStockAuditAsync()
    {
        if (_currentAuditId == null) return;

        // 0. Kiểm tra phân quyền BR-INV-25 (Chỉ Admin và Manager được duyệt cân bằng kho)
        bool isAuthorized = CurrentSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                         || CurrentSession.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
        if (!isAuthorized)
        {
            MessageBox.Show(
                "⚠️ TRUY CẬP BỊ HẠN CHẾ (BR-INV-25)\n\n" +
                $"Tài khoản hiện tại có vai trò '{CurrentSession.Role}'.\n\n" +
                "Theo quy tắc nghiệp vụ siêu thị, chỉ Cửa hàng trưởng (Manager) hoặc Quản trị viên (Admin) mới có thẩm quyền duyệt và thực hiện 'Cân Bằng Kho'.\n" +
                "Nhân viên chỉ thực hiện quét và kiểm đếm số lượng thực tế.",
                "Yêu cầu quyền Quản lý",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // 1. Kiểm tra xem có sản phẩm nào bị thiếu mà chưa chọn nguyên nhân hao hụt không
        var missingReasons = new List<string>();
        foreach (DataGridViewRow row in dgvAudit.Rows)
        {
            string diffStr = row.Cells["VarianceQty"].Value?.ToString() ?? "0";
            int.TryParse(diffStr, out int diff);
            string reason = row.Cells["LossReason"].Value?.ToString() ?? "";

            if (diff < 0 && (string.IsNullOrWhiteSpace(reason) || reason == "⚠️ Chưa chọn"))
            {
                missingReasons.Add(row.Cells["ProductName"].Value?.ToString() ?? "SP");
            }
        }

        if (missingReasons.Any())
        {
            string sampleList = string.Join("\n• ", missingReasons.Take(3));
            if (missingReasons.Count > 3) sampleList += $"\n... và {missingReasons.Count - 3} mặt hàng khác";

            MessageBox.Show(
                $"Không thể cân bằng kho vì còn {missingReasons.Count} mặt hàng bị thiếu chưa được phân loại nguyên nhân hao hụt:\n\n• {sampleList}\n\nVui lòng chọn dòng sản phẩm và bấm 'Lý do lệch' trước khi cân bằng kho!",
                "Yêu cầu chọn nguyên nhân hao hụt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // 2. Xác nhận an toàn
        var confirmResult = MessageBox.Show(
            $"⚠️ XÁC NHẬN CÂN BẰNG TỒN KHO HỆ THỐNG\n\n" +
            $"• Tổng SKU kiểm đếm: {lblTotalSkuVal.Text}\n" +
            $"• Số SKU chênh lệch: {lblDiffSkuVal.Text}\n" +
            $"• Tổng giá trị thất thoát: {lblLossAmountVal.Text}\n\n" +
            $"Hệ thống sẽ cập nhật số tồn thực tế vào CSDL và tự động ghi log biến động kho (StockHistory). Hành động này không thể hoàn tác!\n\n" +
            $"Bạn có đồng ý tiến hành?",
            "Xác nhận Cân Bằng Kho",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmResult != DialogResult.Yes) return;

        try
        {
            await EnsureAuthHeaderAsync();
            var req = new ReconcileStockAuditRequest
            {
                ReconciledBy = CurrentSession.FullName,
                Notes = $"Cân bằng kho bởi {CurrentSession.FullName} ({CurrentSession.Role})"
            };

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit/{_currentAuditId}/reconcile", content);
            if (resp.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    "🎉 ĐÃ CÂN BẰNG KHO THÀNH CÔNG!\n\nSố tồn hệ thống đã được đồng bộ chuẩn xác với thực tế kiểm đếm trên kệ.\nLịch sử biến động đã được ghi nhận vào hệ thống.",
                    "Cân bằng kho hoàn tất",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await ReloadCurrentAuditAsync();
            }
            else
            {
                var errStr = await resp.Content.ReadAsStringAsync();
                AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi cân bằng: {errStr}");
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi kết nối: {ex.Message}");
        }
    }

    private async Task OpenCreateNewAuditDialogAsync()
    {
        using var dlg = new CreateStockAuditForm(CurrentSession.FullName);
        if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
        {
            await CreateNewAuditSessionAsync(dlg.CreatedBy, dlg.Zone, dlg.Notes);
        }
    }

    private async Task OpenHistoryDialogAsync()
    {
        using var dlg = new AuditHistoryDialog(_httpClient);
        if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK && dlg.SelectedAuditId.HasValue)
        {
            await LoadAuditDetailsAsync(dlg.SelectedAuditId.Value);
            AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã nạp phiếu kiểm kê #{dlg.SelectedAuditId.Value.ToString()[..8].ToUpper()}");
        }
    }

    private async Task CreateNewAuditSessionAsync(string createdBy, string zone, string notes)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var req = new CreateStockAuditRequest
            {
                CreatedBy = createdBy,
                Zone = zone,
                Notes = notes
            };

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync($"{AppTheme.ApiBaseUrl}/api/stock-audit", content);
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                var newAudit = JsonSerializer.Deserialize<StockAuditDTO>(respStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (newAudit != null)
                {
                    _currentAuditId = newAudit.Id;
                    await LoadAuditDetailsAsync(newAudit.Id);
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã khởi tạo đợt kiểm kê mới cho: {zone}");
                }
            }
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Không thể khởi tạo đợt kiểm kê: {ex.Message}");
        }
    }

    private async Task ReloadCurrentAuditAsync()
    {
        if (_currentAuditId != null)
        {
            await LoadAuditDetailsAsync(_currentAuditId.Value);
            AntdUI.Message.info(this.FindForm() ?? new Form(), "Đã làm mới dữ liệu kiểm kê");
        }
        else
        {
            await LoadLatestOrCreateAuditAsync();
        }
    }

    private void ExportAuditToExcel()
    {
        if (_currentAuditData != null)
        {
            ExcelExportService.ExportStockAuditReportToExcel(_currentAuditData);
        }
        else
        {
            string auditCode = _currentAuditId != null ? _currentAuditId.Value.ToString()[..8].ToUpper() : "ALL";
            string title = $"BIÊN BẢN KIỂM KÊ KHO #{auditCode}";
            ExcelExportService.ExportDataGridViewToExcel(dgvAudit, title, $"BienBanKiemKe_{auditCode}");
        }
    }

    private void ExportAuditToPdf()
    {
        if (_currentAuditData != null)
        {
            StockAuditPdfService.GenerateStockAuditPdf(_currentAuditData);
        }
        else
        {
            MessageBox.Show(this, "Chưa có dữ liệu phiên kiểm kê để in biên bản PDF!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
