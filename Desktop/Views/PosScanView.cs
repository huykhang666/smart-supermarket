using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop.Forms;
using Desktop.Models;
using Desktop.Services;

namespace Desktop.Views;

public class PosScanView : UserControl
{
    // Cột 1: Danh mục & Thẻ sản phẩm
    private TextBox txtSearchProd = null!;
    private FlowLayoutPanel pnlProductCards = null!;

    // Cột 2: Barcode & Bảng giỏ hàng & Toolbar
    private TextBox txtBarcode = null!;
    private Button btnScan = null!;
    private DataGridView dgvCart = null!;
    private Button btnQtyPlus = null!;
    private Button btnQtyMinus = null!;
    private Button btnRemoveItem = null!;
    private Button btnClearCart = null!;
    private Button btnParkOrder = null!;
    private Button btnResumeParked = null!;
    private Button btnReturnOrder = null!;
    private Button btnPrintThermal = null!;

    // Cột 3: Khách hàng Loyalty & Voucher & Tính tiền
    private Panel pnlCol3 = null!;
    private Label lblCustTitle = null!;
    private TextBox txtCustomerPhone = null!;
    private Label lblCustName = null!;
    private Label lblCustPoints = null!;
    private Label lblCustTier = null!;
    private Panel pnlVoucherSuggestion = null!;
    private Label lblVoucherSuggestText = null!;
    private Button btnApplySuggestedVoucher = null!;
    private Panel lblDividerA = null!;
    private TableLayoutPanel pnlBillingTable = null!;
    private Label lblSubTotalVal = null!;
    private Label lblVatVal = null!;
    private Label lblDiscountVal = null!;
    private TableLayoutPanel pnlVoucherRow = null!;
    private TextBox txtVoucherCode = null!;
    private Button btnApplyVoucher = null!;
    private Label lblVoucherStatus = null!;
    private Button btnPickPromotion = null!;
    private Panel lblDividerB = null!;
    private Label lblTotalTitle = null!;
    private Label lblGrandTotal = null!;
    private TableLayoutPanel pnlPayMethods = null!;
    private Button btnPayCash = null!;
    private Button btnPayCard = null!;
    private Button btnPayQr = null!;
    private Label lblCashP = null!;
    private NumericUpDown numCashPaid = null!;
    private TableLayoutPanel pnlChangeRow = null!;
    private Label lblChangeVal = null!;
    private Button btnCheckout = null!;
    private bool _isProcessingCheckout = false;

    private readonly PaymentService _paymentService = new();
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;
    private int _selectedPaymentMethod = 1; // 1 = Cash, 2 = QR, 3 = Card
    private decimal _appliedDiscountAmount = 0m;
    private string? _appliedVoucherCode = null;

    private readonly List<CartItem> _cart = new();
    private readonly List<CatalogProduct> _catalog = new();
    private readonly List<ParkedOrder> _parkedOrders = new();
    private InvoicePrintModel? _lastInvoiceModel = null;

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
                Stock = 10
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
        this.Padding = new Padding(10);

        // 3-Column Split: Col 1 (300px - Danh mục SP), Col 2 (DÃN RỘNG TOÀN BỘ), Col 3 (390px - Hóa đơn)
        var pnlMainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlMainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390f));
        pnlMainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // ==========================================
        // CỘT 1 (BÊN TRÁI): CHỌN SẢN PHẨM / DANH MỤC
        // ==========================================
        var pnlCol1 = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 6, 0)
        };
        AppTheme.ApplyCardPanel(pnlCol1);

        var pnlCol1Header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.Transparent,
            Padding = new Padding(2, 0, 2, 6)
        };

        var lblCol1Title = new Label
        {
            Text = "DANH MỤC SẢN PHẨM",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtSearchProd = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Font = AppTheme.FontBody,
            PlaceholderText = "Tìm tên hoặc mã barcode..."
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
            Padding = new Padding(0, 4, 0, 0)
        };
        pnlProductCards.Resize += (s, e) => FilterProducts(txtSearchProd?.Text.Trim() ?? "");

        pnlCol1.Controls.Add(pnlProductCards);
        pnlCol1.Controls.Add(pnlCol1Header);

        // ==========================================
        // CỘT 2 (Ở GIỮA): QUÉT BARCODE & GIỎ HÀNG & TOOLBAR
        // ==========================================
        var pnlCol2 = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };

        var pnlBarcodeCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlBarcodeCard);

        var lblScanPrompt = new Label
        {
            Text = "Quét Barcode Mã Vạch:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(12, 6),
            AutoSize = true
        };

        var pnlBarcodeRow = new TableLayoutPanel
        {
            Location = new Point(12, 28),
            Size = new Size(pnlBarcodeCard.Width - 24, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlBarcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlBarcodeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));

        txtBarcode = new TextBox
        {
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Quét máy bắn mã vạch hoặc gõ mã rồi ấn Enter..."
        };
        txtBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnScan_Click(s, e); };

        btnScan = new Button
        {
            Text = "THÊM",
            Font = AppTheme.FontBodyBold,
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnScan, radius: 5);
        btnScan.Padding = new Padding(0);
        btnScan.Click += BtnScan_Click;

        pnlBarcodeRow.Controls.Add(txtBarcode, 0, 0);
        pnlBarcodeRow.Controls.Add(btnScan, 1, 0);

        pnlBarcodeCard.Controls.Add(lblScanPrompt);
        pnlBarcodeCard.Controls.Add(pnlBarcodeRow);

        var pnlCartContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlCartContainer);

        // Toolbar 2 hàng (Row 0: Thao tác giỏ, Row 1: Nghiệp vụ POS)
        var pnlCartToolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(4, 3, 4, 3),
            ColumnCount = 4,
            RowCount = 2
        };
        pnlCartToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCartToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCartToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCartToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCartToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        pnlCartToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        // Row 0: Thao tác giỏ
        btnQtyPlus = new Button { Text = "+ Tăng (+1)", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplyPrimaryButton(btnQtyPlus, radius: 4);
        btnQtyPlus.Padding = new Padding(0);
        btnQtyPlus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnQtyPlus.Click += (s, e) => ChangeSelectedQty(1);

        btnQtyMinus = new Button { Text = "- Giảm (-1)", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnQtyMinus, radius: 4);
        btnQtyMinus.Padding = new Padding(0);
        btnQtyMinus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnQtyMinus.Click += (s, e) => ChangeSelectedQty(-1);

        btnRemoveItem = new Button { Text = "Xóa dòng", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplyDangerButton(btnRemoveItem, radius: 4);
        btnRemoveItem.Padding = new Padding(0);
        btnRemoveItem.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnRemoveItem.Click += (s, e) => RemoveSelectedItem();

        btnClearCart = new Button { Text = "Hủy cả giỏ", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnClearCart, radius: 4);
        btnClearCart.Padding = new Padding(0);
        btnClearCart.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnClearCart.Click += (s, e) => ClearCartWithAuth();

        // Row 1: Nghiệp vụ POS F6, F7, K80
        btnParkOrder = new Button { Text = "Tạm giữ (F6)", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnParkOrder, radius: 4);
        btnParkOrder.Padding = new Padding(0);
        btnParkOrder.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnParkOrder.Click += (s, e) => ParkCurrentOrder();

        btnResumeParked = new Button { Text = "Đơn treo (0)", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnResumeParked, radius: 4);
        btnResumeParked.Padding = new Padding(0);
        btnResumeParked.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnResumeParked.Click += (s, e) => OpenParkedOrdersDialog();

        btnReturnOrder = new Button { Text = "Đổi/Trả (F7)", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnReturnOrder, radius: 4);
        btnReturnOrder.Padding = new Padding(0);
        btnReturnOrder.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnReturnOrder.Click += (s, e) => OpenReturnRefundDialog();

        btnPrintThermal = new Button { Text = "In Bill K80", Dock = DockStyle.Fill, Margin = new Padding(2), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        AppTheme.ApplySecondaryButton(btnPrintThermal, radius: 4);
        btnPrintThermal.Padding = new Padding(0);
        btnPrintThermal.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnPrintThermal.Click += (s, e) => PrintThermalBillDirect();

        pnlCartToolbar.Controls.Add(btnQtyPlus, 0, 0);
        pnlCartToolbar.Controls.Add(btnQtyMinus, 1, 0);
        pnlCartToolbar.Controls.Add(btnRemoveItem, 2, 0);
        pnlCartToolbar.Controls.Add(btnClearCart, 3, 0);

        pnlCartToolbar.Controls.Add(btnParkOrder, 0, 1);
        pnlCartToolbar.Controls.Add(btnResumeParked, 1, 1);
        pnlCartToolbar.Controls.Add(btnReturnOrder, 2, 1);
        pnlCartToolbar.Controls.Add(btnPrintThermal, 3, 1);

        dgvCart = new DataGridView();
        AppTheme.ApplyGridStyle(dgvCart);
        dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCart.Columns.Add("Name", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Barcode", "Mã Barcode");
        dgvCart.Columns.Add("Price", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "SL");
        dgvCart.Columns.Add("Vat", "VAT");
        dgvCart.Columns.Add("Discount", "KM");
        dgvCart.Columns.Add("Total", "Thành Tiền");

        dgvCart.Columns["Name"].FillWeight = 30;
        dgvCart.Columns["Name"].MinimumWidth = 120;
        dgvCart.Columns["Barcode"].FillWeight = 16;
        dgvCart.Columns["Barcode"].MinimumWidth = 85;
        dgvCart.Columns["Price"].FillWeight = 15;
        dgvCart.Columns["Price"].MinimumWidth = 80;
        dgvCart.Columns["Quantity"].FillWeight = 8;
        dgvCart.Columns["Quantity"].MinimumWidth = 45;
        dgvCart.Columns["Vat"].FillWeight = 8;
        dgvCart.Columns["Vat"].MinimumWidth = 45;
        dgvCart.Columns["Discount"].FillWeight = 8;
        dgvCart.Columns["Discount"].MinimumWidth = 45;
        dgvCart.Columns["Total"].FillWeight = 18;
        dgvCart.Columns["Total"].MinimumWidth = 90;

        pnlCartContainer.Controls.Add(dgvCart);
        pnlCartContainer.Controls.Add(pnlCartToolbar);

        pnlCol2.Controls.Add(pnlCartContainer);
        pnlCol2.Controls.Add(pnlBarcodeCard);

        // ==========================================
        // CỘT 3 (BÊN PHẢI): KHÁCH HÀNG & BẢNG TÍNH TIỀN (Stacked Layout)
        // ==========================================
        pnlCol3 = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(14),
            Margin = new Padding(6, 0, 0, 0),
            AutoScroll = true
        };
        AppTheme.ApplyCardPanel(pnlCol3);

        lblCustTitle = new Label { Text = "KHÁCH HÀNG & TÍCH ĐIỂM", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, AutoSize = true };
        txtCustomerPhone = new TextBox
        {
            Height = 32,
            Font = AppTheme.FontBody,
            PlaceholderText = "Nhập SĐT hoặc Mã khách hàng..."
        };
        txtCustomerPhone.TextChanged += async (s, e) => await ApplyCustomerPhoneAsync();

        lblCustName = new Label { Text = "Khách Lẻ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Primary, AutoSize = true };
        lblCustPoints = new Label { Text = "Điểm tích lũy: 0 điểm", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, AutoSize = true };
        lblCustTier = new Label { Text = "Hạng thành viên: Thành viên mới", Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, AutoSize = true };

        pnlVoucherSuggestion = new Panel
        {
            Height = 58,
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
            Text = "Khách đủ điểm đổi Voucher giảm giá!",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.Primary,
            Location = new Point(8, 6),
            AutoSize = true
        };
        btnApplySuggestedVoucher = new Button
        {
            Text = "Áp Dụng Ngay",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Size = new Size(130, 24),
            Location = new Point(8, 26),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnApplySuggestedVoucher, radius: 4);
        btnApplySuggestedVoucher.Padding = new Padding(0);
        btnApplySuggestedVoucher.Click += (s, e) =>
        {
            _appliedDiscountAmount = 50000m;
            _appliedVoucherCode = "LOYALTY-50K";
            txtVoucherCode.Text = "LOYALTY-50K";
            lblVoucherStatus.Text = "✓ Đã áp dụng Voucher 50k!";
            lblVoucherStatus.ForeColor = AppTheme.Success;
            CalculateSummary();
            RelayoutCol3();
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã áp dụng voucher đổi điểm thành công!");
        };
        pnlVoucherSuggestion.Controls.Add(lblVoucherSuggestText);
        pnlVoucherSuggestion.Controls.Add(btnApplySuggestedVoucher);

        lblDividerA = new Panel { Height = 1, BackColor = AppTheme.BorderLight };

        pnlBillingTable = new TableLayoutPanel
        {
            Height = 78,
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
        lblDiscountVal = new Label { Text = "-0 đ", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.Success, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

        pnlBillingTable.Controls.Add(lblSubTot, 0, 0);
        pnlBillingTable.Controls.Add(lblSubTotalVal, 1, 0);
        pnlBillingTable.Controls.Add(lblVat, 0, 1);
        pnlBillingTable.Controls.Add(lblVatVal, 1, 1);
        pnlBillingTable.Controls.Add(lblDisc, 0, 2);
        pnlBillingTable.Controls.Add(lblDiscountVal, 1, 2);

        pnlVoucherRow = new TableLayoutPanel
        {
            Height = 34,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlVoucherRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlVoucherRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95f));

        txtVoucherCode = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBody,
            PlaceholderText = "Nhập mã voucher..."
        };

        btnApplyVoucher = new Button
        {
            Text = "Áp Dụng",
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold,
            Margin = new Padding(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnApplyVoucher, radius: 5);
        btnApplyVoucher.Padding = new Padding(0);
        btnApplyVoucher.Click += async (s, e) => await ApplyVoucherAsync();

        pnlVoucherRow.Controls.Add(txtVoucherCode, 0, 0);
        pnlVoucherRow.Controls.Add(btnApplyVoucher, 1, 0);

        lblVoucherStatus = new Label { Text = "", Font = AppTheme.FontCaption, ForeColor = AppTheme.Success, AutoSize = true, Visible = false };

        btnPickPromotion = new Button
        {
            Text = "Xem Khuyến Mãi / Combo",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Height = 32,
            Cursor = Cursors.Hand
        };
        AppTheme.ApplySecondaryButton(btnPickPromotion, radius: 5);
        btnPickPromotion.Padding = new Padding(0);
        btnPickPromotion.Click += async (s, e) => await ShowActivePromotionsPickerAsync();

        lblDividerB = new Panel { Height = 1, BackColor = AppTheme.BorderLight };

        lblTotalTitle = new Label { Text = "TỔNG TIỀN PHẢI THU", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = AppTheme.TextSecondary, AutoSize = true };
        lblGrandTotal = new Label
        {
            Text = "0 VNĐ",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            AutoSize = true
        };

        pnlPayMethods = new TableLayoutPanel
        {
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        pnlPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        btnPayCash = new Button { Text = "Tiền mặt", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 3, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        btnPayQr = new Button { Text = "VietQR", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        btnPayCard = new Button { Text = "Thẻ POS", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 0, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        AppTheme.ApplySecondaryButton(btnPayCash, radius: 5);
        AppTheme.ApplySecondaryButton(btnPayQr, radius: 5);
        AppTheme.ApplySecondaryButton(btnPayCard, radius: 5);
        btnPayCash.Padding = new Padding(0);
        btnPayQr.Padding = new Padding(0);
        btnPayCard.Padding = new Padding(0);

        btnPayCash.Click += (s, e) => SetPaymentMethod(1);
        btnPayQr.Click += (s, e) => SetPaymentMethod(2);
        btnPayCard.Click += (s, e) => SetPaymentMethod(3);

        pnlPayMethods.Controls.Add(btnPayCash, 0, 0);
        pnlPayMethods.Controls.Add(btnPayQr, 1, 0);
        pnlPayMethods.Controls.Add(btnPayCard, 2, 0);

        lblCashP = new Label { Text = "Tiền khách đưa (VNĐ):", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, AutoSize = true };
        numCashPaid = new NumericUpDown
        {
            Height = 32,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 100000000,
            ThousandsSeparator = true
        };
        numCashPaid.ValueChanged += (s, e) => CalculateChange();

        pnlChangeRow = new TableLayoutPanel
        {
            Height = 28,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlChangeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        pnlChangeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

        var lblChangeT = new Label { Text = "Tiền thừa trả khách:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblChangeVal = new Label { Text = "0 VNĐ", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = AppTheme.Success, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        pnlChangeRow.Controls.Add(lblChangeT, 0, 0);
        pnlChangeRow.Controls.Add(lblChangeVal, 1, 0);

        btnCheckout = new Button
        {
            Text = "HOÀN TẤT THANH TOÁN (F9)",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            Height = 46,
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnCheckout, radius: 6);
        btnCheckout.Padding = new Padding(0);
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
        pnlCol3.Controls.Add(btnPickPromotion);
        pnlCol3.Controls.Add(lblDividerB);
        pnlCol3.Controls.Add(lblTotalTitle);
        pnlCol3.Controls.Add(lblGrandTotal);
        pnlCol3.Controls.Add(pnlPayMethods);
        pnlCol3.Controls.Add(lblCashP);
        pnlCol3.Controls.Add(numCashPaid);
        pnlCol3.Controls.Add(pnlChangeRow);
        pnlCol3.Controls.Add(btnCheckout);

        pnlCol3.Resize += (s, e) => RelayoutCol3();
        pnlCol3.HandleCreated += (s, e) => RelayoutCol3();

        SetPaymentMethod(1);

        pnlMainContainer.Controls.Add(pnlCol1, 0, 0);
        pnlMainContainer.Controls.Add(pnlCol2, 1, 0);
        pnlMainContainer.Controls.Add(pnlCol3, 2, 0);

        this.Controls.Add(pnlMainContainer);
    }

    private void RelayoutCol3()
    {
        if (pnlCol3 == null || !pnlCol3.IsHandleCreated) return;
        pnlCol3.SuspendLayout();

        int padX = 14;
        int scrollbarPad = pnlCol3.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        int targetW = pnlCol3.ClientSize.Width - (padX * 2) - scrollbarPad;
        if (targetW < 260) targetW = 260;

        int currentY = 12;

        lblCustTitle.Location = new Point(padX, currentY);
        currentY += lblCustTitle.Height + 6;

        txtCustomerPhone.Location = new Point(padX, currentY);
        txtCustomerPhone.Width = targetW;
        currentY += txtCustomerPhone.Height + 6;

        lblCustName.Location = new Point(padX, currentY);
        lblCustName.Width = targetW;
        currentY += lblCustName.Height + 2;

        lblCustPoints.Location = new Point(padX, currentY);
        lblCustPoints.Width = targetW;
        currentY += lblCustPoints.Height + 2;

        lblCustTier.Location = new Point(padX, currentY);
        lblCustTier.Width = targetW;
        currentY += lblCustTier.Height + 8;

        if (pnlVoucherSuggestion.Visible)
        {
            pnlVoucherSuggestion.Location = new Point(padX, currentY);
            pnlVoucherSuggestion.Width = targetW;
            pnlVoucherSuggestion.Height = 58;
            currentY += pnlVoucherSuggestion.Height + 8;
        }

        lblDividerA.Location = new Point(padX, currentY);
        lblDividerA.Width = targetW;
        currentY += lblDividerA.Height + 8;

        pnlBillingTable.Location = new Point(padX, currentY);
        pnlBillingTable.Width = targetW;
        currentY += pnlBillingTable.Height + 8;

        pnlVoucherRow.Location = new Point(padX, currentY);
        pnlVoucherRow.Width = targetW;
        currentY += pnlVoucherRow.Height + 4;

        if (!string.IsNullOrWhiteSpace(lblVoucherStatus.Text))
        {
            lblVoucherStatus.Visible = true;
            lblVoucherStatus.Location = new Point(padX, currentY);
            lblVoucherStatus.Width = targetW;
            currentY += lblVoucherStatus.Height + 4;
        }
        else
        {
            lblVoucherStatus.Visible = false;
        }

        btnPickPromotion.Location = new Point(padX, currentY);
        btnPickPromotion.Width = targetW;
        currentY += btnPickPromotion.Height + 8;

        lblDividerB.Location = new Point(padX, currentY);
        lblDividerB.Width = targetW;
        currentY += lblDividerB.Height + 8;

        lblTotalTitle.Location = new Point(padX, currentY);
        currentY += lblTotalTitle.Height + 2;

        lblGrandTotal.Location = new Point(padX - 2, currentY);
        currentY += lblGrandTotal.Height + 8;

        pnlPayMethods.Location = new Point(padX, currentY);
        pnlPayMethods.Width = targetW;
        currentY += pnlPayMethods.Height + 8;

        if (_selectedPaymentMethod == 1)
        {
            lblCashP.Visible = true;
            numCashPaid.Visible = true;
            pnlChangeRow.Visible = true;

            lblCashP.Location = new Point(padX, currentY);
            currentY += lblCashP.Height + 4;

            numCashPaid.Location = new Point(padX, currentY);
            numCashPaid.Width = targetW;
            currentY += numCashPaid.Height + 6;

            pnlChangeRow.Location = new Point(padX, currentY);
            pnlChangeRow.Width = targetW;
            currentY += pnlChangeRow.Height + 10;
        }
        else
        {
            lblCashP.Visible = false;
            numCashPaid.Visible = false;
            pnlChangeRow.Visible = false;
        }

        btnCheckout.Location = new Point(padX, currentY);
        btnCheckout.Width = targetW;
        currentY += btnCheckout.Height + 16;

        pnlCol3.AutoScrollMinSize = new Size(0, currentY);
        pnlCol3.ResumeLayout(true);
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
                        int catId = item.TryGetProperty("categoryId", out var pCat) ? pCat.GetInt32() : 0;

                        if (!string.IsNullOrEmpty(barcode) && !_catalog.Any(c => c.Barcode == barcode))
                        {
                            _catalog.Add(new CatalogProduct
                            {
                                Id = id,
                                Barcode = barcode,
                                Name = name,
                                Price = price,
                                CategoryId = catId,
                                Stock = 100
                            });
                        }
                    }
                }
            }
        }
        catch { }

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
                    CategoryId = dsProd.CategoryId,
                    Stock = 100
                });
            }
        }

        try
        {
            var expResp = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/discount-rules/expiring-products");
            if (expResp.IsSuccessStatusCode)
            {
                var expJson = await expResp.Content.ReadAsStringAsync();
                using var expDoc = JsonDocument.Parse(expJson);
                var expRoot = expDoc.RootElement;
                var expData = expRoot.TryGetProperty("data", out var ed) ? ed : expRoot;
                if (expData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in expData.EnumerateArray())
                    {
                        string bcode = item.TryGetProperty("barcode", out var bc) ? (bc.GetString() ?? "") : "";
                        int pId = item.TryGetProperty("productId", out var pi) ? pi.GetInt32() : 0;
                        decimal discPct = item.TryGetProperty("discountPercent", out var dp) ? dp.GetDecimal() : 0m;
                        decimal clearPrice = item.TryGetProperty("clearancePrice", out var cp) ? cp.GetDecimal() : 0m;
                        int daysLeft = item.TryGetProperty("daysRemaining", out var dl) ? dl.GetInt32() : 0;

                        var match = _catalog.FirstOrDefault(c => (!string.IsNullOrEmpty(bcode) && c.Barcode == bcode) || (pId > 0 && c.Id == pId));
                        if (match != null && discPct > 0)
                        {
                            match.OriginalPrice = match.Price;
                            match.Price = clearPrice;
                            match.DiscountPercent = discPct;
                            match.IsNearExpiry = true;
                            match.ExpiryNote = $"Xả hàng cận date ({daysLeft}d) -{discPct:N0}%";
                        }
                    }
                }
            }
        }
        catch { }

        FilterProducts(txtSearchProd?.Text.Trim() ?? "");
    }

    private void FilterProducts(string keyword)
    {
        pnlProductCards.SuspendLayout();
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
            pnlProductCards.ResumeLayout();
            return;
        }

        var list = string.IsNullOrEmpty(keyword)
            ? _catalog
            : _catalog.Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || p.Barcode.Contains(keyword)).ToList();

        // 2 cột thẻ sản phẩm vừa vặn: tính dựa trên ClientSize.Width
        int scrollWidth = pnlProductCards.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        int availWidth = pnlProductCards.ClientSize.Width - scrollWidth - 12;
        int cardWidth = Math.Max(124, availWidth / 2);
        int cardHeight = 120;

        foreach (var prod in list)
        {
            var card = new Panel
            {
                Size = new Size(cardWidth, cardHeight),
                BackColor = prod.IsNearExpiry ? Color.FromArgb(255, 245, 245) : AppTheme.SurfaceWhite,
                Margin = new Padding(0, 0, 6, 8),
                Padding = new Padding(6),
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            card.MouseEnter += (s, e) => { isHovered = true; card.BackColor = prod.IsNearExpiry ? Color.FromArgb(254, 235, 235) : AppTheme.PrimarySubtle; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHovered = false; card.BackColor = prod.IsNearExpiry ? Color.FromArgb(255, 245, 245) : AppTheme.SurfaceWhite; card.Invalidate(); };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color borderColor = prod.IsNearExpiry ? Color.FromArgb(239, 68, 68) : (isHovered ? AppTheme.Primary : AppTheme.BorderLight);
                using var p = new Pen(borderColor, isHovered ? 1.5f : 1f);
                using var path = AppTheme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };

            var lblName = new Label
            {
                Text = prod.Name,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(8, 6),
                Size = new Size(card.Width - 16, 46),
                BackColor = Color.Transparent,
                AutoEllipsis = false
            };
            var lblPrice = new Label
            {
                Text = $"{prod.Price:N0} đ",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = prod.IsNearExpiry ? Color.FromArgb(220, 38, 38) : AppTheme.Primary,
                Location = new Point(8, 62),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblStock = new Label
            {
                Text = $"Kho: {prod.Stock}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(8, 88),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblName);
            card.Controls.Add(lblPrice);
            card.Controls.Add(lblStock);

            if (prod.IsNearExpiry)
            {
                var lblBadge = new Label
                {
                    Text = $"-{prod.DiscountPercent:N0}%",
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(220, 38, 38),
                    Location = new Point(card.Width - 46, 62),
                    Size = new Size(38, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                card.Controls.Add(lblBadge);
                lblBadge.BringToFront();
            }

            void AttachEvents(Control parent)
            {
                foreach (Control ctrl in parent.Controls)
                {
                    ctrl.Cursor = Cursors.Hand;
                    ctrl.MouseEnter += (s, e) => { isHovered = true; card.BackColor = prod.IsNearExpiry ? Color.FromArgb(254, 235, 235) : AppTheme.PrimarySubtle; card.Invalidate(); };
                    ctrl.MouseLeave += (s, e) => { isHovered = false; card.BackColor = prod.IsNearExpiry ? Color.FromArgb(255, 245, 245) : AppTheme.SurfaceWhite; card.Invalidate(); };
                    ctrl.Click += (s, e) => AddCatalogItemToCart(prod);
                    ctrl.DoubleClick += (s, e) => AddCatalogItemToCart(prod);
                }
                parent.Click += (s, e) => AddCatalogItemToCart(prod);
                parent.DoubleClick += (s, e) => AddCatalogItemToCart(prod);
            }
            AttachEvents(card);

            pnlProductCards.Controls.Add(card);
        }
        pnlProductCards.ResumeLayout();
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
                CategoryId = prod.CategoryId,
                Price = prod.Price,
                OriginalPrice = prod.OriginalPrice > 0 ? prod.OriginalPrice : prod.Price,
                DiscountPercent = prod.DiscountPercent,
                IsNearExpiry = prod.IsNearExpiry,
                ExpiryNote = prod.ExpiryNote,
                Quantity = 1,
                VatPercent = 8
            });
        }
        RefreshCartGrid();

        string msg = prod.IsNearExpiry
            ? $"Đã thêm {prod.Name} (Xả hàng cận date -{prod.DiscountPercent:N0}%: {prod.Price:N0} đ) vào giỏ!"
            : $"Đã thêm {prod.Name} vào giỏ!";
        AntdUI.Message.success(this.FindForm() ?? new Form(), msg);
    }

    private void RefreshCartGrid()
    {
        dgvCart.Rows.Clear();
        foreach (var item in _cart)
        {
            decimal itemSubTotal = item.Price * item.Quantity;
            string displayName = item.IsNearExpiry ? $"{item.Name} [-{item.DiscountPercent:N0}% Cận date]" : item.Name;
            string discountNote = item.IsNearExpiry ? $"-{(item.OriginalPrice - item.Price) * item.Quantity:N0} đ" : "0 đ";

            dgvCart.Rows.Add(
                displayName,
                item.Barcode,
                $"{item.Price:N0} đ",
                item.Quantity,
                $"{item.VatPercent}%",
                discountNote,
                $"{itemSubTotal:N0} đ"
            );
        }
        CalculateSummary();
    }

    private void CalculateSummary()
    {
        decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
        decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);

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
            lblChangeVal.Text = $"0 VNĐ (Làm tròn {Math.Abs(change):N0} đ lẻ)";
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
        btnPayCash.BackColor = method == 1 ? AppTheme.Primary : Color.FromArgb(245, 247, 250);
        btnPayQr.BackColor = method == 2 ? AppTheme.Primary : Color.FromArgb(245, 247, 250);
        btnPayCard.BackColor = method == 3 ? AppTheme.Primary : Color.FromArgb(245, 247, 250);

        btnPayCash.ForeColor = method == 1 ? Color.White : AppTheme.TextPrimary;
        btnPayQr.ForeColor = method == 2 ? Color.White : AppTheme.TextPrimary;
        btnPayCard.ForeColor = method == 3 ? Color.White : AppTheme.TextPrimary;

        btnPayCash.Invalidate();
        btnPayQr.Invalidate();
        btnPayCard.Invalidate();

        RelayoutCol3();

        if (method == 1)
        {
            CalculateChange();
        }
    }

    private void ChangeSelectedQty(int delta)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int idx = dgvCart.CurrentRow.Index;
        if (idx >= 0 && idx < _cart.Count)
        {
            if (delta < 0 && _cart[idx].Quantity <= 1)
            {
                // Giảm về 0 tương đương với xóa món -> Kích hoạt quy trình duyệt của Quản lý
                RemoveSelectedItem();
                return;
            }

            _cart[idx].Quantity += delta;
            RefreshCartGrid();
        }
    }

    private void RemoveSelectedItem()
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int idx = dgvCart.CurrentRow.Index;
        if (idx >= 0 && idx < _cart.Count)
        {
            var item = _cart[idx];
            bool isStaff = string.Equals(CurrentSession.Role, "Staff", StringComparison.OrdinalIgnoreCase);

            if (isStaff)
            {
                using var authDialog = new ManagerApprovalDialog(
                    "DUYỆT XÓA MÓN HÀNG KHỎI HÓA ĐƠN",
                    $"Nhân viên Thu ngân không có quyền tự ý xóa món.\nVui lòng yêu cầu Quản lý nhập mã PIN duyệt để xóa: '{item.Name}' (SL: {item.Quantity}, Tiền: {(item.Price * item.Quantity):N0} đ).",
                    _httpClient,
                    _apiBaseUrl);

                if (authDialog.ShowDialog(this.FindForm()) != DialogResult.OK || !authDialog.IsApproved)
                {
                    return; // Quản lý từ chối hoặc người dùng hủy dialog
                }

                AntdUI.Message.info(this.FindForm() ?? new Form(), $"Quản lý đã duyệt xóa món '{item.Name}'. Lý do: {authDialog.SelectedReason}");
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa món '{item.Name}' khỏi giỏ hàng?",
                    "Xác nhận xóa món", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
            }

            _cart.RemoveAt(idx);
            RefreshCartGrid();
        }
    }

    private void ClearCartWithAuth()
    {
        if (_cart.Count == 0) return;

        bool isStaff = string.Equals(CurrentSession.Role, "Staff", StringComparison.OrdinalIgnoreCase);
        if (isStaff)
        {
            decimal grandTotal = _cart.Sum(i => i.Price * i.Quantity);
            using var authDialog = new ManagerApprovalDialog(
                "DUYỆT HỦY TOÀN BỘ GIỎ HÀNG CHỜ",
                $"Nhân viên Thu ngân không có quyền tự ý hủy giỏ hàng.\nYêu cầu Quản lý nhập mã PIN duyệt để hủy toàn bộ {_cart.Count} sản phẩm (Tổng giá trị: {grandTotal:N0} đ).",
                _httpClient,
                _apiBaseUrl);

            if (authDialog.ShowDialog(this.FindForm()) != DialogResult.OK || !authDialog.IsApproved)
            {
                return;
            }

            AntdUI.Message.info(this.FindForm() ?? new Form(), $"Quản lý đã duyệt hủy cả giỏ hàng. Lý do: {authDialog.SelectedReason}");
        }
        else
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn HỦY TOÀN BỘ GIỎ HÀNG hiện tại?",
                "Xác nhận hủy giỏ hàng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
        }

        _cart.Clear();
        RefreshCartGrid();
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
                    Stock = 10
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
                $"Mã vạch '{code}' chưa có trong danh mục hệ thống!\n\nBạn có muốn mở form tạo sản phẩm mới ngay bây giờ không?",
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
                        Stock = 10
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
        if (string.IsNullOrEmpty(phone))
        {
            lblCustName.Text = "Khách Lẻ";
            lblCustPoints.Text = "Điểm tích lũy: 0 điểm";
            lblCustTier.Text = "Hạng thành viên: Thành viên mới";
            pnlVoucherSuggestion.Visible = false;
            RelayoutCol3();
            return;
        }

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
                        lblVoucherSuggestText.Text = $"Khách có {points} điểm. Đổi 100 điểm lấy Voucher -20k?";
                    }
                    else
                    {
                        pnlVoucherSuggestion.Visible = false;
                    }
                    RelayoutCol3();
                    return;
                }
            }
        }
        catch { }

        lblCustName.Text = "Khách hàng: Nguyễn Văn A (VIP)";
        lblCustPoints.Text = "Điểm tích lũy: 150 điểm";
        lblCustTier.Text = "Hạng thành viên: Kim Cương";
        pnlVoucherSuggestion.Visible = true;
        lblVoucherSuggestText.Text = "Khách có 150 điểm. Đổi 100 điểm lấy Voucher -20k?";
        RelayoutCol3();
    }

    private async Task ApplyVoucherAsync()
    {
        string code = txtVoucherCode.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        if (_cart.Count == 0)
        {
            AntdUI.Message.warn(this.FindForm() ?? new Form(), "Giỏ hàng đang trống! Thêm sản phẩm trước khi áp dụng voucher.");
            return;
        }

        decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);

        var cartItems = _cart.Select(c => new
        {
            productId = c.ProductId,
            barcode = c.Barcode,
            productName = c.Name,
            categoryId = (int?)c.CategoryId,
            quantity = c.Quantity,
            unitPrice = c.Price
        }).ToList();

        try
        {
            btnApplyVoucher.Enabled = false;
            lblVoucherStatus.Text = "Đang kiểm tra...";
            lblVoucherStatus.Visible = true;
            RelayoutCol3();

            var payload = JsonSerializer.Serialize(new
            {
                promotionCode = code,
                orderTotalAmount = grossSubTotal,
                items = cartItems
            });

            var resp = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/promotions/apply",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;

            bool isSuccess = data.TryGetProperty("isSuccess", out var s) && s.GetBoolean();
            decimal discAmt = data.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() : 0m;
            string msg = data.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";

            if (isSuccess && discAmt > 0)
            {
                _appliedDiscountAmount = discAmt;
                _appliedVoucherCode = code;
                lblVoucherStatus.Text = $"✓ {code}: -{discAmt:N0} đ";
                lblVoucherStatus.ForeColor = AppTheme.Success;
                CalculateSummary();
                AntdUI.Message.success(this.FindForm() ?? new Form(), $"Áp dụng thành công! Giảm {discAmt:N0} đ");
            }
            else
            {
                lblVoucherStatus.Text = $"✗ {(string.IsNullOrEmpty(msg) ? "Mã không hợp lệ hoặc không đủ điều kiện." : msg)}";
                lblVoucherStatus.ForeColor = AppTheme.Danger;
                AntdUI.Message.error(this.FindForm() ?? new Form(), lblVoucherStatus.Text);
            }
        }
        catch
        {
            _appliedDiscountAmount = 20000m;
            _appliedVoucherCode = code;
            lblVoucherStatus.Text = $"✓ {code}: -20,000 đ (offline)";
            lblVoucherStatus.ForeColor = AppTheme.Success;
            CalculateSummary();
        }
        finally
        {
            btnApplyVoucher.Enabled = true;
            RelayoutCol3();
        }
    }

    private async Task ShowActivePromotionsPickerAsync()
    {
        try
        {
            var resp = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/promotions/active");
            if (!resp.IsSuccessStatusCode)
            {
                AntdUI.Message.warn(this.FindForm() ?? new Form(), "Không thể tải danh sách khuyến mãi. API offline?");
                return;
            }

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var dataEl = root.TryGetProperty("data", out var d) ? d : root;

            var items = new List<(string Code, string Label)>();
            var arr = dataEl.ValueKind == JsonValueKind.Array ? dataEl
                : (dataEl.TryGetProperty("items", out var sub) ? sub : default);

            if (arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    string pCode = item.TryGetProperty("promotionCode", out var pc) ? (pc.GetString() ?? "") : "";
                    string pName = item.TryGetProperty("promotionName", out var pn) ? (pn.GetString() ?? "") : "";
                    string dtype = item.TryGetProperty("discountType", out var dt) ? (dt.GetString() ?? "") : "";
                    decimal dval = item.TryGetProperty("discountValue", out var dv) ? dv.GetDecimal() : 0m;

                    string typeTag = dtype switch
                    {
                        "ComboDiscount" => "[Combo]",
                        "BuyXGetY" => "[Mua X Tặng Y]",
                        "CategoryDiscount" => "[Ngành Hàng]",
                        "QuantityTier" => "[Mua Sỉ]",
                        "Percentage" => $"[-{dval:N0}%]",
                        "FixedAmount" => $"[-{dval:N0}₫]",
                        _ => "[Voucher]"
                    };

                    string label = $"{typeTag}  [{pCode}]  {pName}";
                    items.Add((pCode, label));
                }
            }

            if (items.Count == 0)
            {
                AntdUI.Message.info(this.FindForm() ?? new Form(), "Hiện chưa có chương trình khuyến mãi nào đang hoạt động.");
                return;
            }

            using var picker = new Form
            {
                Text = "Chọn Khuyến Mãi / Combo Áp Dụng",
                Size = new Size(560, 440),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = AppTheme.SurfaceWhite
            };

            var lblHint = new Label
            {
                Text = "Chọn chương trình khuyến mãi, rồi bấm \"Áp Dụng\":",
                Font = AppTheme.FontBodyBold,
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };

            var listBox = new ListBox
            {
                Location = new Point(16, 42),
                Size = new Size(514, 300),
                Font = AppTheme.FontBody,
                IntegralHeight = false
            };
            foreach (var (_, label) in items) listBox.Items.Add(label);
            if (listBox.Items.Count > 0) listBox.SelectedIndex = 0;

            var btnApply = new Button
            {
                Text = "Áp Dụng",
                Size = new Size(130, 36),
                Location = new Point(16, 358),
                Cursor = Cursors.Hand
            };
            AppTheme.ApplyPrimaryButton(btnApply, radius: 5);

            var btnClose2 = new Button
            {
                Text = "Đóng",
                Size = new Size(90, 36),
                Location = new Point(158, 358),
                Cursor = Cursors.Hand
            };
            AppTheme.ApplySecondaryButton(btnClose2, radius: 5);
            btnClose2.Click += (s2, e2) => picker.Close();

            btnApply.Click += async (s2, e2) =>
            {
                int idx = listBox.SelectedIndex;
                if (idx < 0 || idx >= items.Count) return;

                string selectedCode = items[idx].Code;
                picker.Close();

                txtVoucherCode.Text = selectedCode;
                await ApplyVoucherAsync();
            };

            picker.Controls.Add(lblHint);
            picker.Controls.Add(listBox);
            picker.Controls.Add(btnApply);
            picker.Controls.Add(btnClose2);

            picker.ShowDialog(this.FindForm() ?? new Form());
        }
        catch (Exception ex)
        {
            AntdUI.Message.error(this.FindForm() ?? new Form(), $"Lỗi: {ex.Message}");
        }
    }

    #region POS Special Features: Park / Return / Print

    private void ParkCurrentOrder()
    {
        if (_cart.Count == 0)
        {
            AntdUI.Message.warn(this.FindForm() ?? new Form(), "Giỏ hàng đang trống! Không thể tạm giữ.");
            return;
        }

        var parked = new ParkedOrder
        {
            CustomerPhone = txtCustomerPhone?.Text.Trim() ?? "",
            CustomerName = lblCustName.Text.Replace("Khách hàng: ", "").Trim(),
            CustomerTier = lblCustTier.Text.Replace("Hạng thành viên: ", "").Trim(),
            CustomerPoints = 0,
            AppliedDiscountAmount = _appliedDiscountAmount,
            AppliedVoucherCode = _appliedVoucherCode,
            SelectedPaymentMethod = _selectedPaymentMethod,
            CashPaid = numCashPaid.Value,
            Items = _cart.Select(c => new ParkedOrderItem
            {
                ProductId = c.ProductId,
                Barcode = c.Barcode,
                Name = c.Name,
                Price = c.Price,
                Quantity = c.Quantity
            }).ToList()
        };

        _parkedOrders.Add(parked);
        UpdateParkedOrdersCountBadge();

        _cart.Clear();
        _appliedDiscountAmount = 0m;
        _appliedVoucherCode = null;
        txtCustomerPhone.Clear();
        lblCustName.Text = "Khách Lẻ";
        lblCustPoints.Text = "Điểm tích lũy: 0 điểm";
        lblCustTier.Text = "Hạng thành viên: Thành viên mới";
        pnlVoucherSuggestion.Visible = false;
        txtVoucherCode.Clear();
        lblVoucherStatus.Text = "";
        numCashPaid.Value = 0;
        RefreshCartGrid();
        RelayoutCol3();

        AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã tạm giữ giỏ hàng [{parked.ParkId}]! Sẵn sàng phục vụ khách tiếp theo.");
    }

    private void OpenParkedOrdersDialog()
    {
        if (_parkedOrders.Count == 0)
        {
            AntdUI.Message.info(this.FindForm() ?? new Form(), "Hiện không có đơn hàng nào đang tạm giữ.");
            return;
        }

        using var formParked = new FormParkedOrders(_parkedOrders);
        if (formParked.ShowDialog(this.FindForm()) == DialogResult.OK && formParked.SelectedOrderToResume != null)
        {
            var resume = formParked.SelectedOrderToResume;

            if (_cart.Count > 0)
            {
                var dr = MessageBox.Show(
                    "Giỏ hàng hiện tại đang có sản phẩm. Bạn có muốn tạm giữ giỏ này trước khi mở lại đơn không?",
                    "Tạm giữ đơn hiện tại?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    ParkCurrentOrder();
                }
                else if (dr == DialogResult.Cancel)
                {
                    return;
                }
            }

            _cart.Clear();
            foreach (var item in resume.Items)
            {
                _cart.Add(new CartItem
                {
                    ProductId = item.ProductId,
                    Barcode = item.Barcode,
                    Name = item.Name,
                    Price = item.Price,
                    OriginalPrice = item.Price,
                    Quantity = item.Quantity,
                    VatPercent = 8
                });
            }

            txtCustomerPhone.Text = resume.CustomerPhone;
            lblCustName.Text = string.IsNullOrEmpty(resume.CustomerName) ? "Khách Lẻ" : $"Khách hàng: {resume.CustomerName}";
            lblCustTier.Text = string.IsNullOrEmpty(resume.CustomerTier) ? "Hạng thành viên: Thành viên mới" : $"Hạng thành viên: {resume.CustomerTier}";
            _appliedDiscountAmount = resume.AppliedDiscountAmount;
            _appliedVoucherCode = resume.AppliedVoucherCode;
            if (!string.IsNullOrEmpty(_appliedVoucherCode))
            {
                txtVoucherCode.Text = _appliedVoucherCode;
                lblVoucherStatus.Text = $"✓ {_appliedVoucherCode}: -{_appliedDiscountAmount:N0} đ";
                lblVoucherStatus.ForeColor = AppTheme.Success;
            }
            SetPaymentMethod(resume.SelectedPaymentMethod);
            if (resume.CashPaid > 0) numCashPaid.Value = resume.CashPaid;

            RefreshCartGrid();
            RelayoutCol3();
            UpdateParkedOrdersCountBadge();

            AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã mở lại giỏ hàng [{resume.ParkId}] thành công!");
        }
        else
        {
            UpdateParkedOrdersCountBadge();
        }
    }

    private void UpdateParkedOrdersCountBadge()
    {
        if (btnResumeParked != null)
        {
            btnResumeParked.Text = $"Đơn treo ({_parkedOrders.Count})";
            btnResumeParked.BackColor = _parkedOrders.Count > 0 ? Color.FromArgb(254, 243, 199) : Color.FromArgb(243, 244, 246);
            btnResumeParked.ForeColor = _parkedOrders.Count > 0 ? Color.FromArgb(180, 83, 9) : AppTheme.TextPrimary;
            btnResumeParked.Invalidate();
        }
    }

    private void OpenReturnRefundDialog()
    {
        using var formReturn = new FormPosReturn(_httpClient, _apiBaseUrl, CurrentSession.UserId);
        formReturn.ShowDialog(this.FindForm());
    }

    private void PrintThermalBillDirect()
    {
        InvoicePrintModel model;
        if (_cart.Count > 0)
        {
            decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
            decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);
            decimal netSubTotal = Math.Round(grandTotal / 1.08m, 0);
            decimal vatTotal = grandTotal - netSubTotal;

            model = new InvoicePrintModel
            {
                OrderCode = $"TMP{Random.Shared.Next(10000, 99999)}",
                OrderDate = DateTime.Now,
                CustomerName = lblCustName.Text.Replace("Khách hàng: ", "").Trim(),
                CustomerPhone = txtCustomerPhone?.Text.Trim() ?? "",
                CashierName = CurrentSession.FullName ?? "Thu ngân POS",
                SubTotal = netSubTotal,
                VatTotal = vatTotal,
                DiscountTotal = _appliedDiscountAmount,
                GrandTotal = grandTotal,
                PaymentMethod = _selectedPaymentMethod == 1 ? "Tiền mặt" : (_selectedPaymentMethod == 2 ? "VietQR" : "Thẻ POS"),
                CashGiven = numCashPaid.Value > 0 ? numCashPaid.Value : grandTotal,
                ChangeDue = Math.Max(0, numCashPaid.Value - grandTotal),
                Items = _cart.Select(c => new InvoicePrintItem
                {
                    ProductName = c.Name,
                    Barcode = c.Barcode,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };
        }
        else if (_lastInvoiceModel != null)
        {
            model = _lastInvoiceModel;
        }
        else
        {
            AntdUI.Message.info(this.FindForm() ?? new Form(), "Giỏ hàng trống và chưa có hóa đơn vừa hoàn tất để in.");
            return;
        }

        ThermalBillPrinterService.PrintOrPreview(model, directPrint: false);
    }

    #endregion

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F9)
        {
            if (!_isProcessingCheckout && btnCheckout != null && btnCheckout.Enabled)
            {
                _ = CheckoutAsync();
            }
            return true;
        }
        else if (keyData == Keys.F6)
        {
            if (_cart.Count > 0)
            {
                ParkCurrentOrder();
            }
            else if (_parkedOrders.Count > 0)
            {
                OpenParkedOrdersDialog();
            }
            else
            {
                AntdUI.Message.info(this.FindForm() ?? new Form(), "Giỏ hàng đang trống và không có đơn nào đang tạm giữ.");
            }
            return true;
        }
        else if (keyData == Keys.F7)
        {
            OpenReturnRefundDialog();
            return true;
        }
        else if (keyData == (Keys.Control | Keys.P))
        {
            PrintThermalBillDirect();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private async Task CheckoutAsync()
    {
        if (_isProcessingCheckout) return;
        _isProcessingCheckout = true;

        try
        {
            if (btnCheckout != null)
            {
                btnCheckout.Enabled = false;
                btnCheckout.Text = "⏳ ĐANG XỬ LÝ THANH TOÁN...";
            }
            if (pnlCol3 != null) pnlCol3.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            // Bước 1: Kiểm tra giỏ hàng (TỔNG TIỀN PHẢI THU > 0, có ít nhất 1 sản phẩm)
            if (_cart.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống! Vui lòng thêm sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal grossSubTotal = _cart.Sum(i => i.Price * i.Quantity);
            decimal grandTotal = Math.Max(0, grossSubTotal - _appliedDiscountAmount);

            if (grandTotal <= 0)
            {
                MessageBox.Show("Tổng tiền phải thu không hợp lệ (phải lớn hơn 0 VNĐ).", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal netSubTotal = Math.Round(grandTotal / 1.08m, 0);
            decimal vatTotal = grandTotal - netSubTotal;
            string cashierId = string.IsNullOrEmpty(CurrentSession.UserId.ToString()) ? "NV01" : $"NV{CurrentSession.UserId}";
            string maHoaDon = $"SSM{Random.Shared.Next(10000, 99999)}";

            if (_selectedPaymentMethod == 1)
            {
                if (numCashPaid.Value == 0)
                {
                    numCashPaid.Value = grandTotal;
                }

                if (numCashPaid.Value < grandTotal)
                {
                    decimal cashDiff = grandTotal - numCashPaid.Value;
                    MessageBox.Show($"Số tiền khách đưa ({numCashPaid.Value:N0} đ) chưa đủ để thanh toán ({grandTotal:N0} đ). Còn thiếu: {cashDiff:N0} đ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal changeAmount = Math.Max(0, numCashPaid.Value - grandTotal);
                string cashTransCode = PaymentService.GenerateTransactionCode();
                var cashTransaction = new PaymentTransactionModel
                {
                    TransactionCode = cashTransCode,
                    InvoiceId = maHoaDon,
                    PaymentMethod = PaymentMethod.Cash,
                    Amount = grandTotal,
                    GivenAmount = numCashPaid.Value,
                    ChangeAmount = changeAmount,
                    TransferContent = $"Thanh toan tien mat {maHoaDon}",
                    BankCode = null,
                    Status = PaymentStatus.Success,
                    CreatedDate = DateTime.Now,
                    CompletedDate = DateTime.Now,
                    CashierId = cashierId
                };

                try
                {
                    _paymentService.CreateTransaction(cashTransaction);
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[PosScanView] Error saving cash transaction: {dbEx.Message}");
                }

                await CompleteOrderAsync(maHoaDon, grandTotal, netSubTotal, vatTotal, "Tiền mặt", numCashPaid.Value, changeAmount);
            }
            // Bước 2B: Nếu phương thức là VIETQR / CHUYỂN KHOẢN
            else if (_selectedPaymentMethod == 2)
            {
                DialogResult qrResult;
                using (var formQr = new FormThanhToanVietQR(maHoaDon, grandTotal, cashierId))
                {
                    qrResult = formQr.ShowDialog(this.FindForm());
                }

                if (qrResult == DialogResult.OK)
                {
                    await CompleteOrderAsync(maHoaDon, grandTotal, netSubTotal, vatTotal, "VietQR", grandTotal, 0);
                }
                else
                {
                    MessageBox.Show("Thanh toán chưa hoàn tất! Quý khách có thể chọn lại phương thức thanh toán khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            // Bước 2C: Nếu phương thức là THẺ NGÂN HÀNG / QUẸT MÁY POS
            else if (_selectedPaymentMethod == 3)
            {
                var cardConfirm = MessageBox.Show(
                    $"💳 Vui lòng quẹt/chạm thẻ trên thiết bị máy POS ngân hàng.\n\n" +
                    $"• Số tiền cần thanh toán: {grandTotal:N0} VNĐ\n" +
                    $"• Mã giao dịch đơn: {maHoaDon}\n\n" +
                    $"Quý khách đã quẹt thẻ và máy POS đã in biên lai thành công?",
                    "Xác nhận thanh toán Thẻ POS",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (cardConfirm != DialogResult.Yes)
                {
                    MessageBox.Show("Chưa nhận được xác nhận thanh toán thẻ! Vui lòng thử lại hoặc chọn phương thức khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Lưu PaymentTransaction cho Thẻ POS
                string cardTransCode = PaymentService.GenerateTransactionCode();
                var cardTransaction = new PaymentTransactionModel
                {
                    TransactionCode = cardTransCode,
                    InvoiceId = maHoaDon,
                    PaymentMethod = PaymentMethod.CreditCard,
                    Amount = grandTotal,
                    GivenAmount = grandTotal,
                    ChangeAmount = 0,
                    TransferContent = $"Thanh toan the POS {maHoaDon}",
                    BankCode = "POS-TERMINAL",
                    Status = PaymentStatus.Success,
                    CreatedDate = DateTime.Now,
                    CompletedDate = DateTime.Now,
                    CashierId = cashierId
                };

                try
                {
                    _paymentService.CreateTransaction(cardTransaction);
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[PosScanView] Error saving card transaction: {dbEx.Message}");
                }

                // Tiến hành Lưu Hóa đơn -> Trừ kho -> In Bill -> Xóa giỏ hàng
                await CompleteOrderAsync(maHoaDon, grandTotal, netSubTotal, vatTotal, "Thẻ POS", grandTotal, 0);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi trong quá trình thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (pnlCol3 != null) pnlCol3.Enabled = true;
            if (btnCheckout != null)
            {
                btnCheckout.Enabled = true;
                btnCheckout.Text = "⚡ HOÀN TẤT THANH TOÁN (F9)";
            }
            this.Cursor = Cursors.Default;
            _isProcessingCheckout = false;
        }
    }

    private async Task CompleteOrderAsync(string maHoaDon, decimal grandTotal, decimal netSubTotal, decimal vatTotal, string methodStr, decimal cashGiven, decimal changeDue)
    {
        if (_cart.Count == 0) return;

        // Snapshot giỏ hàng ngay lập tức và làm rỗng giỏ hàng để ngăn chặn hoàn toàn việc thanh toán 2 lần
        var cartSnapshot = _cart.Select(c => new
        {
            ProductId = c.ProductId > 0 ? c.ProductId : 1,
            Barcode = c.Barcode,
            Name = c.Name,
            Price = c.Price,
            Quantity = c.Quantity
        }).ToList();
        _cart.Clear();
        RefreshCartGrid();

        // 1. Record POS Order in DataStore (Lưu Hóa đơn & Quản lý đơn hàng)
        var orderRecord = new PosOrderRecord
        {
            OrderId = Random.Shared.Next(1000, 9999),
            OrderCode = maHoaDon,
            OrderDate = DateTime.Now,
            CustomerName = lblCustName.Text.Replace("Khách hàng: ", "").Trim(),
            SubTotal = netSubTotal,
            VatTotal = vatTotal,
            DiscountTotal = _appliedDiscountAmount,
            GrandTotal = grandTotal,
            PaymentMethod = methodStr,
            Items = cartSnapshot.Select(c => new PosOrderItem
            {
                ProductId = c.ProductId,
                Barcode = c.Barcode,
                ProductName = c.Name,
                Price = c.Price,
                Quantity = c.Quantity
            }).ToList()
        };
        DataStore.AddOrder(orderRecord);

        try
        {
            var reqObj = new
            {
                EmployeeId = CurrentSession.UserId,
                BranchId = 1,
                ShiftSessionId = CurrentSession.ShiftSessionId,
                PaymentMethod = _selectedPaymentMethod,
                PromotionCode = _appliedVoucherCode,
                Payments = new[]
                {
                    new
                    {
                        PaymentMethod = _selectedPaymentMethod,
                        AmountPaid = grandTotal
                    }
                },
                Items = cartSnapshot.Select(c => new
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };
            string jsonBody = JsonSerializer.Serialize(reqObj);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/orders", content);
            if (res.IsSuccessStatusCode)
            {
                var resContent = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resContent);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;
                if (data.TryGetProperty("orderId", out var oid))
                {
                    int realOrderId = oid.GetInt32();
                    orderRecord.OrderId = realOrderId;
                    orderRecord.OrderCode = $"ORD-{realOrderId:D5}";
                }
            }
            else
            {
                var errBody = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Cảnh báo: Không thể đồng bộ hóa đơn lên máy chủ Backend.\nChi tiết: {errBody}", "Cảnh báo lưu đơn Backend", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PosScanView] Order post error: {ex.Message}");
        }

        var invoiceModel = new InvoicePrintModel
        {
            OrderCode = orderRecord.OrderCode,
            OrderDate = orderRecord.OrderDate,
            CustomerName = orderRecord.CustomerName,
            CustomerPhone = txtCustomerPhone?.Text.Trim() ?? "",
            CashierName = CurrentSession.FullName ?? "Thu ngân POS",
            SubTotal = netSubTotal,
            VatTotal = vatTotal,
            DiscountTotal = _appliedDiscountAmount,
            GrandTotal = grandTotal,
            PaymentMethod = methodStr,
            CashGiven = cashGiven,
            ChangeDue = changeDue,
            Items = cartSnapshot.Select(c => new InvoicePrintItem
            {
                ProductName = c.Name,
                Barcode = c.Barcode,
                Quantity = c.Quantity,
                UnitPrice = c.Price
            }).ToList()
        };
        _lastInvoiceModel = invoiceModel;

        try
        {
            ThermalBillPrinterService.PrintOrPreview(invoiceModel, directPrint: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PosScanView] Thermal print error: {ex.Message}");
        }

        string pdfPath = "";
        try
        {
            pdfPath = InvoicePdfService.PrintOrPreview(invoiceModel, autoOpen: false);
        }
        catch { }

        string pdfInfo = !string.IsNullOrEmpty(pdfPath)
            ? $"• Hóa đơn PDF: Đã lưu tại: {pdfPath}\n\n"
            : "";

        var dialogRes = MessageBox.Show(
            $"HOÀN TẤT ĐƠN HÀNG BÁN LẺ!\n\n" +
            $"• Mã hóa đơn: {orderRecord.OrderCode}\n" +
            $"• Khách hàng: {orderRecord.CustomerName}\n" +
            $"• Tổng thanh toán: {grandTotal:N0} VNĐ\n" +
            $"• Hình thức: {methodStr}\n" +
            $"• Tiền khách đưa: {cashGiven:N0} VNĐ\n" +
            $"• Tiền thừa trả khách: {changeDue:N0} VNĐ\n" +
            $"• Điểm cộng thêm: +{Math.Round(grandTotal / 10000):N0} điểm\n\n" +
            pdfInfo +
            $"Đã lưu CSDL, trừ tồn kho và in hóa đơn nhiệt K80.\n\n" +
            $"👉 Bạn có muốn chuyển sang màn hình 'Quản Lý Đơn Hàng' không?",
            "Thanh toán thành công", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

        // 4. Xóa trạng thái voucher & tiền thừa về 0 VNĐ
        _appliedDiscountAmount = 0m;
        _appliedVoucherCode = null;
        numCashPaid.Value = 0;
        txtVoucherCode.Clear();
        lblVoucherStatus.Text = "";
        RefreshCartGrid();
        RelayoutCol3();

        if (dialogRes == DialogResult.Yes)
        {
            if (this.FindForm() is MainForm mainForm)
            {
                mainForm.NavigateToOrders();
            }
        }
    }

    private class CatalogProduct
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsNearExpiry { get; set; }
        public string ExpiryNote { get; set; } = "";
        public int Stock { get; set; }
    }

    private class CartItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsNearExpiry { get; set; }
        public string ExpiryNote { get; set; } = "";
        public int Quantity { get; set; }
        public decimal VatPercent { get; set; }
    }
}
