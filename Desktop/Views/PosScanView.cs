using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
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
    private Label lblGrandTotal = null!;
    private Button btnCheckout = null!;
    private Button btnPayCash = null!;
    private Button btnPayCard = null!;
    private Button btnPayQr = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    private readonly List<CartItem> _cart = new();

    public PosScanView()
    {
        InitializeComponent();
        _ = LoadSampleCartAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(15);

        // Main Container Split: Left 70%, Right 30%
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

        // --- LEFT PANEL (Barcode + Cart) ---
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };

        // Barcode input card
        var pnlBarcodeCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 10)
        };
        ThemeManager.ApplyCardPanel(pnlBarcodeCard);

        var lblScan = new Label
        {
            Text = "📷 QUÉT MÃ VẠCH (BARCODE):",
            Font = ThemeManager.BodyBold,
            Location = new Point(15, 12),
            AutoSize = true,
            ForeColor = ThemeManager.PrimaryHover
        };

        txtBarcode = new TextBox
        {
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Location = new Point(15, 34),
            Size = new Size(380, 32),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập hoặc quét mã vạch (e.g. 8935001800012)..."
        };
        txtBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnScan_Click(s, e); };

        btnScan = new Button
        {
            Text = "THÊM VÀO GIỎ (ENTER)",
            Size = new Size(180, 32),
            Location = new Point(405, 34)
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

        dgvCart = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvCart);
        dgvCart.Columns.Add("Barcode", "Mã Vạch");
        dgvCart.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Price", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "Số Lượng");
        dgvCart.Columns.Add("Vat", "Thuế");
        dgvCart.Columns.Add("Total", "Thành Tiền");

        pnlCartCard.Controls.Add(dgvCart);

        pnlLeft.Controls.Add(pnlCartCard);
        pnlLeft.Controls.Add(pnlBarcodeCard);

        // --- RIGHT PANEL (Sticky Summary & Checkout) ---
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20)
        };
        ThemeManager.ApplyCardPanel(pnlRight);

        var lblSummaryHeader = new Label
        {
            Text = "🧾 TỔNG KẾT ĐƠN HÀNG POS",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 15),
            AutoSize = true
        };

        var lblSubTotal = new Label { Text = "Tạm tính:", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 60), AutoSize = true };
        lblSubTotalVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(160, 60), AutoSize = true };

        var lblVat = new Label { Text = "Thuế VAT (8-10%):", Font = ThemeManager.BodyFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 95), AutoSize = true };
        lblVatVal = new Label { Text = "0 VNĐ", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(160, 95), AutoSize = true };

        var pnlDivider = new Panel { Location = new Point(15, 130), Size = new Size(260, 2), BackColor = ThemeManager.Border };

        var lblTotalTitle = new Label { Text = "TỔNG THANH TOÁN", Font = ThemeManager.SubtitleFont, ForeColor = ThemeManager.TextSecondary, Location = new Point(15, 145), AutoSize = true };
        lblGrandTotal = new Label
        {
            Text = "0 VNĐ",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = ThemeManager.SidebarActive, // Orange
            Location = new Point(12, 170),
            AutoSize = true
        };

        // Payment Methods
        var lblPayMethod = new Label { Text = "Phương thức thanh toán:", Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(15, 230), AutoSize = true };

        btnPayCash = new Button { Text = "💵 Tiền Mặt", Size = new Size(80, 36), Location = new Point(15, 260) };
        ThemeManager.ApplyPrimaryButton(btnPayCash);

        btnPayCard = new Button { Text = "💳 Thẻ POS", Size = new Size(80, 36), Location = new Point(102, 260) };
        ThemeManager.ApplySecondaryButton(btnPayCard);

        btnPayQr = new Button { Text = "📱 VietQR", Size = new Size(80, 36), Location = new Point(189, 260) };
        ThemeManager.ApplySecondaryButton(btnPayQr);

        // Big Checkout CTA Button
        btnCheckout = new Button
        {
            Text = "💳 THANH TOÁN (F5)",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Size = new Size(254, 54),
            Location = new Point(15, 320)
        };
        AppTheme.ApplyAccentButton(btnCheckout);
        btnCheckout.Click += (s, e) =>
        {
            if (_cart.Count == 0)
            {
                AntdUI.Message.info(this.FindForm() ?? new Form(), "Giỏ hàng hiện đang trống!");
                return;
            }
            AntdUI.Message.success(this.FindForm() ?? new Form(), $"Thanh toán thành công {lblGrandTotal.Text}! Đang in hóa đơn POS...");
            _cart.Clear();
            RefreshCartGrid();
        };

        pnlRight.Controls.Add(lblSummaryHeader);
        pnlRight.Controls.Add(lblSubTotal);
        pnlRight.Controls.Add(lblSubTotalVal);
        pnlRight.Controls.Add(lblVat);
        pnlRight.Controls.Add(lblVatVal);
        pnlRight.Controls.Add(pnlDivider);
        pnlRight.Controls.Add(lblTotalTitle);
        pnlRight.Controls.Add(lblGrandTotal);
        pnlRight.Controls.Add(lblPayMethod);
        pnlRight.Controls.Add(btnPayCash);
        pnlRight.Controls.Add(btnPayCard);
        pnlRight.Controls.Add(btnPayQr);
        pnlRight.Controls.Add(btnCheckout);

        pnlMainContainer.Controls.Add(pnlLeft, 0, 0);
        pnlMainContainer.Controls.Add(pnlRight, 1, 0);

        this.Controls.Add(pnlMainContainer);
    }

    private async Task LoadSampleCartAsync()
    {
        _cart.Clear();
        _cart.Add(new CartItem { Barcode = "8935001800012", Name = "Nước ngọt Coca-Cola Lon 330ml", Price = 10000, Quantity = 2, VatPercent = 10 });
        _cart.Add(new CartItem { Barcode = "8934673123456", Name = "Sữa tươi Vinamilk Có đường 1L", Price = 36000, Quantity = 1, VatPercent = 10 });
        RefreshCartGrid();
        await Task.CompletedTask;
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        string code = txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products/barcode/{code}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    string name = dataElem.TryGetProperty("productName", out var n) ? n.GetString() ?? code : code;
                    decimal price = dataElem.TryGetProperty("price", out var p) ? p.GetDecimal() : 15000m;

                    var existing = _cart.FirstOrDefault(c => c.Barcode == code);
                    if (existing != null) existing.Quantity++;
                    else _cart.Add(new CartItem { Barcode = code, Name = name, Price = price, Quantity = 1, VatPercent = 10 });
                }
            }
            else
            {
                var existing = _cart.FirstOrDefault(c => c.Barcode == code);
                if (existing != null) existing.Quantity++;
                else _cart.Add(new CartItem { Barcode = code, Name = $"Sản phẩm mã {code}", Price = 25000m, Quantity = 1, VatPercent = 10 });
            }
        }
        catch
        {
            var existing = _cart.FirstOrDefault(c => c.Barcode == code);
            if (existing != null) existing.Quantity++;
            else _cart.Add(new CartItem { Barcode = code, Name = $"Sản phẩm mã {code}", Price = 25000m, Quantity = 1, VatPercent = 10 });
        }

        txtBarcode.Clear();
        RefreshCartGrid();
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

            dgvCart.Rows.Add(item.Barcode, item.Name, item.Price.ToString("N0") + " đ", item.Quantity, $"{item.VatPercent}%", itemTotal.ToString("N0") + " đ");
        }

        decimal grandTotal = subTotal + vatTotal;
        lblSubTotalVal.Text = subTotal.ToString("N0") + " VNĐ";
        lblVatVal.Text = vatTotal.ToString("N0") + " VNĐ";
        lblGrandTotal.Text = grandTotal.ToString("N0") + " VNĐ";
    }

    private class CartItem
    {
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal VatPercent { get; set; }
    }
}
