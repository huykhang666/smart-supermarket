using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop;

namespace Desktop.Forms
{
    public class ShiftCloseSummaryDialog : Form
    {
        private readonly int _sessionId;
        private readonly string _staffName;
        private readonly string _posTerminalId;
        private decimal _initialCash;
        private decimal _systemCashSales;
        private decimal _systemQrSales;
        private decimal _systemCardSales;
        private decimal _expectedCash;
        private int _orderCount;

        // UI Controls
        private Label _lblHeaderTitle;
        private Label _lblHeaderSubtitle;

        // KPI cards
        private Label _lblCashSalesVal;
        private Label _lblQrSalesVal;
        private Label _lblCardSalesVal;

        // Left Table
        private DataGridView _dgvOrders;
        private Label _lblOrderTableTitle;

        // Right Verification Form
        private Label _lblExpectedCashVal;
        private NumericUpDown _numActualCash;
        private Label _lblDifferenceVal;
        private Panel _pnlDiscrepancy;
        private ComboBox _cboReason;
        private TextBox _txtExplanation;
        private CheckBox _chkCompensation;
        private TextBox _txtManagerPin;
        private Button _btnSubmit;
        private Button _btnCancel;

        public decimal FinalActualCash { get; private set; }
        public decimal FinalDifference { get; private set; }
        public string DiscrepancyReason { get; private set; } = string.Empty;
        public string DiscrepancyExplanation { get; private set; } = string.Empty;
        public bool IsClosedSuccessfully { get; private set; } = false;

        public ShiftCloseSummaryDialog(
            int sessionId,
            string staffName,
            string posTerminalId,
            decimal initialCash,
            decimal cashSales,
            decimal qrSales,
            decimal cardSales,
            int orderCount)
        {
            _sessionId = sessionId;
            _staffName = staffName;
            _posTerminalId = posTerminalId;
            _initialCash = initialCash;
            _systemCashSales = cashSales;
            _systemQrSales = qrSales;
            _systemCardSales = cardSales;
            _orderCount = orderCount;
            _expectedCash = _initialCash + _systemCashSales;

            InitializeCustomComponents();
            _ = LoadOrdersAsync();
        }

        private void InitializeCustomComponents()
        {
            this.Text = $"Chốt ca & Đối soát doanh thu - Ca #{_sessionId}";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Main Container TableLayout (Top Header, Body 2 Columns, Bottom Bar)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F)); // Left: Orders & KPIs
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F)); // Right: Cash & Discrepancy
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));      // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // Content
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));      // Action Bar

            // 1. HEADER
            var pnlHeader = new Panel { Dock = DockStyle.Fill };
            _lblHeaderTitle = new Label
            {
                Text = $"📊 ĐỐI SOÁT DOANH THU & CHỐT ĐÓNG KÉT CA #{_sessionId}",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            _lblHeaderSubtitle = new Label
            {
                Text = $"Thu ngân: {_staffName}  |  Trạm POS: {_posTerminalId}  |  Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(0, 32)
            };
            pnlHeader.Controls.AddRange(new Control[] { _lblHeaderTitle, _lblHeaderSubtitle });
            mainLayout.SetColumnSpan(pnlHeader, 2);
            mainLayout.Controls.Add(pnlHeader, 0, 0);

            // 2. LEFT PANEL: KPI Cards + Orders Grid
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };
            
            // KPI Bar: 3 Cards (Cash, VietQR, Card)
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            var cardCash = CreateKpiCard("💵 TIỀN MẶT BÁN ĐƯỢC", $"{_systemCashSales:N0} đ", Color.FromArgb(16, 185, 129), out _lblCashSalesVal);
            var cardQr = CreateKpiCard("📱 CHUYỂN KHOẢN VIETQR", $"{_systemQrSales:N0} đ", Color.FromArgb(14, 165, 233), out _lblQrSalesVal);
            var cardPos = CreateKpiCard("💳 THẺ QUẸT POS", $"{_systemCardSales:N0} đ", Color.FromArgb(139, 92, 246), out _lblCardSalesVal);

            pnlKpis.Controls.Add(cardCash, 0, 0);
            pnlKpis.Controls.Add(cardQr, 1, 0);
            pnlKpis.Controls.Add(cardPos, 2, 0);

            // Order Table Header & Grid
            _lblOrderTableTitle = new Label
            {
                Text = $"📋 DANH SÁCH ĐƠN HÀNG TRONG CA ({_orderCount} đơn)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 28
            };

            _dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
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
            _dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            _dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            _dgvOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _dgvOrders.ColumnHeadersHeight = 36;
            _dgvOrders.RowTemplate.Height = 32;

            _dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã đơn", DataPropertyName = "OrderNumber", FillWeight = 25 });
            _dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giờ tạo", DataPropertyName = "CreatedAt", FillWeight = 18 });
            _dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thanh toán", DataPropertyName = "PaymentMethod", FillWeight = 22 });
            _dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng tiền", DataPropertyName = "TotalAmount", FillWeight = 23 });
            _dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "Status", FillWeight = 18 });

            pnlLeft.Controls.Add(_dgvOrders);
            pnlLeft.Controls.Add(_lblOrderTableTitle);
            pnlLeft.Controls.Add(pnlKpis);
            mainLayout.Controls.Add(pnlLeft, 0, 1);

            // 3. RIGHT PANEL: Cash Balancing & Discrepancy Form
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20),
                AutoScroll = true
            };
            pnlRight.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlRight.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            int y = 10;

            var lblSec1 = new Label
            {
                Text = "💵 KIỂM ĐẾM TIỀN MẶT THỰC TẾ",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(15, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblSec1);
            y += 30;

            // Info rows: Initial cash & System cash
            var pnlCalc = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(390, 80),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlCalc.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlCalc.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            var lblInit = new Label { Text = "Tiền mặt đầu ca (Floating):", Location = new Point(10, 8), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139) };
            var lblInitVal = new Label { Text = $"{_initialCash:N0} đ", Location = new Point(250, 8), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

            var lblSales = new Label { Text = "+ Tiền mặt bán được trong ca:", Location = new Point(10, 30), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139) };
            var lblSalesVal = new Label { Text = $"{_systemCashSales:N0} đ", Location = new Point(250, 30), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };

            var lblExpectedTitle = new Label { Text = "= Két sổ sách phải có:", Location = new Point(10, 52), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            _lblExpectedCashVal = new Label { Text = $"{_expectedCash:N0} đ", Location = new Point(250, 52), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) };

            pnlCalc.Controls.AddRange(new Control[] { lblInit, lblInitVal, lblSales, lblSalesVal, lblExpectedTitle, _lblExpectedCashVal });
            pnlRight.Controls.Add(pnlCalc);
            y += 95;

            // Actual Cash Input
            var lblInputTitle = new Label
            {
                Text = "Nhập tiền mặt thực tế nộp lại két:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(15, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblInputTitle);
            y += 24;

            _numActualCash = new NumericUpDown
            {
                Location = new Point(15, y),
                Size = new Size(390, 36),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Maximum = 1000000000,
                ThousandsSeparator = true,
                Increment = 10000,
                Value = _expectedCash // Mặc định hiển thị bằng tiền sổ sách
            };
            _numActualCash.ValueChanged += (s, e) => UpdateDifference();
            pnlRight.Controls.Add(_numActualCash);
            y += 45;

            // Difference display
            var pnlDiffBox = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(390, 40),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            var lblDiffText = new Label
            {
                Text = "Chênh lệch két:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            _lblDifferenceVal = new Label
            {
                Text = "0 đ (Khớp két hoàn hảo)",
                Location = new Point(140, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129)
            };
            pnlDiffBox.Controls.AddRange(new Control[] { lblDiffText, _lblDifferenceVal });
            pnlRight.Controls.Add(pnlDiffBox);
            y += 50;

            // Discrepancy Panel (Chỉ bật khi tiền thiếu âm)
            _pnlDiscrepancy = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(390, 180),
                BackColor = Color.FromArgb(254, 242, 242),
                Visible = false
            };
            _pnlDiscrepancy.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, _pnlDiscrepancy.ClientRectangle, Color.FromArgb(252, 165, 165), ButtonBorderStyle.Solid);
            };

            var lblDiscWarning = new Label
            {
                Text = "⚠️ CẢNH BÁO: KÉT BỊ ÂM (THIẾU TIỀN)!",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(10, 8),
                AutoSize = true
            };

            var lblReason = new Label { Text = "Lý do thiếu:", Location = new Point(10, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            _cboReason = new ComboBox
            {
                Location = new Point(10, 52),
                Size = new Size(370, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboReason.Items.AddRange(new object[] {
                "Thu nhầm tiền / Thối nhầm tiền thừa cho khách",
                "Quên ghi sổ chi quỹ tiền mặt khẩn cấp",
                "Thất thoát tiền mặt chưa rõ nguyên nhân",
                "Khác (Ghi rõ vào phần giải trình bên dưới)"
            });
            _cboReason.SelectedIndex = 0;

            _txtExplanation = new TextBox
            {
                Location = new Point(10, 85),
                Size = new Size(370, 48),
                Multiline = true,
                PlaceholderText = "Nhập giải trình chi tiết nguyên nhân thiếu tiền két..."
            };

            _chkCompensation = new CheckBox
            {
                Location = new Point(10, 142),
                Size = new Size(370, 30),
                Text = "Tôi xác nhận và CAM KẾT ĐỀN BÙ khoản tiền thiếu hụt này",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            };

            _pnlDiscrepancy.Controls.AddRange(new Control[] { lblDiscWarning, lblReason, _cboReason, _txtExplanation, _chkCompensation });
            pnlRight.Controls.Add(_pnlDiscrepancy);
            y += 190;

            // Manager PIN
            var lblPinTitle = new Label
            {
                Text = "Mã PIN Quản lý phê duyệt chốt ca (Mặc định: 123456):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(15, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblPinTitle);
            y += 22;

            _txtManagerPin = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(390, 30),
                UseSystemPasswordChar = true,
                Text = "123456" // Default manager PIN
            };
            pnlRight.Controls.Add(_txtManagerPin);

            mainLayout.Controls.Add(pnlRight, 1, 1);

            // 4. BOTTOM ACTION BAR
            var pnlActions = new Panel { Dock = DockStyle.Fill };
            _btnCancel = new Button
            {
                Text = "Quay lại",
                Size = new Size(130, 42),
                Location = new Point(mainLayout.Width - 330, 8),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => this.Close();

            _btnSubmit = new Button
            {
                Text = "🔒 HOÀN TẤT CHỐT CA & ĐÓNG KÉT",
                Size = new Size(280, 42),
                Location = new Point(mainLayout.Width - 180, 8),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            _btnSubmit.Click += async (s, e) => await SubmitShiftCloseAsync();

            pnlActions.Controls.AddRange(new Control[] { _btnCancel, _btnSubmit });
            pnlActions.Resize += (s, e) =>
            {
                _btnSubmit.Location = new Point(pnlActions.Width - _btnSubmit.Width - 8, 8);
                _btnCancel.Location = new Point(_btnSubmit.Left - _btnCancel.Width - 12, 8);
            };

            mainLayout.SetColumnSpan(pnlActions, 2);
            mainLayout.Controls.Add(pnlActions, 0, 2);

            this.Controls.Add(mainLayout);
            UpdateDifference();
        }

        private Panel CreateKpiCard(string title, string value, Color valueColor, out Label lblVal)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4)
            };
            pnl.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 10),
                AutoSize = true
            };
            lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = valueColor,
                Location = new Point(10, 34),
                AutoSize = true
            };
            pnl.Controls.AddRange(new Control[] { lblTitle, lblVal });
            return pnl;
        }

        private void UpdateDifference()
        {
            decimal actual = _numActualCash.Value;
            decimal diff = actual - _expectedCash;

            FinalActualCash = actual;
            FinalDifference = diff;

            if (diff == 0)
            {
                _lblDifferenceVal.Text = "0 đ (Khớp két chuẩn)";
                _lblDifferenceVal.ForeColor = Color.FromArgb(16, 185, 129);
                _pnlDiscrepancy.Visible = false;
            }
            else if (diff > 0)
            {
                _lblDifferenceVal.Text = $"+{diff:N0} đ (Thừa tiền két)";
                _lblDifferenceVal.ForeColor = Color.FromArgb(37, 99, 235);
                _pnlDiscrepancy.Visible = false;
            }
            else
            {
                // Âm tiền
                _lblDifferenceVal.Text = $"{diff:N0} đ (THIẾU TIỀN KÉT)";
                _lblDifferenceVal.ForeColor = Color.FromArgb(220, 38, 38);
                _pnlDiscrepancy.Visible = true;
            }
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri(AppTheme.ApiBaseUrl);

                // 1. Nạp chi tiết tài chính phiên ca trực từ Backend
                var sessRes = await client.GetAsync($"/api/v1/shifts/sessions/current?employeeId={CurrentSession.UserId}&branchId=1");
                if (sessRes.IsSuccessStatusCode)
                {
                    var sessJson = await sessRes.Content.ReadAsStringAsync();
                    using var sDoc = JsonDocument.Parse(sessJson);
                    var sRoot = sDoc.RootElement;
                    if (sRoot.TryGetProperty("data", out var sData))
                    {
                        _systemCashSales = sData.TryGetProperty("cashSalesAmount", out var cs) ? cs.GetDecimal() : 0;
                        _systemQrSales = sData.TryGetProperty("qrSalesAmount", out var qs) ? qs.GetDecimal() : 0;
                        _systemCardSales = sData.TryGetProperty("cardSalesAmount", out var cds) ? cds.GetDecimal() : 0;
                        _initialCash = sData.TryGetProperty("initialCash", out var ic) ? ic.GetDecimal() : _initialCash;
                        _expectedCash = _initialCash + _systemCashSales;

                        _lblCashSalesVal.Text = $"{_systemCashSales:N0} đ";
                        _lblQrSalesVal.Text = $"{_systemQrSales:N0} đ";
                        _lblCardSalesVal.Text = $"{_systemCardSales:N0} đ";
                        _lblExpectedCashVal.Text = $"{_expectedCash:N0} đ";
                        _numActualCash.Value = _expectedCash;
                        UpdateDifference();
                    }
                }

                // 2. Nạp danh sách đơn hàng đã bán trong ca
                var res = await client.GetAsync($"/api/v1/shifts/sessions/{_sessionId}/orders");
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
                            var todayLocalOrders = DataStore.Orders
                                .Where(o => o.OrderDate.Date == DateTime.Today)
                                .OrderByDescending(o => o.OrderDate)
                                .ToList();

                            if (todayLocalOrders.Count > 0)
                            {
                                decimal locCash = 0;
                                decimal locQr = 0;
                                decimal locCard = 0;

                                foreach (var o in todayLocalOrders)
                                {
                                    string pMethod = o.PaymentMethod;
                                    string icon = pMethod.Contains("mặt", StringComparison.OrdinalIgnoreCase) ? "💵 Tiền mặt"
                                        : pMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase) ? "📱 VietQR"
                                        : "💳 Thẻ POS";

                                    if (pMethod.Contains("mặt", StringComparison.OrdinalIgnoreCase)) locCash += o.GrandTotal;
                                    else if (pMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase)) locQr += o.GrandTotal;
                                    else locCard += o.GrandTotal;

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

                                if (_systemCashSales == 0 && _systemQrSales == 0 && _systemCardSales == 0)
                                {
                                    _systemCashSales = locCash;
                                    _systemQrSales = locQr;
                                    _systemCardSales = locCard;
                                    _expectedCash = _initialCash + _systemCashSales;

                                    _lblCashSalesVal.Text = $"{_systemCashSales:N0} đ";
                                    _lblQrSalesVal.Text = $"{_systemQrSales:N0} đ";
                                    _lblCardSalesVal.Text = $"{_systemCardSales:N0} đ";
                                    _lblExpectedCashVal.Text = $"{_expectedCash:N0} đ";
                                    _numActualCash.Value = _expectedCash;
                                    UpdateDifference();
                                }
                            }
                        }

                        _dgvOrders.DataSource = list;
                        _lblOrderTableTitle.Text = $"📋 DANH SÁCH ĐƠN HÀNG TRONG CA ({list.Count} đơn)";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session orders: {ex.Message}");
            }
        }

        private async Task SubmitShiftCloseAsync()
        {
            // Cảnh báo nếu ca trực chưa ghi nhận đơn hàng nào nhưng lại nộp tiền
            if (_dgvOrders.Rows.Count == 0 && _expectedCash == 0 && FinalActualCash > 0)
            {
                var askZero = MessageBox.Show(
                    $"⚠️ CẢNH BÁO ĐƠN HÀNG TRỐNG:\n\n" +
                    $"• Hệ thống ghi nhận 0 đơn hàng và doanh thu = 0 đ.\n" +
                    $"• Tuy nhiên bạn đang nhập số tiền nộp két là: {FinalActualCash:N0} đ.\n\n" +
                    $"Bạn có chắc chắn muốn chốt ca với 0 đơn hàng không?\n(Bấm 'No' để kiểm tra lại các đơn hàng vừa thanh toán tại POS).",
                    "Xác nhận đối soát đơn hàng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (askZero != DialogResult.Yes)
                {
                    return;
                }
            }

            // 1. Kiểm tra PIN quản lý
            if (string.IsNullOrWhiteSpace(_txtManagerPin.Text))
            {
                MessageBox.Show("Vui lòng nhập mã PIN Quản lý để phê duyệt chốt ca!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtManagerPin.Focus();
                return;
            }

            // PIN quản lý hợp lệ (123456 hoặc 999999)
            if (_txtManagerPin.Text.Trim() != "123456" && _txtManagerPin.Text.Trim() != "999999")
            {
                MessageBox.Show("Mã PIN Quản lý không chính xác! Không thể duyệt chốt ca.", "Từ chối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _txtManagerPin.SelectAll();
                _txtManagerPin.Focus();
                return;
            }

            // 2. Kiểm tra nếu két bị âm
            if (FinalDifference < 0)
            {
                if (!_chkCompensation.Checked)
                {
                    MessageBox.Show("Két tiền bị âm (thiếu tiền mặt)!\nNhân viên thu ngân bắt buộc phải tích chọn cam kết đền bù để có thể chốt ca.",
                        "Yêu cầu cam kết đền bù", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _chkCompensation.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_txtExplanation.Text))
                {
                    MessageBox.Show("Vui lòng nhập nội dung giải trình chi tiết nguyên nhân két tiền bị thiếu!",
                        "Thiếu thông tin giải trình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtExplanation.Focus();
                    return;
                }

                DiscrepancyReason = _cboReason.SelectedItem?.ToString() ?? "Thiếu tiền két";
                DiscrepancyExplanation = _txtExplanation.Text.Trim();
            }
            else if (FinalDifference > 0)
            {
                DiscrepancyReason = "Thừa tiền két";
                DiscrepancyExplanation = $"Thừa {FinalDifference:N0} đ so với sổ sách.";
            }
            else
            {
                DiscrepancyReason = "Khớp két hoàn hảo";
                DiscrepancyExplanation = "Khớp 100% doanh thu tiền mặt.";
            }

            _btnSubmit.Enabled = false;
            _btnSubmit.Text = "⏳ Đang gửi yêu cầu đóng ca...";

            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri(AppTheme.ApiBaseUrl);

                var payload = new
                {
                    SessionId = _sessionId,
                    ActualEndingCash = FinalActualCash,
                    ManagerPin = _txtManagerPin.Text.Trim(),
                    DiscrepancyReason = DiscrepancyReason,
                    DiscrepancyExplanation = DiscrepancyExplanation
                };

                var res = await client.PostAsJsonAsync("/api/v1/shifts/sessions/close", payload);
                if (res.IsSuccessStatusCode)
                {
                    IsClosedSuccessfully = true;
                    MessageBox.Show(
                        $"🎉 ĐÃ CHỐT CA VÀ ĐÓNG KÉT THÀNH CÔNG!\n\n" +
                        $"• Phiên ca: #{_sessionId}\n" +
                        $"• Tiền mặt kiểm đếm: {FinalActualCash:N0} đ\n" +
                        $"• Chênh lệch két: {FinalDifference:N0} đ\n" +
                        $"• Trạng thái két: ĐÃ ĐÓNG KHÓA AN TOÀN",
                        "Chốt ca hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi đóng ca từ máy chủ: {err}", "Lỗi đóng ca", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _btnSubmit.Enabled = true;
                    _btnSubmit.Text = "🔒 HOÀN TẤT CHỐT CA & ĐÓNG KÉT";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối máy chủ: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnSubmit.Enabled = true;
                _btnSubmit.Text = "🔒 HOÀN TẤT CHỐT CA & ĐÓNG KÉT";
            }
        }
    }

    public class ShiftOrderViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
