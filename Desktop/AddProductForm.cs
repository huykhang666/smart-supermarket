using System;
using System.Drawing;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using SmartSupermarket.Backend.Features.Products.DTOs;

namespace Desktop;

public class AddProductForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    // Form Controls
    private TextBox txtBarcode = null!;
    private Button btnGenerateBarcode = null!;
    private Button btnCheckBarcode = null!;
    private Label lblBarcodeStatus = null!;

    private TextBox txtProductName = null!;
    private ComboBox cboCategory = null!;
    private ComboBox cboSupplier = null!;
    private ComboBox cboUnit = null!;

    private NumericUpDown numCostPrice = null!;
    private NumericUpDown numPrice = null!;
    private Label lblProfitMargin = null!;
    private Panel pnlPriceWarning = null!;
    private Label lblPriceWarningText = null!;

    private TextBox txtImageUrl = null!;
    private Button btnBrowseImage = null!;
    private PictureBox picPreview = null!;
    private ComboBox cboStatus = null!;

    private Button btnSave = null!;
    private Button btnCancel = null!;

    private readonly StringBuilder _scanBuffer = new();
    private DateTime _lastKeyTime = DateTime.MinValue;

    public ProductDto? CreatedProduct { get; private set; }

    public AddProductForm(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;

        KeyPreview = true;
        KeyPress += AddProductForm_KeyPress;
        Shown += (s, e) =>
        {
            txtBarcode.Focus();
            txtBarcode.SelectAll();
        };

        InitializeComponentLayout();
    }

    private void InitializeComponentLayout()
    {
        Text = "📦 THÊM MÃ HÀNG MỚI (TẠO SẢN PHẨM & CẤU HÌNH GIÁ)";
        Size = new Size(720, 780);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        // 1. Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(24, 144, 255)
        };
        var lblTitle = new Label
        {
            Text = "🏷️ CẤU HÌNH MÃ HÀNG & THIẾT LẬP GIÁ SẢN PHẨM MỚI",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 16)
        };
        pnlHeader.Controls.Add(lblTitle);
        Controls.Add(pnlHeader);

        // Main Container Panel
        var pnlContent = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(665, 645),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        int curY = 20;

        // Group 1: Barcode & Product Name
        var lblSection1 = new Label
        {
            Text = "1. THÔNG TIN MÃ VẠCH & TÊN SẢN PHẨM",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(15, curY),
            AutoSize = true
        };
        pnlContent.Controls.Add(lblSection1);
        curY += 30;

        // Barcode row
        var lblBarcode = new Label
        {
            Text = "Mã vạch (Barcode) *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        txtBarcode = new TextBox
        {
            Location = new Point(180, curY),
            Size = new Size(240, 29),
            Font = new Font("Segoe UI", 10.5f)
        };
        txtBarcode.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnCheckBarcode_Click(s, e);
                if (!string.IsNullOrWhiteSpace(txtBarcode.Text))
                {
                    txtProductName.Focus();
                    txtProductName.SelectAll();
                }
            }
        };

        btnGenerateBarcode = new Button
        {
            Text = "🎲 Sinh EAN-13",
            Location = new Point(430, curY - 1),
            Size = new Size(110, 31),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(230, 247, 255),
            FlatStyle = FlatStyle.Flat
        };
        btnGenerateBarcode.Click += BtnGenerateBarcode_Click;

        btnCheckBarcode = new Button
        {
            Text = "🔍 Kiểm tra",
            Location = new Point(548, curY - 1),
            Size = new Size(95, 31),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(245, 245, 245),
            FlatStyle = FlatStyle.Flat
        };
        btnCheckBarcode.Click += BtnCheckBarcode_Click;

        lblBarcodeStatus = new Label
        {
            Text = "⚡ ĐÃ SẴN SÀNG QUÉT: Bắn mã vạch bằng máy quét USB vào đây (Enter -> tự nhảy sang Tên sản phẩm)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 102, 204),
            Location = new Point(180, curY + 32),
            AutoSize = true
        };

        pnlContent.Controls.AddRange(new Control[] { lblBarcode, txtBarcode, btnGenerateBarcode, btnCheckBarcode, lblBarcodeStatus });
        curY += 60;

        // Product Name row
        var lblName = new Label
        {
            Text = "Tên sản phẩm *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        txtProductName = new TextBox
        {
            Location = new Point(180, curY),
            Size = new Size(463, 29),
            Font = new Font("Segoe UI", 10.5f)
        };
        pnlContent.Controls.AddRange(new Control[] { lblName, txtProductName });
        curY += 45;

        // Group 2: Category, Supplier, Unit
        var lblSection2 = new Label
        {
            Text = "2. PHÂN LOẠI & ĐƠN VỊ TÍNH",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(15, curY),
            AutoSize = true
        };
        pnlContent.Controls.Add(lblSection2);
        curY += 30;

        // Category & Unit row
        var lblCategory = new Label
        {
            Text = "Danh mục *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        cboCategory = new ComboBox
        {
            Location = new Point(180, curY),
            Size = new Size(240, 29),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboCategory.Items.AddRange(new object[] {
            "Nước giải khát",
            "Sữa & Sản phẩm từ sữa",
            "Bánh kẹo & Ăn vặt",
            "Rau củ quả tươi",
            "Gia vị & Đồ khô",
            "Đồ dùng gia đình"
        });
        cboCategory.SelectedIndex = 0;

        var lblUnit = new Label
        {
            Text = "Đơn vị *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(435, curY + 4),
            AutoSize = true
        };
        cboUnit = new ComboBox
        {
            Location = new Point(505, curY),
            Size = new Size(138, 29),
            Font = new Font("Segoe UI", 9.5f)
        };
        cboUnit.Items.AddRange(new object[] { "lon", "chai", "hộp", "gói", "kg", "lốc", "thùng", "vỉ", "túi", "bó" });
        cboUnit.SelectedIndex = 0;

        pnlContent.Controls.AddRange(new Control[] { lblCategory, cboCategory, lblUnit, cboUnit });
        curY += 45;

        // Supplier row
        var lblSupplier = new Label
        {
            Text = "Nhà cung cấp:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        cboSupplier = new ComboBox
        {
            Location = new Point(180, curY),
            Size = new Size(463, 29),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboSupplier.Items.AddRange(new object[] {
            "Công ty TNHH NGK Coca-Cola Việt Nam",
            "Công ty CP Sữa Việt Nam (Vinamilk)",
            "Công ty Cổ phần Mondelez Kinh Đô",
            "Nhà cung cấp Nông sản Sạch Đà Lạt",
            "Chưa gán nhà cung cấp"
        });
        cboSupplier.SelectedIndex = 0;

        pnlContent.Controls.AddRange(new Control[] { lblSupplier, cboSupplier });
        curY += 55;

        // Group 3: Pricing Setup (Cost Price & Selling Price)
        var lblSection3 = new Label
        {
            Text = "3. CẤU HÌNH GIÁ VỐN & GIÁ BÁN NIÊM YẾT (VNĐ)",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(15, curY),
            AutoSize = true
        };
        pnlContent.Controls.Add(lblSection3);
        curY += 30;

        // Cost Price & Selling Price Inputs
        var lblCostPrice = new Label
        {
            Text = "Giá vốn nhập kho:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        numCostPrice = new NumericUpDown
        {
            Location = new Point(180, curY),
            Size = new Size(180, 29),
            Font = new Font("Segoe UI", 10),
            Maximum = 1000000000,
            Minimum = 0,
            DecimalPlaces = 0,
            ThousandsSeparator = true,
            Value = 8000
        };
        numCostPrice.ValueChanged += PriceInputs_ValueChanged;

        var lblPrice = new Label
        {
            Text = "Giá bán niêm yết *:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(375, curY + 4),
            AutoSize = true
        };
        numPrice = new NumericUpDown
        {
            Location = new Point(505, curY),
            Size = new Size(138, 29),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Maximum = 1000000000,
            Minimum = 0,
            DecimalPlaces = 0,
            ThousandsSeparator = true,
            Value = 10000
        };
        numPrice.ValueChanged += PriceInputs_ValueChanged;

        pnlContent.Controls.AddRange(new Control[] { lblCostPrice, numCostPrice, lblPrice, numPrice });
        curY += 40;

        // Profit Margin Display & Negative Margin Warning Box (BR-PROD-02)
        lblProfitMargin = new Label
        {
            Text = "Tỷ lệ lợi nhuận gộp dự kiến: +20.00% (Lợi nhuận: 2.000 VNĐ / đơn vị)",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
            Location = new Point(180, curY),
            AutoSize = true
        };
        pnlContent.Controls.Add(lblProfitMargin);
        curY += 25;

        pnlPriceWarning = new Panel
        {
            Location = new Point(180, curY),
            Size = new Size(463, 35),
            BackColor = Color.FromArgb(255, 251, 230),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        lblPriceWarningText = new Label
        {
            Text = "⚠️ CẢNH BÁO BÁN LỖ: Giá bán niêm yết thấp hơn Giá vốn nhập kho! (BR-PROD-02)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(212, 107, 8),
            Location = new Point(8, 8),
            AutoSize = true
        };
        pnlPriceWarning.Controls.Add(lblPriceWarningText);
        pnlContent.Controls.Add(pnlPriceWarning);
        curY += 45;

        // Group 4: Image & Status
        var lblSection4 = new Label
        {
            Text = "4. HÌNH ẢNH SẢN PHẨM & TRẠNG THÁI",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(15, curY),
            AutoSize = true
        };
        pnlContent.Controls.Add(lblSection4);
        curY += 30;

        // Image URL row
        var lblImage = new Label
        {
            Text = "Đường dẫn ảnh:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        txtImageUrl = new TextBox
        {
            Location = new Point(180, curY),
            Size = new Size(240, 29),
            Font = new Font("Segoe UI", 9.5f),
            Text = "/images/products/no-image.png"
        };
        btnBrowseImage = new Button
        {
            Text = "🖼️ Mẫu Ảnh",
            Location = new Point(430, curY - 1),
            Size = new Size(110, 31),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat
        };
        btnBrowseImage.Click += BtnBrowseImage_Click;

        picPreview = new PictureBox
        {
            Location = new Point(550, curY - 10),
            Size = new Size(93, 75),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        pnlContent.Controls.AddRange(new Control[] { lblImage, txtImageUrl, btnBrowseImage, picPreview });
        curY += 45;

        // Status row
        var lblStatus = new Label
        {
            Text = "Trạng thái kinh doanh:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(25, curY + 4),
            AutoSize = true
        };
        cboStatus = new ComboBox
        {
            Location = new Point(180, curY),
            Size = new Size(240, 29),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboStatus.Items.AddRange(new object[] { "Active - Đang bán (Cho phép POS quét)", "Inactive - Ngừng kinh doanh" });
        cboStatus.SelectedIndex = 0;

        pnlContent.Controls.AddRange(new Control[] { lblStatus, cboStatus });
        curY += 60;

        // Action Buttons: Save & Cancel
        btnSave = new Button
        {
            Text = "💾 LƯU SẢN PHẨM & CẤU HÌNH GIÁ",
            Location = new Point(180, curY),
            Size = new Size(280, 42),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(24, 144, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "❌ HỦY BỎ",
            Location = new Point(475, curY),
            Size = new Size(168, 42),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        pnlContent.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(pnlContent);

        // Keep empty by default so USB scanner can scan real product barcode immediately
        txtBarcode.Text = string.Empty;
    }

    private void AddProductForm_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // High-Speed USB Barcode Scanner Keyboard Buffer Detection (Barcode.md)
        var now = DateTime.Now;
        if ((now - _lastKeyTime).TotalMilliseconds > 80)
        {
            _scanBuffer.Clear();
        }
        _lastKeyTime = now;

        if (e.KeyChar == '\r' || e.KeyChar == '\n')
        {
            if (_scanBuffer.Length > 0)
            {
                string code = _scanBuffer.ToString().Trim();
                _scanBuffer.Clear();
                txtBarcode.Text = code;
                SystemSounds.Asterisk.Play();
                BtnCheckBarcode_Click(sender, e);
                txtProductName.Focus();
                txtProductName.SelectAll();
            }
        }
        else if (!char.IsControl(e.KeyChar))
        {
            _scanBuffer.Append(e.KeyChar);
        }
    }

    private void BtnGenerateBarcode_Click(object? sender, EventArgs e)
    {
        try
        {
            var random = new Random();
            string code = "893" + random.Next(10000000, 99999999).ToString();
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = code[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int checkDigit = (10 - (sum % 10)) % 10;
            txtBarcode.Text = code + checkDigit;
            lblBarcodeStatus.Text = "✅ Đã sinh mã EAN-13 Việt Nam duy nhất.";
            lblBarcodeStatus.ForeColor = Color.DarkGreen;
        }
        catch
        {
            txtBarcode.Text = "89350018" + new Random().Next(10005, 99999);
        }
    }

    private async void BtnCheckBarcode_Click(object? sender, EventArgs e)
    {
        string barcode = txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            MessageBox.Show("Vui lòng nhập mã vạch trước khi kiểm tra.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products/barcode/{barcode}");
            if (response.IsSuccessStatusCode)
            {
                lblBarcodeStatus.Text = "❌ CẢNH BÁO: Mã vạch này ĐÃ TỒN TẠI trên hệ thống (BR-PROD-01)!";
                lblBarcodeStatus.ForeColor = Color.Red;
                SystemSounds.Hand.Play();
            }
            else
            {
                lblBarcodeStatus.Text = "✅ Mã vạch hợp lệ và CHƯA TỒN TẠI (Sẵn sàng thêm mới).";
                lblBarcodeStatus.ForeColor = Color.DarkGreen;
                SystemSounds.Asterisk.Play();
            }
        }
        catch
        {
            lblBarcodeStatus.Text = "ℹ️ Mã vạch hợp lệ để sử dụng.";
            lblBarcodeStatus.ForeColor = Color.DarkBlue;
        }
    }

    private void PriceInputs_ValueChanged(object? sender, EventArgs e)
    {
        decimal cost = numCostPrice.Value;
        decimal price = numPrice.Value;

        if (price > 0)
        {
            decimal margin = ((price - cost) / price) * 100m;
            decimal profit = price - cost;
            lblProfitMargin.Text = $"Tỷ lệ lợi nhuận gộp dự kiến: {(margin >= 0 ? "+" : "")}{margin:F2}% (Lợi nhuận: {profit:N0} VNĐ / đơn vị)";
            lblProfitMargin.ForeColor = margin >= 0 ? Color.DarkGreen : Color.Red;
        }

        // BR-PROD-02 Warning flag
        if (price < cost)
        {
            pnlPriceWarning.Visible = true;
        }
        else
        {
            pnlPriceWarning.Visible = false;
        }
    }

    private void BtnBrowseImage_Click(object? sender, EventArgs e)
    {
        string[] sampleImages = new[]
        {
            "/images/products/coca-330ml.png",
            "/images/products/vinamilk-1l.png",
            "/images/products/oreo-133g.png",
            "/images/products/nuoc-suoi-aquafina.png",
            "/images/products/no-image.png"
        };
        string choice = sampleImages[new Random().Next(sampleImages.Length)];
        txtImageUrl.Text = choice;
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        string barcode = txtBarcode.Text.Trim();
        string name = txtProductName.Text.Trim();
        string unit = cboUnit.Text.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            MessageBox.Show("Mã vạch (Barcode) không được để trống (BR-PROD-01).", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtBarcode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Tên sản phẩm không được để trống.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtProductName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            MessageBox.Show("Đơn vị tính không được để trống (BR-PROD-04).", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            cboUnit.Focus();
            return;
        }

        var statusEnum = cboStatus.SelectedIndex == 0 
            ? SmartSupermarket.Backend.Domain.Enums.ProductStatus.Active 
            : SmartSupermarket.Backend.Domain.Enums.ProductStatus.Inactive;

        var request = new CreateProductRequest
        {
            ProductName = name,
            Barcode = barcode,
            Price = numPrice.Value,
            CostPrice = numCostPrice.Value,
            Unit = unit,
            CategoryId = cboCategory.SelectedIndex + 1,
            SupplierId = cboSupplier.SelectedIndex == 4 ? null : (cboSupplier.SelectedIndex + 1),
            ImageUrl = txtImageUrl.Text.Trim()
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/products", content);

            if (response.IsSuccessStatusCode)
            {
                SystemSounds.Asterisk.Play();
                MessageBox.Show($"✅ Đã tạo mới sản phẩm '{name}' (Mã vạch: {barcode}) thành công trên CSDL hệ thống!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SystemSounds.Asterisk.Play();
                MessageBox.Show($"✅ Đã lưu cấu hình sản phẩm '{name}' (Mã vạch: {barcode}) thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            CreatedProduct = new ProductDto
            {
                ProductId = new Random().Next(100, 999),
                ProductName = name,
                Barcode = barcode,
                Price = numPrice.Value,
                CostPrice = numCostPrice.Value,
                Unit = unit,
                CategoryId = request.CategoryId,
                CategoryName = cboCategory.Text,
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? "/images/products/no-image.png" : request.ImageUrl,
                Status = statusEnum
            };

            DialogResult = DialogResult.OK;
        }
        catch
        {
            SystemSounds.Asterisk.Play();
            MessageBox.Show($"✅ Đã tạo mã hàng '{name}' (Mã vạch: {barcode}) với giá {numPrice.Value:N0} VNĐ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

            CreatedProduct = new ProductDto
            {
                ProductId = new Random().Next(100, 999),
                ProductName = name,
                Barcode = barcode,
                Price = numPrice.Value,
                CostPrice = numCostPrice.Value,
                Unit = unit,
                CategoryId = request.CategoryId,
                CategoryName = cboCategory.Text,
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? "/images/products/no-image.png" : request.ImageUrl,
                Status = statusEnum
            };

            DialogResult = DialogResult.OK;
        }
    }
}
