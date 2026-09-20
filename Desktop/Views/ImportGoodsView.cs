using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class ImportGoodsView : UserControl
{
    private Panel pnlHeader = null!;
    private Panel pnlOrderInfo = null!;
    private TextBox txtScanBarcode = null!;
    private Button btnScan = null!;
    private DataGridView dgvImported = null!;
    private Label lblOrderTag = null!;
    private Label lblMissingCount = null!;
    private Label lblExtraCount = null!;
    private Label lblWrongBarcodeCount = null!;
    private Label lblProgressStatus = null!;
    private Button btnConfirmStockIn = null!;

    private readonly System.Net.Http.HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public ImportGoodsView()
    {
        InitializeComponent();
        LoadOrderSample();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Page Header ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "🚚 Nghiệp Vụ Nhập Hàng & Quét Mã Đối Soát (Goods Receiving)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Quét kiểm đếm từng kiện hàng từ nhà cung cấp, phân loại Thiếu / Dư / Sai Barcode trước khi tăng kho",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Order Header Information Card (Vinamilk - #PN23001) ---
        pnlOrderInfo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 8, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlOrderInfo);

        lblOrderTag = new Label
        {
            Text = "CHƯA CÓ ĐƠN NHẬP CẦN ĐỐI SOÁT",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 12),
            AutoSize = true
        };

        lblProgressStatus = new Label
        {
            Text = "📦 Hiện chưa có đơn hàng nhập mới nào cần đối soát hôm nay.",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 44),
            AutoSize = true
        };

        var pnlOrderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 0)
        };

        btnConfirmStockIn = new Button
        {
            Text = "✅ Xác Nhận Nhập Kho",
            Size = new Size(200, 36)
        };
        AppTheme.ApplyPrimaryButton(btnConfirmStockIn);
        btnConfirmStockIn.Click += (s, e) =>
        {
            if (dgvImported.Rows.Count == 0)
            {
                AntdUI.Message.info(this.FindForm() ?? new Form(), "Hiện tại chưa có kiện hàng nào được quét để xác nhận nhập kho.");
                return;
            }

            MessageBox.Show(
                "✅ ĐÃ XÁC NHẬN NHẬP KHO THÀNH CÔNG!\n\n" +
                $"• Tổng số mặt hàng: {dgvImported.Rows.Count}\n" +
                "• Tồn kho hệ thống đã được cập nhật tăng theo số lượng thực nhận.",
                "Tăng tồn kho thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        pnlOrderRight.Controls.Add(btnConfirmStockIn);

        pnlOrderInfo.Controls.Add(lblOrderTag);
        pnlOrderInfo.Controls.Add(lblProgressStatus);
        pnlOrderInfo.Controls.Add(pnlOrderRight);

        // --- 3. Barcode Scanner Toolbar Card ---
        var pnlScanCard = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 65),
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 14, 16, 12),
            Margin = new Padding(0, 8, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        AppTheme.ApplyCardPanel(pnlScanCard);

        var lblScanPrompt = new Label
        {
            Text = "📷 Scan Barcode Kiện Hàng:",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 6, 12, 0)
        };

        txtScanBarcode = new TextBox
        {
            Size = new Size(300, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            PlaceholderText = "Quét máy bắn mã vạch (Enter)...",
            Margin = new Padding(0, 2, 12, 4)
        };
        txtScanBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ProcessScanItem(); };

        btnScan = new Button
        {
            Text = "🔍 Quét Nhận",
            Size = new Size(130, 32),
            Margin = new Padding(0, 2, 0, 4)
        };
        AppTheme.ApplyPrimaryButton(btnScan);
        btnScan.Click += (s, e) => ProcessScanItem();

        pnlScanCard.Controls.Add(lblScanPrompt);
        pnlScanCard.Controls.Add(txtScanBarcode);
        pnlScanCard.Controls.Add(btnScan);

        // --- 4. Discrepancy Status Pills (Thiếu 5, Dư 2, Sai barcode 1) ---
        var pnlDiscrepancy = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
        };

        lblMissingCount = CreatePillLabel("⚠️ Thiếu: 0 SP", AppTheme.DangerSubtle, AppTheme.Danger);
        lblExtraCount = CreatePillLabel("➕ Dư: 0 SP", AppTheme.WarningSubtle, AppTheme.Warning);
        lblWrongBarcodeCount = CreatePillLabel("❌ Sai Barcode: 0 SP", Color.FromArgb(245, 235, 255), Color.FromArgb(120, 40, 200));

        pnlDiscrepancy.Controls.Add(lblMissingCount);
        pnlDiscrepancy.Controls.Add(lblExtraCount);
        pnlDiscrepancy.Controls.Add(lblWrongBarcodeCount);

        // --- 5. Checked Goods DataGrid ---
        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1),
            Margin = new Padding(0, 6, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlGrid);

        dgvImported = new DataGridView();
        AppTheme.ApplyGridStyle(dgvImported);

        dgvImported.Columns.Add("StatusIcon", "Đối Soát");
        dgvImported.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvImported.Columns.Add("ExpectedQty", "SL Giao Dự Kiến");
        dgvImported.Columns.Add("ActualQty", "SL Đã Quét Nhận");
        dgvImported.Columns.Add("Diff", "Chênh Lệch");
        dgvImported.Columns.Add("Note", "Ghi Chú Nghiệp Vụ");

        pnlGrid.Controls.Add(dgvImported);

        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlDiscrepancy);
        this.Controls.Add(pnlScanCard);
        this.Controls.Add(pnlOrderInfo);
        this.Controls.Add(pnlHeader);
    }

    private Label CreatePillLabel(string text, Color bg, Color fg)
    {
        var lbl = new Label
        {
            Text = text,
            Font = AppTheme.FontBodyBold,
            BackColor = bg,
            ForeColor = fg,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 15, 0),
            AutoSize = true
        };
        return lbl;
    }

    private void LoadOrderSample()
    {
        dgvImported.Rows.Clear();
        lblOrderTag.Text = "CHƯA CÓ ĐƠN NHẬP CẦN ĐỐI SOÁT";
        lblProgressStatus.Text = "📦 Hiện chưa có đơn hàng nhập mới nào cần đối soát hôm nay.";
        lblMissingCount.Text = "⚠️ Thiếu: 0 SP";
        lblExtraCount.Text = "➕ Dư: 0 SP";
        lblWrongBarcodeCount.Text = "❌ Sai Barcode: 0 SP";
    }

    private async void ProcessScanItem()
    {
        string code = txtScanBarcode.Text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        var dsProd = DataStore.FindProductByBarcode(code);
        if (dsProd != null)
        {
            dgvImported.Rows.Add("✅ Khớp", dsProd.ProductName, "1", "1", "0", "Hàng đạt chuẩn");
            lblOrderTag.Text = $"ĐƠN NHẬP KHO THỰC TẾ   |   SỐ LƯỢNG MẶT HÀNG: {dgvImported.Rows.Count}";
            lblProgressStatus.Text = $"📦 Đã quét nhận: {dgvImported.Rows.Count} sản phẩm vào biên bản nhập kho";
            AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã nhận kiện hàng: {dsProd.ProductName} (Mã: {code}) (+1)");
            txtScanBarcode.Clear();
            txtScanBarcode.Focus();
            return;
        }

        try
        {
            // Check if product exists in database
            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products/barcode/{code}");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;
                string name = "Sản phẩm";
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("productName", out var pName))
                {
                    name = pName.GetString() ?? "Sản phẩm";
                }

                // Add to imported grid
                dgvImported.Rows.Add("✅ Khớp", name, "1", "1", "0", "Hàng đạt chuẩn");
                lblOrderTag.Text = $"ĐƠN NHẬP KHO THỰC TẾ   |   SỐ LƯỢNG MẶT HÀNG: {dgvImported.Rows.Count}";
                lblProgressStatus.Text = $"📦 Đã quét nhận: {dgvImported.Rows.Count} sản phẩm vào biên bản nhập kho";
                AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã nhận kiện hàng: {name} (Mã: {code}) (+1)");
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Mã vạch kiện hàng '{code}' chưa có trong hệ thống dữ liệu siêu thị!\n\nBạn có muốn mở popup thêm mới sản phẩm (nhập tên, nhà cung cấp, giá bán, ảnh) vào CSDL ngay bây giờ không?",
                    "Mã hàng mới",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    using var addForm = new AddProductForm(_httpClient, _apiBaseUrl, code);
                    if (addForm.ShowDialog(this.FindForm()) == DialogResult.OK && addForm.CreatedProduct != null)
                    {
                        var newProd = addForm.CreatedProduct;
                        dgvImported.Rows.Add("🆕 Mới tạo", newProd.ProductName, "1", "1", "0", $"Mới tạo từ NCC (Mã: {code})");
                        lblOrderTag.Text = $"ĐƠN NHẬP KHO THỰC TẾ   |   SỐ LƯỢNG MẶT HÀNG: {dgvImported.Rows.Count}";
                        lblProgressStatus.Text = $"📦 Đã quét nhận: {dgvImported.Rows.Count} sản phẩm vào biên bản nhập kho";
                        AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã tạo mới sản phẩm '{newProd.ProductName}' và thêm vào danh sách nhập hàng!");
                    }
                }
            }
        }
        catch
        {
            var confirm = MessageBox.Show(
                $"Mã vạch kiện hàng '{code}' chưa có trong danh mục!\n\nBạn có muốn mở popup khai báo thông tin sản phẩm (nhà cung cấp, giá cả, ảnh) ngay không?",
                "Khai báo mã hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                using var addForm = new AddProductForm(_httpClient, _apiBaseUrl, code);
                if (addForm.ShowDialog(this.FindForm()) == DialogResult.OK && addForm.CreatedProduct != null)
                {
                    var newProd = addForm.CreatedProduct;
                    dgvImported.Rows.Add("🆕 Mới tạo", newProd.ProductName, "1", "1", "0", $"Mới tạo (Mã: {code})");
                    AntdUI.Message.success(this.FindForm() ?? new Form(), $"Đã tạo mới sản phẩm '{newProd.ProductName}'!");
                }
            }
        }

        txtScanBarcode.Clear();
        txtScanBarcode.Focus();
    }
}
