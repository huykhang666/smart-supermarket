using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Desktop.Forms;

namespace Desktop.Views;

public class StaffShiftView : UserControl
{
    private Label lblShiftStatus = null!;
    private Panel pnlShiftBadge = null!;
    private Label lblShiftTime = null!;
    private Label lblWorkstation = null!;
    private NumericUpDown numInitialCash = null!;
    private Label lblTotalOrdersVal = null!;
    private Label lblTotalRevenueVal = null!;
    private Label lblCashAmountVal = null!;
    private Label lblCashPercentVal = null!;
    private Label lblQrAmountVal = null!;
    private Label lblQrPercentVal = null!;
    private Label lblCardAmountVal = null!;
    private Label lblCardPercentVal = null!;
    private Label lblExpectedCashVal = null!;
    private NumericUpDown numActualCash = null!;
    private Label lblCashDifference = null!;
    private Panel pnlDiscrepancyBox = null!;
    private ComboBox cbDiscrepancyReason = null!;
    private TextBox txtDiscrepancyNote = null!;
    private Button btnOpenShift = null!;
    private Button btnCloseShift = null!;
    private Button btnPrintReport = null!;
    private Button btnRefreshData = null!;
    private DataGridView _dgvOrdersInShift = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    private int _currentSessionId = 0;
    private bool _isShiftOpen = false;
    private decimal _cashSales = 0;
    private decimal _qrSales = 0;
    private decimal _cardSales = 0;
    private decimal _totalSales = 0;
    private int _ordersCount = 0;
    private DateTime _shiftStartTime = DateTime.Now;

    public StaffShiftView()
    {
        InitializeComponent();
        this.VisibleChanged += async (s, e) =>
        {
            if (this.Visible)
            {
                await LoadCurrentSessionAsync();
            }
        };
        _ = LoadCurrentSessionAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(20);
        this.AutoScroll = true;

        // --- 1. Header Bar ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "👤 Quản Lý Ca Trực & Kiểm Đếm Két Tiền (Cash Balancing)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Kiểm soát tiền mặt đầu ca, giám sát doanh thu realtime, đối soát thừa thiếu và bàn giao két thu ngân",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        btnRefreshData = new Button
        {
            Text = "🔄 Làm Mới Số Liệu",
            Size = new Size(160, 34),
            Location = new Point(this.Width - 190, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        AppTheme.ApplySecondaryButton(btnRefreshData, radius: 6);
        btnRefreshData.Click += async (s, e) => await LoadCurrentSessionAsync();

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);
        pnlHeader.Controls.Add(btnRefreshData);

        // --- 2. Main 2-Column Grid ---
        var pnlMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // ==========================================
        // CỘT TRÁI: KIỂM KÉT & THAO TÁC CA TRỰC
        // ==========================================
        var pnlLeftCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(20),
            Margin = new Padding(0, 8, 10, 0),
            AutoScroll = true
        };
        AppTheme.ApplyCardPanel(pnlLeftCard);

        var lblLeftTitle = new Label
        {
            Text = "🕒 THÔNG TIN CA & KIỂM ĐẾM KÉT TIỀN",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 14),
            AutoSize = true
        };

        pnlShiftBadge = new Panel
        {
            Location = new Point(16, 46),
            Size = new Size(240, 32),
            BackColor = AppTheme.DangerSubtle
        };
        lblShiftStatus = new Label
        {
            Text = "🔴 Trạng thái: CHƯA MỞ CA",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Danger,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlShiftBadge.Controls.Add(lblShiftStatus);

        lblWorkstation = new Label
        {
            Text = $"🖥️ Trạm POS: {Environment.MachineName}  |  Thu ngân: {CurrentSession.FullName}",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Primary,
            Location = new Point(16, 88),
            AutoSize = true
        };

        lblShiftTime = new Label
        {
            Text = "Thời gian ca: Chưa ghi nhận phiên làm việc",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 112),
            AutoSize = true
        };

        var divider1 = new Panel { Location = new Point(16, 138), Size = new Size(pnlLeftCard.Width - 32, 1), BackColor = AppTheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        // 1. Tiền mặt đầu ca
        var lblInitCashTitle = new Label
        {
            Text = "💵 Tiền Mặt Đầu Ca Bàn Giao (Floating Cash):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 148),
            AutoSize = true
        };

        numInitialCash = new NumericUpDown
        {
            Location = new Point(16, 174),
            Size = new Size(320, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 100000000,
            Minimum = 0,
            Value = 2000000, // 2 triệu tiền mặt tiền lẻ mặc định
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
        numInitialCash.ValueChanged += (s, e) => RecalculateDifference();

        // 2. Tiền mặt thực tế đếm cuối ca
        var lblActualCashTitle = new Label
        {
            Text = "💰 Tiền Mặt Kiểm Đếm Thực Tế Cuối Ca Nộp Lại:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 220),
            AutoSize = true
        };

        numActualCash = new NumericUpDown
        {
            Location = new Point(16, 246),
            Size = new Size(320, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 500000000,
            Minimum = 0,
            Value = 2000000,
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
        numActualCash.ValueChanged += (s, e) => RecalculateDifference();

        // 3. Kết quả đối soát chênh lệch
        var lblDiffTitle = new Label
        {
            Text = "Kết quả đối soát tiền mặt (Thực tế vs Sổ sách ca):",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 290),
            AutoSize = true
        };

        lblCashDifference = new Label
        {
            Text = "0 VNĐ (Khớp tiền bàn giao)",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = AppTheme.Success,
            Location = new Point(16, 314),
            AutoSize = true
        };

        // 4. Khối bắt buộc giải trình khi chênh lệch != 0
        pnlDiscrepancyBox = new Panel
        {
            Location = new Point(16, 348),
            Size = new Size(pnlLeftCard.Width - 32, 130),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(255, 245, 245),
            Padding = new Padding(12),
            Visible = false
        };
        pnlDiscrepancyBox.Paint += (s, e) =>
        {
            using var p = new Pen(Color.FromArgb(239, 68, 68), 1);
            e.Graphics.DrawRectangle(p, 0, 0, pnlDiscrepancyBox.Width - 1, pnlDiscrepancyBox.Height - 1);
        };

        var lblDiscWarn = new Label
        {
            Text = "⚠️ BẮT BUỘC GIẢI TRÌNH CHÊNH LỆCH KÉT TIỀN:",
            Font = AppTheme.FontCaptionBold,
            ForeColor = Color.FromArgb(185, 28, 28),
            Location = new Point(10, 8),
            AutoSize = true
        };

        cbDiscrepancyReason = new ComboBox
        {
            Location = new Point(10, 30),
            Size = new Size(pnlDiscrepancyBox.Width - 20, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontBody
        };
        cbDiscrepancyReason.Items.AddRange(new object[]
        {
            "Làm tròn tiền lẻ trả khách (bớt lẻ dưới 1.000đ)",
            "Khách hàng quên nhận tiền thối",
            "Thu ngân thối nhầm mệnh giá tiền mặt",
            "Bán hàng không xuất hóa đơn / Nhập sót",
            "Hao hụt tiền mặt chưa rõ nguyên nhân (chờ rà soát lại camera)",
            "Lý do khác"
        });
        cbDiscrepancyReason.SelectedIndex = 0;

        txtDiscrepancyNote = new TextBox
        {
            Location = new Point(10, 64),
            Size = new Size(pnlDiscrepancyBox.Width - 20, 54),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Multiline = true,
            Font = AppTheme.FontCaption,
            PlaceholderText = "Nhập nội dung giải trình chi tiết về khoản chênh lệch..."
        };

        pnlDiscrepancyBox.Controls.Add(lblDiscWarn);
        pnlDiscrepancyBox.Controls.Add(cbDiscrepancyReason);
        pnlDiscrepancyBox.Controls.Add(txtDiscrepancyNote);

        // 2. Hướng dẫn quy trình chốt ca
        var pnlShiftHint = new Panel
        {
            Location = new Point(16, 222),
            Size = new Size(pnlLeftCard.Width - 32, 72),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(12)
        };
        pnlShiftHint.Paint += (s, e) =>
        {
            using var p = new Pen(AppTheme.BorderLight, 1);
            e.Graphics.DrawRectangle(p, 0, 0, pnlShiftHint.Width - 1, pnlShiftHint.Height - 1);
        };
        var lblShiftHint = new Label
        {
            Text = "📋 Quy trình chốt ca:\nKhi kết thúc ca làm việc, bấm 'Chốt & Đóng Ca' bên dưới. Hệ thống sẽ mở bảng kiểm đếm két tiền, đối soát chênh lệch, checklist đơn hàng và xác thực PIN quản lý.",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Fill
        };
        pnlShiftHint.Controls.Add(lblShiftHint);

        // 3. Nút điều khiển ca trực
        var pnlButtons = new FlowLayoutPanel
        {
            Location = new Point(16, 310),
            Size = new Size(pnlLeftCard.Width - 32, 90),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        btnOpenShift = new Button
        {
            Text = "🔓 Mở Ca Trực Mới",
            Size = new Size(180, 38),
            Margin = new Padding(0, 0, 10, 8),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnOpenShift, radius: 6);
        btnOpenShift.Click += async (s, e) => await OpenShiftAsync();

        btnCloseShift = new Button
        {
            Text = "🔒 Chốt & Đóng Ca",
            Size = new Size(180, 38),
            Margin = new Padding(0, 0, 10, 8),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyDangerButton(btnCloseShift, radius: 6);
        btnCloseShift.Click += async (s, e) => await CloseShiftAsync();

        btnPrintReport = new Button
        {
            Text = "🖨️ In Báo Cáo Bàn Giao Ca (Z-Report)",
            Size = new Size(370, 38),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplySecondaryButton(btnPrintReport, radius: 6);
        btnPrintReport.Click += (s, e) => PrintZReport();

        pnlButtons.Controls.Add(btnOpenShift);
        pnlButtons.Controls.Add(btnCloseShift);
        pnlButtons.Controls.Add(btnPrintReport);

        pnlLeftCard.Controls.Add(lblLeftTitle);
        pnlLeftCard.Controls.Add(pnlShiftBadge);
        pnlLeftCard.Controls.Add(lblWorkstation);
        pnlLeftCard.Controls.Add(lblShiftTime);
        pnlLeftCard.Controls.Add(divider1);
        pnlLeftCard.Controls.Add(lblInitCashTitle);
        pnlLeftCard.Controls.Add(numInitialCash);
        pnlLeftCard.Controls.Add(pnlShiftHint);
        pnlLeftCard.Controls.Add(pnlButtons);

        // ==========================================
        // CỘT PHẢI: BÁO CÁO DOANH THU & PHÂN BỔ PHƯƠNG THỨC
        // ==========================================
        var pnlRightCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(20),
            Margin = new Padding(10, 8, 0, 0),
            AutoScroll = true
        };
        AppTheme.ApplyCardPanel(pnlRightCard);

        var lblRightTitle = new Label
        {
            Text = "📊 DOANH THU CA & PHÂN BỔ THANH TOÁN REALTIME",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 14),
            AutoSize = true
        };

        var lblOrdersTitle = new Label { Text = "Tổng số hóa đơn đã xuất:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(16, 55), AutoSize = true };
        lblTotalOrdersVal = new Label { Text = "0 hóa đơn", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(230, 55), AutoSize = true };

        var lblRevTitle = new Label { Text = "Tổng doanh thu ca trực:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(16, 88), AutoSize = true };
        lblTotalRevenueVal = new Label { Text = "0 VNĐ", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = AppTheme.Primary, Location = new Point(228, 85), AutoSize = true };

        var divider2 = new Panel { Location = new Point(16, 122), Size = new Size(pnlRightCard.Width - 32, 1), BackColor = AppTheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        // 3 dòng phân bổ thanh toán
        var pnlPayCash = CreatePaymentStatRow("💵 Tiền Mặt (Cash - Nộp Két)", out lblCashAmountVal, out lblCashPercentVal, AppTheme.Success, 138);
        var pnlPayQr = CreatePaymentStatRow("📱 Quét Mã VietQR Chuyển Khoản", out lblQrAmountVal, out lblQrPercentVal, AppTheme.Primary, 208);
        var pnlPayCard = CreatePaymentStatRow("💳 Quẹt Thẻ Ngân Hàng POS", out lblCardAmountVal, out lblCardPercentVal, AppTheme.Warning, 278);

        // Tiền mặt sổ sách kỳ vọng
        var pnlExpectedCard = new Panel
        {
            Location = new Point(16, 358),
            Size = new Size(pnlRightCard.Width - 32, 75),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(240, 249, 255),
            Padding = new Padding(14)
        };
        pnlExpectedCard.Paint += (s, e) =>
        {
            using var p = new Pen(AppTheme.Primary, 1);
            e.Graphics.DrawRectangle(p, 0, 0, pnlExpectedCard.Width - 1, pnlExpectedCard.Height - 1);
        };

        var lblExpTitle = new Label
        {
            Text = "📌 TIỀN MẶT SỔ SÁCH KỲ VỌNG TRONG KÉT (Đầu ca + Doanh thu mặt):",
            Font = AppTheme.FontCaptionBold,
            ForeColor = AppTheme.Primary,
            Location = new Point(12, 10),
            AutoSize = true
        };

        lblExpectedCashVal = new Label
        {
            Text = "2,000,000 VNĐ",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            Location = new Point(12, 34),
            AutoSize = true
        };
        pnlExpectedCard.Controls.Add(lblExpTitle);
        pnlExpectedCard.Controls.Add(lblExpectedCashVal);

        pnlRightCard.Controls.Add(lblRightTitle);
        pnlRightCard.Controls.Add(lblOrdersTitle);
        pnlRightCard.Controls.Add(lblTotalOrdersVal);
        pnlRightCard.Controls.Add(lblRevTitle);
        pnlRightCard.Controls.Add(lblTotalRevenueVal);
        pnlRightCard.Controls.Add(divider2);
        pnlRightCard.Controls.Add(pnlPayCash);
        pnlRightCard.Controls.Add(pnlPayQr);
        pnlRightCard.Controls.Add(pnlPayCard);
        pnlRightCard.Controls.Add(pnlExpectedCard);

        var lblOrdersGridTitle = new Label
        {
            Text = "📋 ĐƠN HÀNG ĐÃ BÁN TRONG CA TRỰC",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 445),
            AutoSize = true
        };

        _dgvOrdersInShift = new DataGridView
        {
            Location = new Point(16, 475),
            Size = new Size(pnlRightCard.Width - 32, 220),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false
        };
        _dgvOrdersInShift.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _dgvOrdersInShift.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
        _dgvOrdersInShift.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _dgvOrdersInShift.ColumnHeadersHeight = 32;
        _dgvOrdersInShift.RowTemplate.Height = 28;

        _dgvOrdersInShift.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã đơn", DataPropertyName = "OrderNumber", FillWeight = 25 });
        _dgvOrdersInShift.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giờ tạo", DataPropertyName = "CreatedAt", FillWeight = 20 });
        _dgvOrdersInShift.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thanh toán", DataPropertyName = "PaymentMethod", FillWeight = 25 });
        _dgvOrdersInShift.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng tiền", DataPropertyName = "TotalAmount", FillWeight = 30 });

        pnlRightCard.Controls.Add(lblOrdersGridTitle);
        pnlRightCard.Controls.Add(_dgvOrdersInShift);

        pnlMain.Controls.Add(pnlLeftCard, 0, 0);
        pnlMain.Controls.Add(pnlRightCard, 1, 0);

        this.Controls.Add(pnlMain);
        this.Controls.Add(pnlHeader);
    }

    private Panel CreatePaymentStatRow(string title, out Label lblAmount, out Label lblPercent, Color accent, int y)
    {
        var p = new Panel
        {
            Location = new Point(16, y),
            Size = new Size(420, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = AppTheme.BackgroundGray
        };
        p.Paint += (s, e) =>
        {
            using var b = new SolidBrush(accent);
            e.Graphics.FillRectangle(b, 0, 0, 4, p.Height);
        };

        var lblT = new Label { Text = title, Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(14, 8), AutoSize = true };
        lblAmount = new Label { Text = "0 VNĐ", Font = AppTheme.FontBodyBold, ForeColor = accent, Location = new Point(14, 32), AutoSize = true };
        lblPercent = new Label { Text = "0.0%", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(320, 20), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };

        p.Controls.Add(lblT);
        p.Controls.Add(lblAmount);
        p.Controls.Add(lblPercent);
        return p;
    }

    private async Task LoadCurrentSessionAsync()
    {
        btnRefreshData.Enabled = false;
        try
        {
            int empId = CurrentSession.UserId;
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/shifts/sessions/current?employeeId={empId}&branchId=1");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    _currentSessionId = data.GetProperty("sessionId").GetInt32();
                    CurrentSession.ShiftSessionId = _currentSessionId;
                    _isShiftOpen = true;
                    _shiftStartTime = data.GetProperty("startTime").GetDateTime().ToLocalTime();
                    decimal initCash = data.GetProperty("initialCash").GetDecimal();
                    _ordersCount = data.GetProperty("totalOrdersCount").GetInt32();
                    _cashSales = data.GetProperty("cashSalesAmount").GetDecimal();
                    _qrSales = data.GetProperty("qrSalesAmount").GetDecimal();
                    _cardSales = data.GetProperty("cardSalesAmount").GetDecimal();
                    _totalSales = data.GetProperty("totalRevenue").GetDecimal();

                    numInitialCash.Value = initCash;
                    numInitialCash.Enabled = false; // Đã mở ca thì khóa tiền đầu ca
                    btnOpenShift.Enabled = false;
                    btnCloseShift.Enabled = true;

                    lblShiftStatus.Text = "🟢 Trạng thái: ĐANG MỞ CA";
                    pnlShiftBadge.BackColor = AppTheme.SuccessSubtle;
                    lblShiftStatus.ForeColor = AppTheme.Success;
                    lblShiftTime.Text = $"Bắt đầu lúc: {_shiftStartTime:dd/MM/yyyy HH:mm:ss}";

                    UpdateFinancialUI();
                    await LoadSessionOrdersAsync(_currentSessionId);
                    return;
                }
            }

            // Chưa mở ca hoặc phiên đã đóng
            ResetShiftFormState();
        }
        catch
        {
            // Fallback demo mode
        }
        finally
        {
            btnRefreshData.Enabled = true;
        }
    }

    private async Task LoadSessionOrdersAsync(int sessionId)
    {
        try
        {
            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/shifts/sessions/{sessionId}/orders");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var dataEl = root.TryGetProperty("data", out var d) ? d : root;

                if (dataEl.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<ShiftOrderViewModel>();
                    foreach (var item in dataEl.EnumerateArray())
                    {
                        string code = item.TryGetProperty("orderCode", out var c) ? (c.GetString() ?? "") : "";
                        string cust = item.TryGetProperty("customerName", out var cn) ? (cn.GetString() ?? "") : "Khách Lẻ";
                        string payMethod = item.TryGetProperty("paymentMethodName", out var pm) ? (pm.GetString() ?? "") : "💵 Tiền mặt";
                        string status = item.TryGetProperty("status", out var st) ? (st.GetString() ?? "") : "Hoàn Thành";
                        decimal amount = item.TryGetProperty("finalAmount", out var fa) ? fa.GetDecimal() : (item.TryGetProperty("totalAmount", out var ta) ? ta.GetDecimal() : 0);
                        DateTime dt = item.TryGetProperty("orderDate", out var od) ? od.GetDateTime().ToLocalTime() : DateTime.Now;

                        list.Add(new ShiftOrderViewModel
                        {
                            OrderNumber = code,
                            CreatedAt = dt.ToString("HH:mm:ss"),
                            CustomerName = cust,
                            PaymentMethod = payMethod,
                            TotalAmount = $"{amount:N0} đ",
                            Status = status
                        });
                    }

                    if (list.Count == 0 && DataStore.Orders.Count > 0)
                    {
                        var todayLocal = DataStore.Orders
                            .Where(o => o.OrderDate.Date == DateTime.Today)
                            .OrderByDescending(o => o.OrderDate)
                            .ToList();

                        if (todayLocal.Count > 0)
                        {
                            decimal lCash = 0, lQr = 0, lCard = 0;
                            foreach (var o in todayLocal)
                            {
                                string pMethod = o.PaymentMethod;
                                string icon = pMethod.Contains("mặt", StringComparison.OrdinalIgnoreCase) ? "💵 Tiền mặt"
                                    : pMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase) ? "📱 VietQR"
                                    : "💳 Thẻ POS";

                                if (pMethod.Contains("mặt", StringComparison.OrdinalIgnoreCase)) lCash += o.GrandTotal;
                                else if (pMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase)) lQr += o.GrandTotal;
                                else lCard += o.GrandTotal;

                                list.Add(new ShiftOrderViewModel
                                {
                                    OrderNumber = string.IsNullOrEmpty(o.OrderCode) ? $"ORD-{o.OrderId:D5}" : o.OrderCode,
                                    CreatedAt = o.OrderDate.ToString("HH:mm:ss"),
                                    CustomerName = o.CustomerName,
                                    PaymentMethod = icon,
                                    TotalAmount = $"{o.GrandTotal:N0} đ",
                                    Status = "Hoàn Thành"
                                });
                            }

                            if (_ordersCount == 0)
                            {
                                _cashSales = lCash;
                                _qrSales = lQr;
                                _cardSales = lCard;
                                _totalSales = lCash + lQr + lCard;
                                _ordersCount = list.Count;
                                UpdateFinancialUI();
                            }
                        }
                    }

                    if (_dgvOrdersInShift != null)
                    {
                        _dgvOrdersInShift.DataSource = list;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading orders for shift view: {ex.Message}");
        }
    }

    private void ResetShiftFormState()
    {
        _currentSessionId = 0;
        CurrentSession.ShiftSessionId = null;
        _isShiftOpen = false;
        numInitialCash.Value = 0;
        numInitialCash.Enabled = true;
        numActualCash.Value = 0;
        btnOpenShift.Enabled = true;
        btnCloseShift.Enabled = false;
        lblShiftStatus.Text = "🔴 Trạng thái: CHƯA MỞ CA";
        pnlShiftBadge.BackColor = AppTheme.DangerSubtle;
        lblShiftStatus.ForeColor = AppTheme.Danger;
        lblShiftTime.Text = "Thời gian ca: Vui lòng nhập tiền đầu ca và bấm Mở Ca Trực";
        pnlDiscrepancyBox.Visible = false;
        lblCashDifference.Text = "0 VNĐ (Khớp tiền bàn giao)";
        lblCashDifference.ForeColor = AppTheme.Success;

        _cashSales = 0;
        _qrSales = 0;
        _cardSales = 0;
        _totalSales = 0;
        _ordersCount = 0;
        UpdateFinancialUI();

        if (_dgvOrdersInShift != null)
        {
            _dgvOrdersInShift.DataSource = null;
        }
    }

    private void UpdateFinancialUI()
    {
        lblTotalOrdersVal.Text = $"{_ordersCount} hóa đơn";
        lblTotalRevenueVal.Text = $"{_totalSales:N0} VNĐ";

        decimal pctCash = _totalSales > 0 ? (_cashSales / _totalSales) * 100m : 0;
        decimal pctQr = _totalSales > 0 ? (_qrSales / _totalSales) * 100m : 0;
        decimal pctCard = _totalSales > 0 ? (_cardSales / _totalSales) * 100m : 0;

        lblCashAmountVal.Text = $"{_cashSales:N0} VNĐ";
        lblCashPercentVal.Text = $"{pctCash:F1}%";

        lblQrAmountVal.Text = $"{_qrSales:N0} VNĐ";
        lblQrPercentVal.Text = $"{pctQr:F1}%";

        lblCardAmountVal.Text = $"{_cardSales:N0} VNĐ";
        lblCardPercentVal.Text = $"{pctCard:F1}%";

        decimal expectedCash = numInitialCash.Value + _cashSales;
        lblExpectedCashVal.Text = $"{expectedCash:N0} VNĐ";

        RecalculateDifference();
    }

    private void RecalculateDifference()
    {
        decimal expectedCash = numInitialCash.Value + _cashSales;
        decimal actualCash = numActualCash.Value;
        decimal diff = actualCash - expectedCash;

        if (diff == 0)
        {
            lblCashDifference.Text = "0 VNĐ (Khớp tiền bàn giao 100%)";
            lblCashDifference.ForeColor = AppTheme.Success;
            pnlDiscrepancyBox.Visible = false;
        }
        else if (diff > 0)
        {
            lblCashDifference.Text = $"+{diff:N0} VNĐ (⚠️ THỪA TIỀN KÉT)";
            lblCashDifference.ForeColor = AppTheme.Success;
            pnlDiscrepancyBox.Visible = true;
        }
        else
        {
            lblCashDifference.Text = $"-{Math.Abs(diff):N0} VNĐ (⚠️ THIẾU HỤT TIỀN KÉT)";
            lblCashDifference.ForeColor = AppTheme.Danger;
            pnlDiscrepancyBox.Visible = true;
        }
    }

    private async Task OpenShiftAsync()
    {
        if (numInitialCash.Value < 0)
        {
            MessageBox.Show("Tiền mặt đầu ca không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var payload = new
        {
            employeeId = CurrentSession.UserId,
            branchId = 1,
            workstationName = Environment.MachineName,
            initialCash = numInitialCash.Value
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/shifts/sessions/open", content);
            if (res.IsSuccessStatusCode)
            {
                AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã ghi nhận MỞ CA TRỰC mới thành công!");
                await LoadCurrentSessionAsync();
            }
            else
            {
                var body = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể mở ca trực: {body}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối máy chủ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CloseShiftAsync()
    {
        if (_currentSessionId <= 0)
        {
            MessageBox.Show("Hiện không có ca trực nào đang mở!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Tải dữ liệu tài chính mới nhất trước khi mở dialog chốt ca
        await LoadCurrentSessionAsync();

        // Mở popup đối soát doanh thu & kiểm đếm chốt ca chuyên biệt
        using var dlg = new ShiftCloseSummaryDialog(
            _currentSessionId,
            CurrentSession.FullName,
            Environment.MachineName,
            numInitialCash.Value,
            _cashSales,
            _qrSales,
            _cardSales,
            _ordersCount
        );

        if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK && dlg.IsClosedSuccessfully)
        {
            numActualCash.Value = dlg.FinalActualCash;
            PrintZReport();
            ResetShiftFormState();
            await LoadCurrentSessionAsync();
        }
    }

    private void PrintZReport()
    {
        decimal expectedCash = numInitialCash.Value + _cashSales;
        decimal actualCash = numActualCash.Value;
        decimal diff = actualCash - expectedCash;
        string diffStr = diff == 0 ? "0 VNĐ (KHỚP TIỀN BÀN GIAO)" : (diff > 0 ? $"+{diff:N0} VNĐ (THỪA TIỀN)" : $"-{Math.Abs(diff):N0} VNĐ (THIẾU TIỀN)");

        string report = 
            "=================== BÁO CÁO KẾT CA BÁN HÀNG (Z-REPORT) ===================\n" +
            "HỆ THỐNG SIÊU THỊ THÔNG MINH SMART SUPERMARKET\n" +
            $"Trạm POS: {Environment.MachineName}  |  Mã phiên ca: #{_currentSessionId}\n" +
            $"Thu ngân: {CurrentSession.FullName} (Mã NV: NV-{CurrentSession.UserId:D4})\n" +
            $"Thời gian bắt đầu: {_shiftStartTime:dd/MM/yyyy HH:mm:ss}\n" +
            $"Thời gian kết ca: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
            "--------------------------------------------------------------------------\n" +
            $"1. Tổng số hóa đơn đã xuất:   {_ordersCount} hóa đơn\n" +
            $"2. Tiền mặt đầu ca (Float):    {numInitialCash.Value:N0} VNĐ\n" +
            $"3. Doanh thu Tiền mặt (Cash):  {_cashSales:N0} VNĐ\n" +
            $"4. Doanh thu VietQR:           {_qrSales:N0} VNĐ\n" +
            $"5. Doanh thu Thẻ POS:          {_cardSales:N0} VNĐ\n" +
            "--------------------------------------------------------------------------\n" +
            $"TỔNG DOANH THU CA:             {_totalSales:N0} VNĐ\n" +
            $"TIỀN MẶT SỔ SÁCH KỲ VỌNG:      {expectedCash:N0} VNĐ\n" +
            $"TIỀN MẶT THỰC TẾ KIỂM ĐẾM:     {actualCash:N0} VNĐ\n" +
            $"CHÊNH LỆCH KÉT TIỀN:           {diffStr}\n" +
            (diff != 0 ? $"Lý do giải trình:              {cbDiscrepancyReason.SelectedItem} - {txtDiscrepancyNote.Text}\n" : "") +
            "==========================================================================\n" +
            "KÝ TÊN BÀN GIAO:\n\n" +
            "   [Thu ngân ca trực]                       [Quản lý duyệt chốt ca]\n" +
            $"   {CurrentSession.FullName}                                   Đã duyệt qua PIN\n";

        MessageBox.Show(report, "In phiếu bàn giao ca (Z-Report)", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
