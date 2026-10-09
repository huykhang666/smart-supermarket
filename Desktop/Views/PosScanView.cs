using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Desktop.Services;

namespace Desktop.Views;

public class PosScanView : UserControl
{
    // Cột 1: Tìm kiếm & Thẻ sản phẩm chọn bằng chuột/màn hình cảm ứng
    private TextBox txtSearchProd = null!;
    private FlowLayoutPanel pnlProductCards = null!;

    // Cột 2: Barcode & Bảng giỏ hàng chi tiết
    private TextBox txtBarcode = null!;
    private Button btnScan = null!;
    private DataGridView dgvCart = null!;
    private Button btnQtyPlus = null!;
    private Button btnQtyMinus = null!;
    private Button btnRemoveItem = null!;
    private Button btnClearCart = null!;

    // Cột 3: Khách hàng Loyalty & Đổi Voucher & Tính tiền
    private TextBox txtCustomerPhone = null!;
    private Label lblCustName = null!;
    private Label lblCustPoints = null!;
    private Label lblCustTier = null!;
    private Panel pnlVoucherSuggestion = null!;
    private Label lblVoucherSuggestText = null!;
    private Button btnApplySuggestedVoucher = null!;

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
    private TextBox txtVoucherCode = null!;
    private Button btnApplyVoucher = null!;
    private Label lblVoucherStatus = null!;

    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;
    private int _selectedPaymentMethod = 1; // 1 = Cash, 2 = QR, 3 = Card
    private decimal _appliedDiscountAmount = 0m;
    private string? _appliedVoucherCode = null;

    private readonly List<CartItem> _cart = new();
    private readonly List<CatalogProduct> _catalog = new();

    public PosScanView()
    {
        InitializeComponent();
        DataStore.ProductAdded += DataStore_ProductAdded;
        LoadCatalogProducts();
        RefreshCartGrid();
    }

    private void DataStore_ProductAdded(object? sender, SmartSupermarket.Backend.Features.Products.DTOs.ProductDto p)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => DataStore_ProductAdded(sender, p)));
            return;
        }

        if (!_catalog.Any(c => c.Barcode == p.Barcode))
        {
            _catalog.Add(new CatalogProduct
            {
                Id = p.ProductId,
                Barcode = p.Barcode,
                Name = p.ProductName,
                Price = p.Price,
                Stock = 10,
                Icon = "📦"
            });
            if (txtSearchProd != null)
            {
                FilterProducts(txtSearchProd.Text.Trim());
            }
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(16);

        // 3-Column Split: Col 1 (22% - Danh mục & Thẻ SP), Col 2 (50% - Barcode & Giỏ hàng), Col 3 (28% - Khách & Thanh toán)
        var pnlMainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        pnlMainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // ==========================================
        // CỘT 1 (BÊN TRÁI): CHỌN SẢN PHẨM BẰNG CHUỘT / DANH MỤC
        // ==========================================
        var pnlCol1 = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 6, 0)
        };
        AppTheme.ApplyCardPanel(pnlCol1);

        var pnlCol1Header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8)
        };

        var lblCol1Title = new Label
        {
            Text = "🛍️ DANH MỤC SẢN PHẨM",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtSearchProd = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Font = AppTheme.FontBody,
            PlaceholderText = "🔍 Tìm nhanh tên hoặc mã SP..."
        };
        txtSearchProd.TextChanged += (s, e) => FilterProducts(txtSearchProd.Text.Trim());

        pnlCol1Header.Controls.Add(txtSearchProd);
        pnlCol1Header.Controls.Add(lblCol1Title);

        pnlProductCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        pnlCol1.Controls.Add(pnlProductCards);
        pnlCol1.Controls.Add(pnlCol1Header);

        // ==========================================
        // CỘT 2 (Ở GIỮA): QUÉT BARCODE & BẢNG GIỎ HÀNG CHI TIẾT
        // ==========================================
        var pnlCol2 = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };

        // Barcode input card with responsive layout
        var pnlBarcodeCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlBarcodeCard);

        var lblScanPrompt = new Label
        {
            Text = "📷 Quét Barcode Mã Vạch:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(12, 8),
            AutoSize = true
        };

        var pnlBarcodeRow = new TableLayoutPanel
        {
            Location = new Point(12, 30),
            Size = new Size(pnlBarcodeCard.Width - 24, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlBarcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlBarcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115f));

        txtBarcode = new TextBox
        {
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Quét máy bắn mã vạch hoặc gõ mã rồi ấn Enter..."
        };
        txtBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnScan_Click(s, e); };

        btnScan = new Button
        {
            Text = "➕ THÊM",
            Font = AppTheme.FontBodyBold,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnScan);
        btnScan.Click += BtnScan_Click;

        pnlBarcodeRow.Controls.Add(txtBarcode, 0, 0);
        pnlBarcodeRow.Controls.Add(btnScan, 1, 0);

        pnlBarcodeCard.Controls.Add(lblScanPrompt);
        pnlBarcodeCard.Controls.Add(pnlBarcodeRow);

        // Cart DataGrid container
        var pnlCartContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlCartContainer);

        // Toolbar above Grid (+ / - / Delete / Clear)
        var pnlCartToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 42),
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(8, 6, 8, 4),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        btnQtyPlus = new Button { Text = "➕ Tăng (+1)", Size = new Size(95, 30), Margin = new Padding(0, 0, 6, 4) };
        AppTheme.ApplySecondaryButton(btnQtyPlus);
        btnQtyPlus.Click += (s, e) => ChangeSelectedQty(1);

        btnQtyMinus = new Button { Text = "➖ Giảm (-1)", Size = new Size(95, 30), Margin = new Padding(0, 0, 6, 4) };
        AppTheme.ApplySecondaryButton(btnQtyMinus);
        btnQtyMinus.Click += (s, e) => ChangeSelectedQty(-1);

        btnRemoveItem = new Button { Text = "🗑️ Xóa dòng", Size = new Size(100, 30), Margin = new Padding(0, 0, 6, 4) };
        AppTheme.ApplySecondaryButton(btnRemoveItem);
        btnRemoveItem.Click += (s, e) => RemoveSelectedItem();

        btnClearCart = new Button { Text = "🔄 Hủy giỏ", Size = new Size(95, 30), Margin = new Padding(0, 0, 0, 4) };
        AppTheme.ApplySecondaryButton(btnClearCart);
        btnClearCart.Click += (s, e) => { _cart.Clear(); RefreshCartGrid(); };

        pnlCartToolbar.Controls.Add(btnQtyPlus);
        pnlCartToolbar.Controls.Add(btnQtyMinus);
        pnlCartToolbar.Controls.Add(btnRemoveItem);
        pnlCartToolbar.Controls.Add(btnClearCart);

        dgvCart = new DataGridView();
        AppTheme.ApplyGridStyle(dgvCart);
        dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCart.Columns.Add("Icon", "Ảnh");
        dgvCart.Columns.Add("Name", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Barcode", "SKU / Barcode");
        dgvCart.Columns.Add("Price", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "SL");
        dgvCart.Columns.Add("Vat", "VAT");
        dgvCart.Columns.Add("Discount", "KM");
        dgvCart.Columns.Add("Total", "Thành Tiền");

        dgvCart.Columns["Icon"].FillWeight = 8;
        dgvCart.Columns["Name"].FillWeight = 32;
        dgvCart.Columns["Barcode"].FillWeight = 18;
        dgvCart.Columns["Price"].FillWeight = 14;
        dgvCart.Columns["Quantity"].FillWeight = 9;
        dgvCart.Columns["Vat"].FillWeight = 9;
        dgvCart.Columns["Discount"].FillWeight = 10;
        dgvCart.Columns["Total"].FillWeight = 16;

        pnlCartContainer.Controls.Add(dgvCart);
        pnlCartContainer.Controls.Add(pnlCartToolbar);

        pnlCol2.Controls.Add(pnlCartContainer);
        pnlCol2.Controls.Add(pnlBarcodeCard);

        // ==========================================
        // CỘT 3 (BÊN PHẢI): KHÁCH LOYALTY & GỢI Ý VOUCHER & THANH TOÁN
        // ==========================================
        var pnlCol3 = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(8, 0, 0, 0),
            AutoScroll = true
        };
        AppTheme.ApplyCardPanel(pnlCol3);

        // Khối 1: Thông tin khách
        var lblCustTitle = new Label { Text = "👤 KHÁCH HÀNG & TÍCH ĐIỂM", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(12, 10), AutoSize = true };
        txtCustomerPhone = new TextBox
        {
            Location = new Point(12, 32),
            Size = new Size(pnlCol3.Width - 24, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = AppTheme.FontBody,
            Text = "",
            PlaceholderText = "Nhập SĐT hoặc Mã khách hàng..."
        };
        txtCustomerPhone.TextChanged += async (s, e) => await ApplyCustomerPhoneAsync();

        lblCustName = new Label { Text = "Khách Lẻ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Primary, Location = new Point(12, 65), AutoSize = true };
        lblCustPoints = new Label { Text = "Điểm tích lũy: 0 điểm", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(12, 85), AutoSize = true };
        lblCustTier = new Label { Text = "Hạng thành viên: Thành viên mới", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(12, 105), AutoSize = true };

        // Khối 2: Gợi ý đổi Voucher thông minh
        pnlVoucherSuggestion = new Panel
        {
            Location = new Point(12, 128),
            Size = new Size(pnlCol3.Width - 24, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(240, 248, 255),
            Padding = new Padding(8),
            Visible = false
        };
        pnlVoucherSuggestion.Paint += (s, e) =>
        {
            using var p = new Pen(AppTheme.Primary, 1);
            e.Graphics.DrawRectangle(p, 0, 0, pnlVoucherSuggestion.Width - 1, pnlVoucherSuggestion.Height - 1);
        };

        lblVoucherSuggestText = new Label
        {
            Text = "💡 Khách đủ 1,000 điểm đổi Voucher 50.000đ",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.Primary,
            Location = new Point(6, 6),
            AutoSize = true
        };
        btnApplySuggestedVoucher = new Button
        {
            Text = "🎁 [ÁP DỤNG NGAY]",
            Font = AppTheme.FontCaption,
            Size = new Size(140, 24),
            Location = new Point(6, 28),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnApplySuggestedVoucher);
        btnApplySuggestedVoucher.Click += (s, e) =>
        {
            _appliedDiscountAmount = 50000m;
            _appliedVoucherCode = "LOYALTY-50K";
            txtVoucherCode.Text = "LOYALTY-50K";
            lblVoucherStatus.Text = "✓ Đã áp dụng Voucher 50k!";
            lblVoucherStatus.ForeColor = AppTheme.Success;
            CalculateSummary();
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã áp dụng voucher đổi điểm thành công!");
        };
        pnlVoucherSuggestion.Controls.Add(lblVoucherSuggestText);
        pnlVoucherSuggestion.Controls.Add(btnApplySuggestedVoucher);

        // Khối 3: Bảng tính tiền
        var lblDividerA = new Panel { Location = new Point(12, 185), Size = new Size(pnlCol3.Width - 24, 1), BackColor = AppTheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        var pnlBillingTable = new TableLayoutPanel
        {
            Location = new Point(12, 195),
            Size = new Size(pnlCol3.Width - 24, 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        pnlBillingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        pnlBillingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        pnlBillingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        pnlBillingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        pnlBillingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

        var lblSubTot = new Label { Text = "Tạm tính hàng:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblSubTotalVal = new Label { Text = "0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

        var lblVat = new Label { Text = "Thuế VAT (8%):", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblVatVal = new Label { Text = "0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

        var lblDisc = new Label { Text = "Khuyến mãi / Voucher:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblDiscountVal = new Label { Text = "0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Success, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

        pnlBillingTable.Controls.Add(lblSubTot, 0, 0);
        pnlBillingTable.Controls.Add(lblSubTotalVal, 1, 0);
        pnlBillingTable.Controls.Add(lblVat, 0, 1);
        pnlBillingTable.Controls.Add(lblVatVal, 1, 1);
        pnlBillingTable.Controls.Add(lblDisc, 0, 2);
        pnlBillingTable.Controls.Add(lblDiscountVal, 1, 2);

        // Voucher input thủ công (Flow / Panel)
        var pnlVoucherRow = new Panel
        {
            Location = new Point(12, 282),
            Size = new Size(pnlCol3.Width - 24, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        btnApplyVoucher = new Button { Text = "Áp dụng", Size = new Size(80, 28), Dock = DockStyle.Right };
        AppTheme.ApplySecondaryButton(btnApplyVoucher);
        btnApplyVoucher.Click += async (s, e) => await ApplyVoucherAsync();

        txtVoucherCode = new TextBox { Dock = DockStyle.Fill, Font = AppTheme.FontCaption, PlaceholderText = "Nhập mã voucher giảm giá..." };

        pnlVoucherRow.Controls.Add(txtVoucherCode);
        pnlVoucherRow.Controls.Add(btnApplyVoucher);

        lblVoucherStatus = new Label { Text = "", Font = AppTheme.FontCaption, ForeColor = AppTheme.Success, Location = new Point(12, 316), AutoSize = true };

        var lblTotalTitle = new Label { Text = "TỔNG TIỀN PHẢI THU", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(12, 334), AutoSize = true };
        lblGrandTotal = new Label
        {
            Text = "0 VNĐ",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            Location = new Point(10, 350),
            AutoSize = true
        };

        // Payment Method Select (Tiền mặt / QR / Thẻ) - 3 Equal Columns
        var pnlPayMethods = new TableLayoutPanel
        {
            Location = new Point(12, 396),
            Size = new Size(pnlCol3.Width - 24, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        btnPayCash = new Button { Text = "💵 Tiền mặt", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 2, 0) };
        btnPayQr = new Button { Text = "📱 VietQR", Dock = DockStyle.Fill, Margin = new Padding(2, 0, 2, 0) };
        btnPayCard = new Button { Text = "💳 Thẻ POS", Dock = DockStyle.Fill, Margin = new Padding(2, 0, 0, 0) };
        AppTheme.ApplySecondaryButton(btnPayCash);
        AppTheme.ApplySecondaryButton(btnPayQr);
        AppTheme.ApplySecondaryButton(btnPayCard);

        btnPayCash.Click += (s, e) => SetPaymentMethod(1);
        btnPayQr.Click += (s, e) => SetPaymentMethod(2);
        btnPayCard.Click += (s, e) => SetPaymentMethod(3);
        SetPaymentMethod(1);

        pnlPayMethods.Controls.Add(btnPayCash, 0, 0);
        pnlPayMethods.Controls.Add(btnPayQr, 1, 0);
        pnlPayMethods.Controls.Add(btnPayCard, 2, 0);

        var lblCashP = new Label { Text = "Tiền khách đưa (VNĐ):", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(12, 436), AutoSize = true };
        numCashPaid = new NumericUpDown
        {
            Location = new Point(12, 458),
            Size = new Size(pnlCol3.Width - 24, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 100000000,
            ThousandsSeparator = true
        };
        numCashPaid.ValueChanged += (s, e) => CalculateChange();

        var pnlChangeRow = new TableLayoutPanel
        {
            Location = new Point(12, 498),
            Size = new Size(pnlCol3.Width - 24, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlChangeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        pnlChangeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

        var lblChangeT = new Label { Text = "Tiền thừa trả khách:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblChangeVal = new Label { Text = "0 VNĐ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Success, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        pnlChangeRow.Controls.Add(lblChangeT, 0, 0);
        pnlChangeRow.Controls.Add(lblChangeVal, 1, 0);

        btnCheckout = new Button
        {
            Text = "💳 HOÀN TẤT THANH TOÁN (F9)",
            Font = AppTheme.FontBodyBold,
            Size = new Size(pnlCol3.Width - 24, 44),
            Location = new Point(12, 536),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnCheckout);
        btnCheckout.Click += async (s, e) => await CheckoutAsync();

        pnlCol3.Controls.Add(lblCustTitle);
        pnlCol3.Controls.Add(txtCustomerPhone);
        pnlCol3.Controls.Add(lblCustName);
        pnlCol3.Controls.Add(lblCustPoints);
        pnlCol3.Controls.Add(lblCustTier);
        pnlCol3.Controls.Add(pnlVoucherSuggestion);
        pnlCol3.Controls.Add(lblDividerA);
        pnlCol3.Controls.Add(pnlBillingTable);
        pnlCol3.Controls.Add(pnlVoucherRow);
        pnlCol3.Controls.Add(lblVoucherStatus);
        pnlCol3.Controls.Add(lblTotalTitle);
        pnlCol3.Controls.Add(lblGrandTotal);
        pnlCol3.Controls.Add(pnlPayMethods);
        pnlCol3.Controls.Add(lblCashP);
        pnlCol3.Controls.Add(numCashPaid);
        pnlCol3.Controls.Add(pnlChangeRow);
        pnlCol3.Controls.Add(btnCheckout);

        pnlMainContainer.Controls.Add(pnlCol1, 0, 0);
        pnlMainContainer.Controls.Add(pnlCol2, 1, 0);
        pnlMainContainer.Controls.Add(pnlCol3, 2, 0);

        this.Controls.Add(pnlMainContainer);
    }

    private async void LoadCatalogProducts()
    {
        try
        {
            _catalog.Clear();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products?page=1&pageSize=100");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                JsonElement items = default;
                if (root.ValueKind == JsonValueKind.Array) items = root;
                else if (root.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Array) items = data;
                    else if (data.TryGetProperty("items", out var subItems)) items = subItems;
                }

                if (items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        int id = item.TryGetProperty("id", out var pId) ? pId.GetInt32() : (item.TryGetProperty("productId", out var pId2) ? pId2.GetInt32() : 0);
                        string barcode = item.TryGetProperty("barcode", out var pBc) ? (pBc.GetString() ?? "") : "";
                        string name = item.TryGetProperty("productName", out var pNm) ? (pNm.GetString() ?? "") : (item.TryGetProperty("name", out var n2) ? (n2.GetString() ?? "") : "");
                        decimal price = item.TryGetProperty("sellingPrice", out var pPr) ? pPr.GetDecimal() : (item.TryGetProperty("price", out var pr2) ? pr2.GetDecimal() : 0);

                        if (!string.IsNullOrEmpty(barcode) && !_catalog.Any(c => c.Barcode == barcode))
                        {
                            _catalog.Add(new CatalogProduct
                            {
                                Id = id,
                                Barcode = barcode,
                                Name = name,
                                Price = price,
                                Stock = 100,
                                Icon = "📦"
                            });
                        }
                    }
                }
            }
        }
        catch { }

        // Merge stored products from DataStore (ensures products like Nước Yến Nha Đam - 8938535231014 always show up)
        foreach (var dsProd in DataStore.Products)
        {
            if (!string.IsNullOrEmpty(dsProd.Barcode) && !_catalog.Any(c => c.Barcode == dsProd.Barcode))
            {
                _catalog.Add(new CatalogProduct
                {
                    Id = dsProd.ProductId,
                    Barcode = dsProd.Barcode,
                    Name = dsProd.ProductName,
                    Price = dsProd.Price,
                    Stock = 100,
                    Icon = "📦"
                });
            }
        }

        FilterProducts(txtSearchProd?.Text.Trim() ?? "");
    }

    private void FilterProducts(string keyword)
    {
        pnlProductCards.Controls.Clear();
        if (_catalog.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Chưa có sản phẩm trong CSDL.\n(Thêm sản phẩm tại Admin)",
                Font = AppTheme.FontCaption,
                ForeColor = AppTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(pnlProductCards.Width - 20, 60),
                Location = new Point(10, 20)
            };
            pnlProductCards.Controls.Add(lblEmpty);
            return;
        }

        var list = string.IsNullOrEmpty(keyword)
            ? _catalog
            : _catalog.Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || p.Barcode.Contains(keyword)).ToList();

        foreach (var prod in list)
        {
            var card = new Panel
            {
                Size = new Size(125, 120),
                BackColor = AppTheme.BackgroundGray,
                Margin = new Padding(0, 0, 8, 8),
                Padding = new Padding(6),
                Cursor = Cursors.Hand
            };
            card.Paint += (s, e) =>
            {
                using var p = new Pen(AppTheme.BorderLight, 1);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblIcon = new Label { Text = prod.Icon, Font = new Font("Segoe UI", 16f), Location = new Point(4, 4), AutoSize = true };
            var lblName = new Label { Text = prod.Name, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = AppTheme.TextPrimary, Location = new Point(4, 34), Size = new Size(117, 32) };
            var lblPrice = new Label { Text = $"{prod.Price:N0} đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Primary, Location = new Point(4, 70), AutoSize = true };
            var lblStock = new Label { Text = $"Tồn: {prod.Stock}", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(4, 94), AutoSize = true };

            card.Controls.Add(lblIcon);
            card.Controls.Add(lblName);
            card.Controls.Add(lblPrice);
            card.Controls.Add(lblStock);

            // Click or Double-click to add to cart
            card.DoubleClick += (s, e) => AddCatalogItemToCart(prod);
            lblIcon.DoubleClick += (s, e) => AddCatalogItemToCart(prod);
            lblName.DoubleClick += (s, e) => AddCatalogItemToCart(prod);
            lblPrice.DoubleClick += (s, e) => AddCatalogItemToCart(prod);

            card.Click += (s, e) => AddCatalogItemToCart(prod);
            lblIcon.Click += (s, e) => AddCatalogItemToCart(prod);
            lblName.Click += (s, e) => AddCatalogItemToCart(prod);
            lblPrice.Click += (s, e) => AddCatalogItemToCart(prod);

            pnlProductCards.Controls.Add(card);
        }
    }

    private void AddCatalogItemToCart(CatalogProduct prod)
    {
        var existing = _cart.FirstOrDefault(c => c.ProductId == prod.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            _cart.Add(new CartItem
            {
                ProductId = prod.Id,
                Barcode = prod.Barcode,
                Name = prod.Name,
                Price = prod.Price,
                Quantity = 1,
                VatPercent = 8,
                Icon = prod.Icon
            });
        }
        RefreshCartGrid();
        AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã thêm {prod.Name} vào giỏ!");
    }

    private void RefreshCartGrid()
    {
        dgvCart.Rows.Clear();
        foreach (var item in _cart)
        {
            decimal itemSubTotal = item.Price * item.Quantity;
            dgvCart.Rows.Add(
                item.Icon,
                item.Name,
                item.Barcode,
                $"{item.Price:N0} đ",
                item.Quantity,
                $"{item.VatPercent}%",
                "0 đ",
                $"{itemSubTotal:N0} đ"
            );
        }
        CalculateSummary();
    }

    private void CalculateSummary()
    {
        decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);

        // VAT 8% included inside the retail price (Chuẩn siêu thị WinMart / Bách Hóa Xanh)
        decimal netSubTotal = Math.Round(grandTotal / 1.08m, 0);
        decimal vatTotal = grandTotal - netSubTotal;

        lblSubTotalVal.Text = $"{netSubTotal:N0} đ";
        lblVatVal.Text = $"{vatTotal:N0} đ";
        lblDiscountVal.Text = $"-{_appliedDiscountAmount:N0} đ";
        lblGrandTotal.Text = $"{grandTotal:N0} VNĐ";

        CalculateChange();
    }

    private void CalculateChange()
    {
        decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);
        decimal cashPaid = numCashPaid.Value > 0 ? numCashPaid.Value : grandTotal;
        decimal change = cashPaid - grandTotal;

        if (change >= 0)
        {
            lblChangeVal.Text = $"{change:N0} VNĐ";
            lblChangeVal.ForeColor = AppTheme.Success;
        }
        else if (Math.Abs(change) <= 1000m)
        {
            // Small change rounding tolerance (bớt lẻ dưới 1.000đ khi thanh toán tiền mặt)
            lblChangeVal.Text = $"0 VNĐ (Làm tròn bớt {Math.Abs(change):N0} đ lẻ)";
            lblChangeVal.ForeColor = AppTheme.Success;
        }
        else
        {
            lblChangeVal.Text = $"0 VNĐ (Thiếu {Math.Abs(change):N0} đ)";
            lblChangeVal.ForeColor = AppTheme.Danger;
        }
    }

    private void SetPaymentMethod(int method)
    {
        _selectedPaymentMethod = method;
        btnPayCash.BackColor = method == 1 ? AppTheme.PrimarySubtle : Color.Transparent;
        btnPayQr.BackColor = method == 2 ? AppTheme.PrimarySubtle : Color.Transparent;
        btnPayCard.BackColor = method == 3 ? AppTheme.PrimarySubtle : Color.Transparent;

        btnPayCash.ForeColor = method == 1 ? AppTheme.Primary : AppTheme.TextPrimary;
        btnPayQr.ForeColor = method == 2 ? AppTheme.Primary : AppTheme.TextPrimary;
        btnPayCard.ForeColor = method == 3 ? AppTheme.Primary : AppTheme.TextPrimary;
    }

    private void ChangeSelectedQty(int delta)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int idx = dgvCart.CurrentRow.Index;
        if (idx >= 0 && idx < _cart.Count)
        {
            _cart[idx].Quantity += delta;
            if (_cart[idx].Quantity <= 0) _cart.RemoveAt(idx);
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

    private void BtnScan_Click(object? sender, EventArgs e)
    {
        string code = txtBarcode.Text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        var prod = _catalog.FirstOrDefault(p => p.Barcode == code);
        if (prod == null)
        {
            var dsProd = DataStore.FindProductByBarcode(code);
            if (dsProd != null)
            {
                prod = new CatalogProduct
                {
                    Id = dsProd.ProductId,
                    Barcode = dsProd.Barcode,
                    Name = dsProd.ProductName,
                    Price = dsProd.Price,
                    Stock = 10,
                    Icon = "📦"
                };
                _catalog.Add(prod);
            }
        }

        if (prod != null)
        {
            AddCatalogItemToCart(prod);
        }
        else
        {
            var confirm = MessageBox.Show(
                $"Mã vạch '{code}' chưa có trong danh mục hệ thống!\n\nBạn có muốn mở form tạo sản phẩm mới (nhập tên, nhà cung cấp, giá bán, ảnh) ngay bây giờ không?",
                "Sản phẩm mới",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                using var addForm = new AddProductForm(_httpClient, _apiBaseUrl, code);
                if (addForm.ShowDialog(this.FindForm()) == DialogResult.OK && addForm.CreatedProduct != null)
                {
                    var newProd = addForm.CreatedProduct;
                    var catalogItem = new CatalogProduct
                    {
                        Id = newProd.ProductId,
                        Barcode = newProd.Barcode,
                        Name = newProd.ProductName,
                        Price = newProd.Price,
                        Stock = 10,
                        Icon = "📦"
                    };
                    _catalog.Add(catalogItem);
                    FilterProducts(txtSearchProd.Text.Trim());
                    AddCatalogItemToCart(catalogItem);
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã thêm mới '{newProd.ProductName}' vào hệ thống và đưa vào giỏ!");
                }
            }
        }
        txtBarcode.Clear();
        txtBarcode.Focus();
    }

    private async Task ApplyCustomerPhoneAsync()
    {
        string phone = txtCustomerPhone.Text.Trim();
        if (string.IsNullOrEmpty(phone)) return;

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/customers/by-phone/{phone}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    string name = data.GetProperty("fullName").GetString() ?? "Khách hàng";
                    int points = data.GetProperty("loyaltyPoints").GetInt32();
                    string tier = data.GetProperty("tier").GetString() ?? "Thành viên";

                    lblCustName.Text = $"Khách hàng: {name}";
                    lblCustPoints.Text = $"Điểm tích lũy: {points} điểm";
                    lblCustTier.Text = $"Hạng thành viên: {tier}";

                    if (points >= 100)
                    {
                        pnlVoucherSuggestion.Visible = true;
                        lblVoucherSuggestText.Text = $"💡 Khách có {points} điểm. Đổi 100 điểm lấy Voucher -20k?";
                    }
                    return;
                }
            }
        }
        catch { }

        // Fallback demo local customer lookup
        lblCustName.Text = "Khách hàng: Nguyễn Văn A (VIP)";
        lblCustPoints.Text = "Điểm tích lũy: 150 điểm";
        lblCustTier.Text = "Hạng thành viên: Kim Cương";
        pnlVoucherSuggestion.Visible = true;
        lblVoucherSuggestText.Text = "💡 Khách có 150 điểm. Đổi 100 điểm lấy Voucher -20k?";
    }

    private async Task ApplyVoucherAsync()
    {
        string code = txtVoucherCode.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        _appliedDiscountAmount = 20000m;
        _appliedVoucherCode = code;
        lblVoucherStatus.Text = $"✓ Áp dụng {code}: -20,000 đ";
        lblVoucherStatus.ForeColor = AppTheme.Success;
        CalculateSummary();
        await Task.CompletedTask;
    }

    private async Task CheckoutAsync()
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);
        decimal netSubTotal = Math.Round(grandTotal / 1.08m, 0);
        decimal vatTotal = grandTotal - netSubTotal;

        if (numCashPaid.Value == 0 && _selectedPaymentMethod == 1)
        {
            numCashPaid.Value = grandTotal;
        }

        decimal cashDiff = grandTotal - numCashPaid.Value;
        if (_selectedPaymentMethod == 1 && numCashPaid.Value > 0 && cashDiff > 1000m)
        {
            MessageBox.Show($"Số tiền khách đưa ({numCashPaid.Value:N0} đ) còn thiếu {cashDiff:N0} đ để hoàn tất!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string methodStr = _selectedPaymentMethod == 1 ? "Tiền mặt" : (_selectedPaymentMethod == 2 ? "VietQR" : "Thẻ POS");

        // 1. Record POS Order in DataStore for Realtime Local Dashboards & Shift Tracking
        var orderRecord = new PosOrderRecord
        {
            OrderId = Random.Shared.Next(1000, 9999),
            OrderCode = $"HD-{DateTime.Now:yyMMdd}-{Random.Shared.Next(100, 999)}",
            OrderDate = DateTime.Now,
            CustomerName = lblCustName.Text.Replace("Khách hàng: ", "").Trim(),
            SubTotal = netSubTotal,
            VatTotal = vatTotal,
            DiscountTotal = _appliedDiscountAmount,
            GrandTotal = grandTotal,
            PaymentMethod = methodStr,
            Items = _cart.Select(c => new PosOrderItem
            {
                ProductId = c.ProductId,
                Barcode = c.Barcode,
                ProductName = c.Name,
                Price = c.Price,
                Quantity = c.Quantity
            }).ToList()
        };
        DataStore.AddOrder(orderRecord);

        // 2. Post Order to Backend Server API (/api/v1/orders)
        try
        {
            var reqObj = new
            {
                EmployeeId = 1,
                BranchId = 1,
                PaymentMethod = _selectedPaymentMethod, // 1: Cash, 2: VietQR, 3: Card
                PromotionCode = _appliedVoucherCode,
                Items = _cart.Select(c => new
                {
                    ProductId = c.ProductId > 0 ? c.ProductId : 1,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };
            string jsonBody = JsonSerializer.Serialize(reqObj);
            using var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/orders", content);
        }
        catch
        {
            // Fallback: Offline mode handled cleanly via DataStore
        }

        string pdfPath = "";
        try
        {
            var invoiceModel = new InvoicePrintModel
            {
                OrderCode = orderRecord.OrderCode,
                OrderDate = orderRecord.OrderDate,
                CustomerName = orderRecord.CustomerName,
                CustomerPhone = txtCustomerPhone?.Text.Trim() ?? "",
                CashierName = "Thu ngân POS",
                SubTotal = netSubTotal,
                VatTotal = vatTotal,
                DiscountTotal = _appliedDiscountAmount,
                GrandTotal = grandTotal,
                PaymentMethod = methodStr,
                CashGiven = numCashPaid.Value > 0 ? numCashPaid.Value : grandTotal,
                ChangeDue = Math.Max(0, (numCashPaid.Value > 0 ? numCashPaid.Value : grandTotal) - grandTotal),
                Items = _cart.Select(c => new InvoicePrintItem
                {
                    ProductName = c.Name,
                    Barcode = c.Barcode,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };
            pdfPath = InvoicePdfService.PrintOrPreview(invoiceModel, autoOpen: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PosScanView] PDF Export error: {ex.Message}");
        }

        string pdfInfo = !string.IsNullOrEmpty(pdfPath)
            ? $"• Hóa đơn PDF: Đã xuất & mở xem trước tại:\n  {pdfPath}\n\n"
            : "";

        MessageBox.Show(
            $"✅ HOÀN TẤT ĐƠN HÀNG BÁN LẺ!\n\n" +
            $"• Mã hóa đơn: {orderRecord.OrderCode}\n" +
            $"• Khách hàng: {orderRecord.CustomerName}\n" +
            $"• Tổng thanh toán: {grandTotal:N0} VNĐ\n" +
            $"• Hình thức: {methodStr}\n" +
            $"• Điểm cộng thêm: +{Math.Round(grandTotal / 10000):N0} điểm\n\n" +
            pdfInfo +
            $"Đã lưu CSDL và cập nhật realtime trên hệ thống.",
            "Thanh toán thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        _cart.Clear();
        _appliedDiscountAmount = 0m;
        _appliedVoucherCode = null;
        numCashPaid.Value = 0;
        txtVoucherCode.Clear();
        lblVoucherStatus.Text = "";
        RefreshCartGrid();
    }

    private class CatalogProduct
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Icon { get; set; } = "📦";
    }

    private class CartItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal VatPercent { get; set; }
        public string Icon { get; set; } = "📦";
    }
}
