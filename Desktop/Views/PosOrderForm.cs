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

public class PosOrderForm : Form
{
    private readonly HttpClient? _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5137";
    private readonly int _employeeId = 1;
    private readonly int _branchId = 1;

    // Model item giỏ hàng nội bộ của Form
    private class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }

    private readonly List<CartItem> _cartItems = new();

    // UI Controls
    private TextBox txtBarcode = null!;
    private Button btnSearchProduct = null!;
    private DataGridView dgvCart = null!;
    private NumericUpDown numQuantity = null!;
    private Button btnUpdateQty = null!;
    private Button btnRemoveItem = null!;
    private TextBox txtPromotionCode = null!;
    private Button btnApplyPromotion = null!;
    private Label lblDiscountInfo = null!;
    private Label lblTotalAmount = null!;
    private Label lblFinalAmount = null!;
    private ComboBox cbPaymentMethod = null!;
    private Button btnCheckout = null!;
    private Button btnCancel = null!;

    private decimal _discountAmount = 0m;

    // 1. Constructor mặc định bắt buộc cho Designer
    public PosOrderForm()
    {
        InitializeComponent();
    }

    // 2. Constructor nhận tham số khi gọi thực tế
    public PosOrderForm(HttpClient httpClient, string apiBaseUrl, int employeeId = 1, int branchId = 1) : this()
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;
        _employeeId = employeeId;
        _branchId = branchId;
    }

    private void InitializeComponent()
    {
        this.Text = "POS - Lập Đơn Hàng & Bán Lẻ";
        this.Size = new Size(950, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(244, 246, 248);

        // --- Left Panel: Quét Barcode & Bảng Giỏ Hàng ---
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var pnlBarcode = new Panel { Dock = DockStyle.Top, Height = 55 };
        var lblScan = new Label { Text = "Mã vạch (Barcode):", Location = new Point(0, 5), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        txtBarcode = new TextBox { Location = new Point(0, 25), Size = new Size(350, 27), Font = new Font("Segoe UI", 10f) };
        txtBarcode.KeyDown += TxtBarcode_KeyDown;

        btnSearchProduct = new Button
        {
            Text = "Thêm SP",
            Location = new Point(360, 24),
            Size = new Size(90, 29),
            BackColor = Color.FromArgb(9, 109, 217),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSearchProduct.Click += BtnSearchProduct_Click;

        pnlBarcode.Controls.Add(lblScan);
        pnlBarcode.Controls.Add(txtBarcode);
        pnlBarcode.Controls.Add(btnSearchProduct);

        var pnlCartActions = new Panel { Dock = DockStyle.Bottom, Height = 45 };
        var lblQty = new Label { Text = "Số lượng:", Location = new Point(5, 12), AutoSize = true };
        numQuantity = new NumericUpDown { Location = new Point(70, 10), Size = new Size(70, 25), Minimum = 1, Maximum = 9999, Value = 1 };

        btnUpdateQty = new Button { Text = "Sửa SL", Location = new Point(150, 8), Size = new Size(75, 28) };
        btnUpdateQty.Click += BtnUpdateQty_Click;

        btnRemoveItem = new Button { Text = "Xóa Món", Location = new Point(235, 8), Size = new Size(85, 28), ForeColor = Color.Red };
        btnRemoveItem.Click += BtnRemoveItem_Click;

        pnlCartActions.Controls.Add(lblQty);
        pnlCartActions.Controls.Add(numQuantity);
        pnlCartActions.Controls.Add(btnUpdateQty);
        pnlCartActions.Controls.Add(btnRemoveItem);

        dgvCart = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvCart.Columns.Add("ProductId", "ID");
        dgvCart.Columns["ProductId"].Visible = false;
        dgvCart.Columns.Add("Barcode", "Mã Vạch");
        dgvCart.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvCart.Columns.Add("UnitPrice", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "SL");
        dgvCart.Columns.Add("SubTotal", "Thành Tiền");

        pnlLeft.Controls.Add(dgvCart);
        pnlLeft.Controls.Add(pnlCartActions);
        pnlLeft.Controls.Add(pnlBarcode);

        // --- Right Panel: Khuyến mãi & Thanh toán ---
        var pnlRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 330,
            BackColor = Color.White,
            Padding = new Padding(20)
        };

        var lblPromoTitle = new Label { Text = "MÃ KHUYẾN MÃI (PROMOTION)", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        txtPromotionCode = new TextBox { Location = new Point(20, 45), Size = new Size(180, 25) };
        btnApplyPromotion = new Button { Text = "Áp Dụng", Location = new Point(210, 44), Size = new Size(85, 27) };
        btnApplyPromotion.Click += BtnApplyPromotion_Click;

        lblDiscountInfo = new Label { Text = "Chưa áp dụng khuyến mãi", ForeColor = Color.Gray, Location = new Point(20, 75), AutoSize = true };

        var sep = new Label { BorderStyle = BorderStyle.Fixed3D, Height = 2, Width = 280, Location = new Point(20, 110) };

        var lblPayMethodTitle = new Label { Text = "Phương thức thanh toán:", Location = new Point(20, 130), AutoSize = true };
        cbPaymentMethod = new ComboBox
        {
            Location = new Point(20, 155),
            Size = new Size(275, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbPaymentMethod.Items.Add(new KeyValuePair<int, string>(1, "💵 Tiền mặt"));
        cbPaymentMethod.Items.Add(new KeyValuePair<int, string>(2, "📱 Chuyển khoản QR (VietQR)"));
        cbPaymentMethod.Items.Add(new KeyValuePair<int, string>(3, "💳 Thẻ ngân hàng / POS"));
        cbPaymentMethod.DisplayMember = "Value";
        cbPaymentMethod.ValueMember = "Key";
        cbPaymentMethod.SelectedIndex = 0;

        lblTotalAmount = new Label { Text = "Tạm tính: 0 ₫", Font = new Font("Segoe UI", 11f), Location = new Point(20, 210), AutoSize = true };
        lblFinalAmount = new Label { Text = "CẦN TRẢ: 0 ₫", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(9, 109, 217), Location = new Point(20, 250), AutoSize = true };

        btnCheckout = new Button
        {
            Text = "HOÀN TẤT THANH TOÁN",
            Location = new Point(20, 310),
            Size = new Size(275, 45),
            BackColor = Color.FromArgb(56, 158, 13),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        btnCheckout.Click += BtnCheckout_Click;

        btnCancel = new Button
        {
            Text = "Đóng POS",
            Location = new Point(20, 370),
            Size = new Size(275, 35),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += BtnCancel_Click;

        pnlRight.Controls.Add(lblPromoTitle);
        pnlRight.Controls.Add(txtPromotionCode);
        pnlRight.Controls.Add(btnApplyPromotion);
        pnlRight.Controls.Add(lblDiscountInfo);
        pnlRight.Controls.Add(sep);
        pnlRight.Controls.Add(lblPayMethodTitle);
        pnlRight.Controls.Add(cbPaymentMethod);
        pnlRight.Controls.Add(lblTotalAmount);
        pnlRight.Controls.Add(lblFinalAmount);
        pnlRight.Controls.Add(btnCheckout);
        pnlRight.Controls.Add(btnCancel);

        this.Controls.Add(pnlLeft);
        this.Controls.Add(pnlRight);
    }

    private void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            BtnSearchProduct_Click(sender, EventArgs.Empty);
        }
    }

    private async void BtnSearchProduct_Click(object? sender, EventArgs e)
    {
        string code = txtBarcode.Text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        if (_httpClient == null)
        {
            MessageBox.Show("Môi trường Designer: Không gọi API.", "Thông báo");
            return;
        }

        try
        {
            // 1. Gọi endpoint tra cứu barcode chuẩn của backend
            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Products/barcode/{code}");

            // 2. Dự phòng tìm kiếm theo Id nếu mã nhập là số nguyên
            if (!res.IsSuccessStatusCode)
            {
                res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Products/{code}");
            }

            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                int pId = data.GetProperty("productId").GetInt32();
                string pName = data.GetProperty("productName").GetString() ?? "Sản phẩm";
                string barcode = data.TryGetProperty("barcode", out var bc) ? (bc.GetString() ?? code) : code;

                // Tương thích cả field 'price' từ ProductBarcodeDto và 'sellingPrice' từ ProductDto
                decimal price = 0m;
                if (data.TryGetProperty("price", out var pr))
                {
                    price = pr.GetDecimal();
                }
                else if (data.TryGetProperty("sellingPrice", out var sp))
                {
                    price = sp.GetDecimal();
                }

                var exist = _cartItems.FirstOrDefault(x => x.ProductId == pId);
                if (exist != null)
                {
                    exist.Quantity++;
                }
                else
                {
                    _cartItems.Add(new CartItem
                    {
                        ProductId = pId,
                        ProductName = pName,
                        Barcode = barcode,
                        UnitPrice = price,
                        Quantity = 1
                    });
                }

                txtBarcode.Clear();
                txtBarcode.Focus();
                RefreshCartGrid();
            }
            else
            {
                MessageBox.Show($"Không tìm thấy sản phẩm với mã: {code}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi truy vấn sản phẩm: {ex.Message}", "Lỗi");
        }
    }

    private void BtnUpdateQty_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null) return;
        int pId = Convert.ToInt32(dgvCart.CurrentRow.Cells["ProductId"].Value);
        var item = _cartItems.FirstOrDefault(x => x.ProductId == pId);
        if (item != null)
        {
            item.Quantity = (int)numQuantity.Value;
            RefreshCartGrid();
        }
    }

    private void BtnRemoveItem_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null) return;
        int pId = Convert.ToInt32(dgvCart.CurrentRow.Cells["ProductId"].Value);
        var item = _cartItems.FirstOrDefault(x => x.ProductId == pId);
        if (item != null)
        {
            _cartItems.Remove(item);
            RefreshCartGrid();
        }
    }

    private async void BtnApplyPromotion_Click(object? sender, EventArgs e)
    {
        string promo = txtPromotionCode.Text.Trim();
        if (string.IsNullOrEmpty(promo))
        {
            _discountAmount = 0;
            lblDiscountInfo.Text = "Chưa nhập mã";
            lblDiscountInfo.ForeColor = Color.Gray;
            RefreshCartGrid();
            return;
        }

        if (_httpClient == null) return;

        try
        {
            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/promotions/code/{promo}");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                string discType = data.TryGetProperty("discountType", out var dt) ? (dt.GetString() ?? "Percentage") : "Percentage";
                decimal discVal = data.TryGetProperty("discountValue", out var dv) ? dv.GetDecimal() : (data.TryGetProperty("discountPercent", out var dp) ? dp.GetDecimal() : 0m);
                decimal minOrder = data.TryGetProperty("minimumOrderAmount", out var mo) ? mo.GetDecimal() : 0m;
                decimal? maxDiscount = data.TryGetProperty("maximumDiscountAmount", out var mx) && mx.ValueKind != JsonValueKind.Null ? mx.GetDecimal() : null;

                decimal subtotal = _cartItems.Sum(x => x.SubTotal);

                // Kiểm tra điều kiện đơn hàng tối thiểu
                if (subtotal < minOrder)
                {
                    _discountAmount = 0;
                    lblDiscountInfo.Text = $"Đơn tối thiểu phải từ {minOrder:N0} ₫";
                    lblDiscountInfo.ForeColor = Color.Red;
                }
                else
                {
                    if (discType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                    {
                        _discountAmount = subtotal * (discVal / 100m);
                        if (maxDiscount.HasValue && _discountAmount > maxDiscount.Value)
                        {
                            _discountAmount = maxDiscount.Value;
                        }
                        lblDiscountInfo.Text = $"Áp dụng: Giảm {discVal}% (-{_discountAmount:N0} ₫)";
                    }
                    else
                    {
                        _discountAmount = discVal;
                        lblDiscountInfo.Text = $"Áp dụng: Giảm -{_discountAmount:N0} ₫";
                    }
                    lblDiscountInfo.ForeColor = Color.DarkGreen;
                }
            }
            else
            {
                _discountAmount = 0;
                lblDiscountInfo.Text = "Mã không hợp lệ hoặc hết hạn";
                lblDiscountInfo.ForeColor = Color.Red;
            }
            RefreshCartGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi áp dụng KM: {ex.Message}", "Lỗi");
        }
    }

    private void RefreshCartGrid()
    {
        dgvCart.Rows.Clear();
        foreach (var item in _cartItems)
        {
            dgvCart.Rows.Add(item.ProductId, item.Barcode, item.ProductName, $"{item.UnitPrice:N0} ₫", item.Quantity, $"{item.SubTotal:N0} ₫");
        }

        decimal total = _cartItems.Sum(x => x.SubTotal);
        decimal finalAmount = Math.Max(0, total - _discountAmount);

        lblTotalAmount.Text = $"Tạm tính: {total:N0} ₫";
        lblFinalAmount.Text = $"CẦN TRẢ: {finalAmount:N0} ₫";
    }

    private async void BtnCheckout_Click(object? sender, EventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống! Vui lòng thêm sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_httpClient == null) return;

        var selectedPay = (KeyValuePair<int, string>)cbPaymentMethod.SelectedItem!;

        var payload = new
        {
            employeeId = _employeeId,
            customerId = (int?)null,
            branchId = _branchId,
            voucherId = (int?)null,
            promotionCode = string.IsNullOrWhiteSpace(txtPromotionCode.Text) ? null : txtPromotionCode.Text.Trim(),
            paymentMethod = selectedPay.Key,
            items = _cartItems.Select(x => new
            {
                productId = x.ProductId,
                quantity = x.Quantity,
                unitPrice = x.UnitPrice
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            btnCheckout.Enabled = false;
            btnCheckout.Text = "Đang tạo đơn...";

            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/orders", content);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể tạo đơn hàng.\nChi tiết: {body}", "Lỗi Backend", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var responseBody = await res.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(responseBody);
            var orderRoot = orderDoc.RootElement;
            var orderData = orderRoot.TryGetProperty("data", out var od) ? od : orderRoot;

            int orderId = orderData.GetProperty("orderId").GetInt32();
            decimal finalAmount = orderData.GetProperty("finalAmount").GetDecimal();

            // Nếu chọn phương thức thanh toán là QR Code (Key == 2)
            if (selectedPay.Key == 2)
            {
                btnCheckout.Text = "Đang khởi tạo QR...";

                // Gọi API tạo transaction thanh toán QR
                var paymentReqObj = new
                {
                    orderId = orderId,
                    paymentMethod = 2,
                    amount = finalAmount
                };

                var payContent = new StringContent(JsonSerializer.Serialize(paymentReqObj), Encoding.UTF8, "application/json");
                var payRes = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/payments", payContent);

                if (payRes.IsSuccessStatusCode)
                {
                    var payBody = await payRes.Content.ReadAsStringAsync();
                    using var payDoc = JsonDocument.Parse(payBody);
                    var payRoot = payDoc.RootElement;
                    var payData = payRoot.GetProperty("data");

                    int transId = payData.GetProperty("paymentTransactionId").GetInt32();
                    string transCode = payData.GetProperty("transactionCode").GetString() ?? "";
                    string qrCode = payData.TryGetProperty("qrCode", out var qr) ? (qr.GetString() ?? "") : "";
                    string paymentUrl = payData.TryGetProperty("paymentUrl", out var url) ? (url.GetString() ?? "") : "";

                    string qrDataToDisplay = !string.IsNullOrEmpty(qrCode) ? qrCode : paymentUrl;

                    // Mở Form Thanh toán QR chuyên nghiệp kiểu Bách Hóa Xanh
                    using var qrForm = new QrPaymentForm(_httpClient, _apiBaseUrl, transId, orderId, finalAmount, qrDataToDisplay, transCode);
                    var qrResult = qrForm.ShowDialog(this);

                    if (qrResult == DialogResult.OK)
                    {
                        MessageBox.Show("✅ THANH TOÁN CHUYỂN KHOẢN QR THÀNH CÔNG!\nĐã cập nhật đơn hàng & in hóa đơn.", "Bách Hóa Xanh POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("⚠️ Khách hàng đã hủy hoặc chưa hoàn tất thanh toán QR.", "Thông báo POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    var errBody = await payRes.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể tạo giao dịch QR.\nChi tiết: {errBody}", "Lỗi Thanh Toán QR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Thanh toán Tiền mặt hoặc Thẻ
                MessageBox.Show("✅ Thanh toán đơn hàng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCheckout.Enabled = true;
            btnCheckout.Text = "HOÀN TẤT THANH TOÁN";
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}