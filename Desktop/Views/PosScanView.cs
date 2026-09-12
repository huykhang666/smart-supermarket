using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class PosScanView : UserControl
{
    private TextBox txtBarcode = null!;
    private Button btnScan = null!;
    private DataGridView dgvCart = null!;
    private Label lblSubTotalVal = null!;
    private Label lblVatVal = null!;
    private Label lblDiscountVal = null!;
    private Label lblGrandTotal = null!;
    private NumericUpDown numCashPaid = null!;
    private Label lblChangeVal = null!;
    private Button btnCheckout = null!;
    private Button btnPayCash = null!;
    private Button btnPayCard = null!;
    private Button btnPayQr = null!;
    private Button btnQtyPlus = null!;
    private Button btnQtyMinus = null!;
    private Button btnRemoveItem = null!;
    private Button btnClearCart = null!;
    private TextBox txtVoucherCode = null!;
    private Button btnApplyVoucher = null!;
    private Label lblVoucherStatus = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";
    private int _selectedPaymentMethod = 1; // 1 = Cash, 2 = Card, 3 = VietQR
    private decimal _appliedDiscountAmount = 0m;
    private string? _appliedVoucherCode = null;

    private readonly List<CartItem> _cart = new();

    public PosScanView()
    {
        InitializeComponent();
        RefreshCartGrid();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(15);

        // Main Split: Left 68% (Cart & Scanner), Right 32% (Payment & Bill Summary)
        var pnlMainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
        pnlMainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // --- LEFT PANEL (Barcode Input + Quick Item Actions + Cart Grid) ---
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };

        // Barcode input card
        var pnlBarcodeCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 10)
        };
        ThemeManager.ApplyCardPanel(pnlBarcodeCard);

        var lblScan = new Label
        {
            Text = "📷 MÁY QUÉT BARCODE BÁN HÀNG THU NGÂN (STAFF POS):",
            Font = ThemeManager.BodyBold,
            Location = new Point(15, 12),
            AutoSize = true,
            ForeColor = ThemeManager.PrimaryHover
        };

        txtBarcode = new TextBox
        {
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            Location = new Point(15, 38),
            Size = new Size(380, 34),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Quét máy bắn mã vạch hoặc nhập mã (Enter)..."
        };
        txtBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnScan_Click(s, e); };

        btnScan = new Button
        {
            Text = "➕ THÊM VÀO GIỎ (ENTER)",
            Size = new Size(200, 34),
            Location = new Point(405, 38)
        };
        ThemeManager.ApplyPrimaryButton(btnScan);
        btnScan.Click += BtnScan_Click;

        pnlBarcodeCard.Controls.Add(lblScan);
        pnlBarcodeCard.Controls.Add(txtBarcode);
        pnlBarcodeCard.Controls.Add(btnScan);

        // Cart DataGrid container
        var pnlCartCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlCartCard);

        // Action Toolbar above Grid (+ / - / Delete / Clear)
        var pnlGridToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.White,
            Padding = new Padding(5)
        };

        btnQtyPlus = new Button { Text = "➕ Tăng SL", Size = new Size(110, 34), Location = new Point(5, 5), Font = ThemeManager.BodyBold };
        ThemeManager.ApplySecondaryButton(btnQtyPlus);
        btnQtyPlus.Click += (s, e) => ChangeSelectedQty(1);

        btnQtyMinus = new Button { Text = "➖ Giảm SL", Size = new Size(110, 34), Location = new Point(122, 5), Font = ThemeManager.BodyBold };
        ThemeManager.ApplySecondaryButton(btnQtyMinus);
        btnQtyMinus.Click += (s, e) => ChangeSelectedQty(-1);

        btnRemoveItem = new Button { Text = "🗑️ Xóa dòng", Size = new Size(110, 34), Location = new Point(239, 5), Font = ThemeManager.BodyBold };
        ThemeManager.ApplySecondaryButton(btnRemoveItem);
        btnRemoveItem.Click += (s, e) => RemoveSelectedItem();

        btnClearCart = new Button { Text = "🔄 Hủy giỏ hàng", Size = new Size(130, 34), Location = new Point(356, 5), Font = ThemeManager.BodyBold };
        ThemeManager.ApplySecondaryButton(btnClearCart);
        btnClearCart.Click += (s, e) => { _cart.Clear(); RefreshCartGrid(); };

        pnlGridToolbar.Controls.Add(btnQtyPlus);
        pnlGridToolbar.Controls.Add(btnQtyMinus);
        pnlGridToolbar.Controls.Add(btnRemoveItem);
        pnlGridToolbar.Controls.Add(btnClearCart);

        dgvCart = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvCart);
        dgvCart.Columns.Add("ProductId", "ID");
        dgvCart.Columns.Add("Barcode", "Mã Vạch");
        dgvCart.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Price", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "Số Lượng");
        dgvCart.Columns.Add("Vat", "Thuế VAT");
        dgvCart.Columns.Add("Total", "Thành Tiền");

        pnlCartCard.Controls.Add(dgvCart);
        pnlCartCard.Controls.Add(pnlGridToolbar);

        pnlLeft.Controls.Add(pnlCartCard);
        pnlLeft.Controls.Add(pnlBarcodeCard);

        // --- RIGHT PANEL (Billing Summary & Cashier Payment) ---
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20)
        };
        ThemeManager.ApplyCardPanel(pnlRight);

        var lblSummaryHeader = new Label
        {
            Text = "🧾 BẢNG TÍNH TIỀN THU NGÂN",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 15),
            AutoSize = true
        };

        var lblSubTotal = new Label { Text = "Tạm tính tiền hàng:", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 55), AutoSize = true };
        lblSubTotalVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(160, 55), AutoSize = true };

        var lblVat = new Label { Text = "Thuế VAT (10%):", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 88), AutoSize = true };
        lblVatVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(160, 88), AutoSize = true };

        // Voucher / Promotion Code Input
        var pnlVoucher = new Panel { Location = new Point(15, 115), Size = new Size(270, 60), BackColor = Color.FromArgb(235, 245, 255) };
        var lblVoucherLbl = new Label { Text = "🎁 Mã khuyến mãi:", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.NavyBrand, Location = new Point(0, 0), AutoSize = true };
        txtVoucherCode = new TextBox
        {
            Font = ThemeManager.BodyFont,
            Location = new Point(0, 22),
            Size = new Size(165, 26),
            PlaceholderText = "Nhập mã voucher..."
        };
        txtVoucherCode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = ApplyVoucherAsync(); };
        btnApplyVoucher = new Button { Text = "✅ Áp Dụng", Size = new Size(98, 26), Location = new Point(170, 22) };
        ThemeManager.ApplyPrimaryButton(btnApplyVoucher);
        btnApplyVoucher.Click += async (s, e) => await ApplyVoucherAsync();
        lblVoucherStatus = new Label { Text = "", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.Success, Location = new Point(0, 52), AutoSize = true };
        pnlVoucher.Controls.Add(lblVoucherLbl);
        pnlVoucher.Controls.Add(txtVoucherCode);
        pnlVoucher.Controls.Add(btnApplyVoucher);
        pnlVoucher.Controls.Add(lblVoucherStatus);

        var lblDiscount = new Label { Text = "Giảm giá (Voucher):", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 180), AutoSize = true };
        lblDiscountVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.Success, Location = new Point(175, 180), AutoSize = true };

        var pnlDivider1 = new Panel { Location = new Point(15, 205), Size = new Size(270, 2), BackColor = ThemeManager.Border };

        var lblTotalTitle = new Label { Text = "TỔNG TIỀN THANH TOÁN", Font = ThemeManager.SubtitleFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 215), AutoSize = true };
        lblGrandTotal = new Label
        {
            Text = "0 VNĐ",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(12, 238),
            AutoSize = true
        };

        var pnlDivider2 = new Panel { Location = new Point(15, 288), Size = new Size(270, 2), BackColor = ThemeManager.Border };

        // Customer Cash Input & Change Calculation
        var lblCashPaid = new Label { Text = "Tiền khách đưa (VNĐ):", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(15, 298), AutoSize = true };
        numCashPaid = new NumericUpDown
        {
            Location = new Point(15, 323),
            Size = new Size(265, 34),
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            Maximum = 100000000,
            Minimum = 0,
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
        numCashPaid.ValueChanged += (s, e) => CalculateChange();

        var lblChangeTitle = new Label { Text = "Tiền thừa trả lại khách:", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 366), AutoSize = true };
        lblChangeVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.Success, Location = new Point(175, 366), AutoSize = true };

        // Payment Method Options
        var lblPayMethod = new Label { Text = "Hình thức thanh toán:", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(15, 400), AutoSize = true };

        btnPayCash = new Button { Text = "💵 Tiền Mặt", Size = new Size(85, 36), Location = new Point(15, 428) };
        ThemeManager.ApplyPrimaryButton(btnPayCash);
        btnPayCash.Click += (s, e) => SelectPaymentMethod(1);

        btnPayCard = new Button { Text = "💳 Thẻ POS", Size = new Size(85, 36), Location = new Point(105, 428) };
        ThemeManager.ApplySecondaryButton(btnPayCard);
        btnPayCard.Click += (s, e) => SelectPaymentMethod(2);

        btnPayQr = new Button { Text = "📱 VietQR", Size = new Size(85, 36), Location = new Point(195, 428) };
        ThemeManager.ApplySecondaryButton(btnPayQr);
        btnPayQr.Click += (s, e) => SelectPaymentMethod(3);

        // Big Checkout CTA Button
        btnCheckout = new Button
        {
            Text = "💳 HOÀN TẤT THANH TOÁN & IN HÓA ĐƠN",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            Size = new Size(265, 52),
            Location = new Point(15, 475)
        };
        AppTheme.ApplyAccentButton(btnCheckout);
        btnCheckout.Click += BtnCheckout_Click;

        pnlRight.Controls.Add(lblSummaryHeader);
        pnlRight.Controls.Add(lblSubTotal);
        pnlRight.Controls.Add(lblSubTotalVal);
        pnlRight.Controls.Add(lblVat);
        pnlRight.Controls.Add(lblVatVal);
        pnlRight.Controls.Add(pnlVoucher);
        pnlRight.Controls.Add(lblDiscount);
        pnlRight.Controls.Add(lblDiscountVal);
        pnlRight.Controls.Add(pnlDivider1);
        pnlRight.Controls.Add(lblTotalTitle);
        pnlRight.Controls.Add(lblGrandTotal);
        pnlRight.Controls.Add(pnlDivider2);
        pnlRight.Controls.Add(lblCashPaid);
        pnlRight.Controls.Add(numCashPaid);
        pnlRight.Controls.Add(lblChangeTitle);
        pnlRight.Controls.Add(lblChangeVal);
        pnlRight.Controls.Add(lblPayMethod);
        pnlRight.Controls.Add(btnPayCash);
        pnlRight.Controls.Add(btnPayCard);
        pnlRight.Controls.Add(btnPayQr);
        pnlRight.Controls.Add(btnCheckout);

        pnlMainContainer.Controls.Add(pnlLeft, 0, 0);
        pnlMainContainer.Controls.Add(pnlRight, 1, 0);

        this.Controls.Add(pnlMainContainer);
    }

    private void SelectPaymentMethod(int method)
    {
        _selectedPaymentMethod = method;
        ThemeManager.ApplySecondaryButton(btnPayCash);
        ThemeManager.ApplySecondaryButton(btnPayCard);
        ThemeManager.ApplySecondaryButton(btnPayQr);

        if (method == 1) ThemeManager.ApplyPrimaryButton(btnPayCash);
        else if (method == 2) ThemeManager.ApplyPrimaryButton(btnPayCard);
        else if (method == 3) ThemeManager.ApplyPrimaryButton(btnPayQr);
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        string code = txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        try
        {
            // Lookup product via real backend API
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products/barcode/{code}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    int productId = dataElem.GetProperty("productId").GetInt32();
                    string name = dataElem.GetProperty("productName").GetString() ?? code;
                    decimal price = dataElem.GetProperty("price").GetDecimal();
                    string barcode = dataElem.GetProperty("barcode").GetString() ?? code;

                    var existing = _cart.FirstOrDefault(c => c.Barcode == barcode || c.ProductId == productId);
                    if (existing != null)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        _cart.Add(new CartItem
                        {
                            ProductId = productId,
                            Barcode = barcode,
                            Name = name,
                            Price = price,
                            Quantity = 1,
                            VatPercent = 10
                        });
                    }

                    txtBarcode.Clear();
                    RefreshCartGrid();
                    return;
                }
            }

            // Fallback search by barcode/name if lookup by exact barcode path returned 404
            var searchResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products?search={Uri.EscapeDataString(code)}&page=1&pageSize=1");
            if (searchResponse.IsSuccessStatusCode)
            {
                var searchJson = await searchResponse.Content.ReadAsStringAsync();
                using var searchDoc = JsonDocument.Parse(searchJson);
                if (searchDoc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var firstItem = items[0];
                    int productId = firstItem.GetProperty("productId").GetInt32();
                    string name = firstItem.GetProperty("productName").GetString() ?? code;
                    decimal price = firstItem.GetProperty("price").GetDecimal();
                    string barcode = firstItem.GetProperty("barcode").GetString() ?? code;

                    var existing = _cart.FirstOrDefault(c => c.Barcode == barcode || c.ProductId == productId);
                    if (existing != null)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        _cart.Add(new CartItem
                        {
                            ProductId = productId,
                            Barcode = barcode,
                            Name = name,
                            Price = price,
                            Quantity = 1,
                            VatPercent = 10
                        });
                    }

                    txtBarcode.Clear();
                    RefreshCartGrid();
                    return;
                }
            }

            MessageBox.Show($"⚠️ Không tìm thấy sản phẩm có mã vạch '{code}' trong CSDL hệ thống!", "Không tìm thấy sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch
        {
            MessageBox.Show("Không thể kết nối đến Backend Server API. Vui lòng kiểm tra lại dịch vụ Backend.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        txtBarcode.SelectAll();
        txtBarcode.Focus();
    }

    private void ChangeSelectedQty(int delta)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int idx = dgvCart.CurrentRow.Index;
        if (idx >= 0 && idx < _cart.Count)
        {
            _cart[idx].Quantity += delta;
            if (_cart[idx].Quantity <= 0)
            {
                _cart.RemoveAt(idx);
            }
            RefreshCartGrid();
        }
    }

    private void RemoveSelectedItem()
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int idx = dgvCart.CurrentRow.Index;
        if (idx >= 0 && idx < _cart.Count)
        {
            _cart.RemoveAt(idx);
            RefreshCartGrid();
        }
    }

    private void CalculateChange()
    {
        decimal subTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal vatTotal = subTotal * 0.10m;
        decimal grandTotal = subTotal + vatTotal - _appliedDiscountAmount;
        if (grandTotal < 0) grandTotal = 0;
        decimal cashPaid = numCashPaid.Value;

        decimal change = cashPaid - grandTotal;
        lblChangeVal.Text = change >= 0 ? $"{change:N0} VNĐ" : "0 VNĐ (Thiếu " + Math.Abs(change).ToString("N0") + " VNĐ)";
        lblChangeVal.ForeColor = change >= 0 ? ThemeManager.Success : ThemeManager.Danger;
    }

    private async Task ApplyVoucherAsync()
    {
        string code = txtVoucherCode.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        decimal subTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal vatTotal = subTotal * 0.10m;
        decimal orderTotal = subTotal + vatTotal;

        if (orderTotal <= 0)
        {
            lblVoucherStatus.Text = "⚠️ Giỏ hàng trống!";
            lblVoucherStatus.ForeColor = ThemeManager.Danger;
            return;
        }

        try
        {
            btnApplyVoucher.Enabled = false;
            lblVoucherStatus.Text = "Đang kiểm tra...";
            lblVoucherStatus.ForeColor = ThemeManager.TextSecondary;

            var payload = JsonSerializer.Serialize(new { promotionCode = code, orderTotalAmount = orderTotal });
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/promotions/apply",
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
            bool isSuccess = data.TryGetProperty("isSuccess", out var s) && s.GetBoolean();
            string message = data.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";
            decimal discountAmt = data.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() : 0m;

            if (isSuccess)
            {
                _appliedDiscountAmount = discountAmt;
                _appliedVoucherCode = code;
                lblVoucherStatus.Text = $"✅ Đã áp dụng! Giảm {discountAmt:N0} VNĐ";
                lblVoucherStatus.ForeColor = ThemeManager.Success;
                RefreshCartGrid();
            }
            else
            {
                _appliedDiscountAmount = 0;
                _appliedVoucherCode = null;
                lblVoucherStatus.Text = $"❌ {message}";
                lblVoucherStatus.ForeColor = ThemeManager.Danger;
                RefreshCartGrid();
            }
        }
        catch
        {
            lblVoucherStatus.Text = "⚠️ Lỗi kết nối API";
            lblVoucherStatus.ForeColor = ThemeManager.Danger;
        }
        finally
        {
            btnApplyVoucher.Enabled = true;
        }
    }

    private void RefreshCartGrid()
    {
        dgvCart.Rows.Clear();
        decimal subTotal = 0;
        decimal vatTotal = 0;

        foreach (var item in _cart)
        {
            decimal itemSub = item.Price * item.Quantity;
            decimal itemVat = itemSub * (item.VatPercent / 100m);
            decimal itemTotal = itemSub + itemVat;

            subTotal += itemSub;
            vatTotal += itemVat;

            dgvCart.Rows.Add(item.ProductId, item.Barcode, item.Name, $"{item.Price:N0} đ", item.Quantity, $"{item.VatPercent}%", $"{itemTotal:N0} đ");
        }

        decimal grandTotal = subTotal + vatTotal - _appliedDiscountAmount;
        if (grandTotal < 0) grandTotal = 0;

        lblSubTotalVal.Text = $"{subTotal:N0} VNĐ";
        lblVatVal.Text = $"{vatTotal:N0} VNĐ";
        lblDiscountVal.Text = _appliedDiscountAmount > 0 ? $"-{_appliedDiscountAmount:N0} VNĐ" : "0 VNĐ";
        lblDiscountVal.ForeColor = _appliedDiscountAmount > 0 ? ThemeManager.Success : ThemeManager.TextSecondary;
        lblGrandTotal.Text = $"{grandTotal:N0} VNĐ";

        if (numCashPaid.Value == 0 || numCashPaid.Value < grandTotal)
        {
            numCashPaid.Value = grandTotal;
        }

        CalculateChange();
    }

    private async void BtnCheckout_Click(object? sender, EventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Giỏ hàng POS hiện đang trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        decimal subTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal vatTotal = subTotal * 0.10m;
        decimal grandTotal = Math.Max(0, subTotal + vatTotal - _appliedDiscountAmount);

        if (numCashPaid.Value < grandTotal && _selectedPaymentMethod == 1)
        {
            MessageBox.Show("Số tiền khách đưa chưa đủ để hoàn tất đơn hàng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnCheckout.Enabled = false;
            btnCheckout.Text = "⏳ Đang xử lý...";

            var orderItems = _cart.Select(i => new
            {
                productId = i.ProductId,
                quantity = i.Quantity,
                unitPrice = i.Price
            }).ToList();

            // PaymentMethod mapping: 1=Cash, 2=QRCode(VietQR), 3=CreditCard(Thẻ)
            int apiPayMethod = _selectedPaymentMethod == 3 ? 3 : (_selectedPaymentMethod == 2 ? 2 : 1);

            var orderPayload = new
            {
                employeeId = 1,
                branchId = 1,
                paymentMethod = apiPayMethod,
                promotionCode = _appliedVoucherCode,
                items = orderItems
            };

            var content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            string methodText = _selectedPaymentMethod == 1 ? "Tiền mặt" : (_selectedPaymentMethod == 2 ? "VietQR" : "Thẻ POS");
            decimal changeAmount = Math.Max(0, numCashPaid.Value - grandTotal);

            if (response.IsSuccessStatusCode)
            {
                string voucherInfo = _appliedVoucherCode != null
                    ? $"\n• Mã giảm giá: {_appliedVoucherCode} (-{_appliedDiscountAmount:N0} VNĐ)"
                    : "";

                MessageBox.Show(
                    $"✅ HOÀN TẤT ĐƠN HÀNG BÁN LẺ THÀNH CÔNG!\n\n" +
                    $"• Tổng tiền hàng: {(subTotal + vatTotal):N0} VNĐ" +
                    voucherInfo +
                    $"\n• Thực thu: {grandTotal:N0} VNĐ" +
                    $"\n• Phương thức: {methodText}" +
                    $"\n• Tiền khách đưa: {numCashPaid.Value:N0} VNĐ" +
                    $"\n• Tiền thừa trả lại: {changeAmount:N0} VNĐ\n\n" +
                    $"Hệ thống đã lưu hóa đơn vào CSDL và đã trừ tồn kho.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _cart.Clear();
                numCashPaid.Value = 0;
                _appliedDiscountAmount = 0m;
                _appliedVoucherCode = null;
                txtVoucherCode.Clear();
                lblVoucherStatus.Text = "";
                RefreshCartGrid();
            }
            else
            {
                MessageBox.Show($"Lỗi tạo đơn hàng:\n{responseBody}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCheckout.Enabled = true;
            btnCheckout.Text = "💳 HOÀN TẤT THANH TOÁN & IN HÓA ĐƠN";
        }
    }

    private class CartItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal VatPercent { get; set; }
    }
}
