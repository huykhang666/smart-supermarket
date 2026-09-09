using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 100;
    public int Quantity { get; set; } = 1;
    public double VatRate { get; set; } = 0.10; // 10% VAT
    public decimal SubTotal => Price * Quantity;
    public decimal VatAmount => SubTotal * (decimal)VatRate;
    public decimal TotalAmount => SubTotal + VatAmount;
}

public partial class Form1 : Form
{
    private readonly List<CartItem> _cartItems = new();
    private readonly Dictionary<string, CartItem> _customProducts = new();
    private readonly HttpClient _httpClient = new();
    private readonly StringBuilder _scanBuffer = new();
    private DateTime _lastKeyTime = DateTime.MinValue;
    private readonly string _apiBaseUrl = "http://localhost:5000";

    // Controls
    private TextBox txtBarcode = null!;
    private Button btnScan = null!;
    private Button btnAddNewProduct = null!;
    private Button btnDemoScan1 = null!;
    private Button btnDemoScan2 = null!;
    private Button btnDemoScan3 = null!;
    private DataGridView dgvCart = null!;

    private PictureBox picProduct = null!;
    private Label lblProductName = null!;
    private Label lblBarcode = null!;
    private Label lblPrice = null!;
    private Label lblVat = null!;
    private Label lblStock = null!;
    private Label lblStatusMessage = null!;

    private Label lblSubTotal = null!;
    private Label lblTotalVat = null!;
    private Label lblGrandTotal = null!;
    private Label lblItemCount = null!;

    private Button btnPlus = null!;
    private Button btnMinus = null!;
    private Button btnRemove = null!;
    private Button btnClearCart = null!;

    public Form1()
    {
        InitializeComponent();
        BuildCustomLayout();
        KeyPreview = true;
        KeyDown += Form1_KeyDown;
        KeyPress += Form1_KeyPress;
    }

    private void BuildCustomLayout()
    {
        Text = "SMART SUPERMARKET - DESKTOP POS DEMO (QUÉT MÃ VẠCH SẢN PHẨM)";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(24, 144, 255)
        };
        var lblTitle = new Label
        {
            Text = "🛒 SMART SUPERMARKET POS — MÔ PHỎNG QUÉT MÃ VẠCH TÍNH TIỀN",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 15)
        };
        pnlHeader.Controls.Add(lblTitle);
        Controls.Add(pnlHeader);

        // Top Barcode Scanner Section
        var pnlScanBar = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(1220, 70),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblScanPrompt = new Label
        {
            Text = "Quét / Nhập Barcode:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(15, 22),
            AutoSize = true
        };

        txtBarcode = new TextBox
        {
            Location = new Point(160, 18),
            Size = new Size(300, 30),
            Font = new Font("Segoe UI", 12)
        };
        txtBarcode.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ProcessScanBarcode(txtBarcode.Text);
            }
        };

        btnScan = new Button
        {
            Text = "🔍 Quét Mã Vạch",
            Location = new Point(475, 16),
            Size = new Size(125, 36),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(24, 144, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnScan.Click += (s, e) => ProcessScanBarcode(txtBarcode.Text);

        btnAddNewProduct = new Button
        {
            Text = "➕ Thêm Mã Hàng Mới (F2)",
            Location = new Point(610, 16),
            Size = new Size(185, 36),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(82, 196, 26),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAddNewProduct.Click += BtnAddNewProduct_Click;

        var lblDemoTag = new Label
        {
            Text = "Demo:",
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            Location = new Point(805, 25),
            AutoSize = true
        };

        btnDemoScan1 = new Button
        {
            Text = "Coca 330ml",
            Location = new Point(855, 18),
            Size = new Size(105, 32),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(230, 247, 255),
            FlatStyle = FlatStyle.Flat
        };
        btnDemoScan1.Click += (s, e) => ProcessScanBarcode("8935001800012");

        btnDemoScan2 = new Button
        {
            Text = "Vinamilk 1L",
            Location = new Point(968, 18),
            Size = new Size(110, 32),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(246, 255, 237),
            FlatStyle = FlatStyle.Flat
        };
        btnDemoScan2.Click += (s, e) => ProcessScanBarcode("8934673123456");

        btnDemoScan3 = new Button
        {
            Text = "Oreo 133g",
            Location = new Point(1085, 18),
            Size = new Size(100, 32),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(255, 242, 232),
            FlatStyle = FlatStyle.Flat
        };
        btnDemoScan3.Click += (s, e) => ProcessScanBarcode("8935001800099");

        pnlScanBar.Controls.AddRange(new Control[] {
            lblScanPrompt, txtBarcode, btnScan, btnAddNewProduct, lblDemoTag, btnDemoScan1, btnDemoScan2, btnDemoScan3
        });
        Controls.Add(pnlScanBar);

        // Status Message Bar
        lblStatusMessage = new Label
        {
            Text = "Ready - Hãy sử dụng máy quét mã vạch USB hoặc bấm phím Enter sau khi nhập mã.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
            Location = new Point(20, 150),
            AutoSize = true
        };
        Controls.Add(lblStatusMessage);

        // Left Panel: Cart DataGridView (Invoice Items Table)
        dgvCart = new DataGridView
        {
            Location = new Point(20, 180),
            Size = new Size(820, 420),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };
        SetupGridColumns();
        Controls.Add(dgvCart);

        // Grid Action Buttons (Below Grid)
        btnPlus = new Button
        {
            Text = "➕ Tăng số lượng (+)",
            Location = new Point(20, 610),
            Size = new Size(160, 38),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(82, 196, 26),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnPlus.Click += BtnPlus_Click;

        btnMinus = new Button
        {
            Text = "➖ Giảm số lượng (-)",
            Location = new Point(190, 610),
            Size = new Size(160, 38),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(250, 173, 20),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnMinus.Click += BtnMinus_Click;

        btnRemove = new Button
        {
            Text = "❌ Xóa món (Remove)",
            Location = new Point(360, 610),
            Size = new Size(170, 38),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(255, 77, 79),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRemove.Click += BtnRemove_Click;

        btnClearCart = new Button
        {
            Text = "🗑️ Xóa sạch giỏ",
            Location = new Point(540, 610),
            Size = new Size(140, 38),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(140, 140, 140),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnClearCart.Click += (s, e) =>
        {
            _cartItems.Clear();
            RefreshCartGrid();
            lblStatusMessage.Text = "Đã xóa toàn bộ giỏ hàng.";
            lblStatusMessage.ForeColor = Color.Gray;
        };

        Controls.AddRange(new Control[] { btnPlus, btnMinus, btnRemove, btnClearCart });

        // Right Side Panel: Scanned Product Preview + Invoice Total Summary
        var pnlRight = new Panel
        {
            Location = new Point(860, 180),
            Size = new Size(380, 520),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Product Preview Sub-panel
        var pnlPreview = new Panel
        {
            Location = new Point(15, 15),
            Size = new Size(350, 240),
            BackColor = Color.FromArgb(250, 250, 250),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblPreviewTitle = new Label
        {
            Text = "📦 SẢN PHẨM VỪA QUÉT",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(10, 10),
            AutoSize = true
        };

        picProduct = new PictureBox
        {
            Location = new Point(15, 40),
            Size = new Size(110, 110),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        lblProductName = new Label
        {
            Text = "Tên: Chưa quét sản phẩm",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(135, 40),
            Size = new Size(200, 45)
        };

        lblBarcode = new Label
        {
            Text = "Barcode: -",
            Font = new Font("Segoe UI", 9),
            Location = new Point(135, 90),
            AutoSize = true
        };

        lblPrice = new Label
        {
            Text = "Đơn giá: 0 VNĐ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            Location = new Point(135, 112),
            AutoSize = true
        };

        lblVat = new Label
        {
            Text = "VAT (10%): 0 VNĐ",
            Font = new Font("Segoe UI", 9),
            Location = new Point(135, 134),
            AutoSize = true
        };

        lblStock = new Label
        {
            Text = "Tồn kho: -",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
            Location = new Point(15, 175),
            AutoSize = true
        };

        pnlPreview.Controls.AddRange(new Control[] {
            lblPreviewTitle, picProduct, lblProductName, lblBarcode, lblPrice, lblVat, lblStock
        });
        pnlRight.Controls.Add(pnlPreview);

        // Invoice Summary Sub-panel
        var pnlSummary = new Panel
        {
            Location = new Point(15, 270),
            Size = new Size(350, 230),
            BackColor = Color.FromArgb(240, 245, 255),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblSummaryTitle = new Label
        {
            Text = "🧾 TỔNG CỘNG HÓA ĐƠN",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 144, 255),
            Location = new Point(10, 12),
            AutoSize = true
        };

        lblItemCount = new Label
        {
            Text = "Tổng mặt hàng: 0 sản phẩm",
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(15, 45),
            AutoSize = true
        };

        lblSubTotal = new Label
        {
            Text = "Tiền hàng (chưa VAT): 0 VNĐ",
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(15, 75),
            AutoSize = true
        };

        lblTotalVat = new Label
        {
            Text = "Thuế VAT (10%): 0 VNĐ",
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(15, 105),
            AutoSize = true
        };

        var lblDivider = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(15, 140),
            Size = new Size(320, 2)
        };

        lblGrandTotal = new Label
        {
            Text = "0 VNĐ",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            Location = new Point(15, 160),
            Size = new Size(320, 45),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlSummary.Controls.AddRange(new Control[] {
            lblSummaryTitle, lblItemCount, lblSubTotal, lblTotalVat, lblDivider, lblGrandTotal
        });
        pnlRight.Controls.Add(pnlSummary);

        Controls.Add(pnlRight);
    }

    private void SetupGridColumns()
    {
        dgvCart.Columns.Clear();
        dgvCart.Columns.Add("STT", "STT");
        dgvCart.Columns.Add("Barcode", "Mã Vạch");
        dgvCart.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Unit", "ĐVT");
        dgvCart.Columns.Add("Price", "Đơn Giá (VNĐ)");
        dgvCart.Columns.Add("Quantity", "Số Lượng");
        dgvCart.Columns.Add("VatAmount", "Thuế VAT (10%)");
        dgvCart.Columns.Add("TotalAmount", "Thành Tiền (VNĐ)");

        dgvCart.Columns[0].Width = 45;
        dgvCart.Columns[1].Width = 130;
        dgvCart.Columns[2].Width = 200;
        dgvCart.Columns[3].Width = 60;
        dgvCart.Columns[4].Width = 110;
        dgvCart.Columns[5].Width = 75;
        dgvCart.Columns[6].Width = 110;
        dgvCart.Columns[7].Width = 130;
    }

    private async void ProcessScanBarcode(string rawBarcode)
    {
        if (string.IsNullOrWhiteSpace(rawBarcode))
        {
            return;
        }

        string barcode = rawBarcode.Trim();
        txtBarcode.Text = string.Empty;

        CartItem? productItem = await FetchProductByBarcodeAsync(barcode);

        if (productItem == null)
        {
            SystemSounds.Asterisk.Play();
            lblStatusMessage.Text = $"⚠️ LỖI: Mã vạch '{barcode}' không tồn tại hoặc đã bị ngừng kinh doanh!";
            lblStatusMessage.ForeColor = Color.Red;
            return;
        }

        // Play scanner Beep sound
        SystemSounds.Beep.Play();

        // Update Product Preview Panel
        UpdateProductPreview(productItem);

        // Check if barcode already exists in Cart (Scan Same Barcode Flow)
        var existingItem = _cartItems.FirstOrDefault(i => i.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            // Flow requirement: Quantity++ without adding new row
            existingItem.Quantity++;
            lblStatusMessage.Text = $"✅ Đã tăng số lượng sản phẩm '{existingItem.ProductName}' lên {existingItem.Quantity}.";
            lblStatusMessage.ForeColor = Color.DarkBlue;
        }
        else
        {
            _cartItems.Add(productItem);
            lblStatusMessage.Text = $"✅ Đã thêm sản phẩm '{productItem.ProductName}' vào giỏ hàng.";
            lblStatusMessage.ForeColor = Color.DarkGreen;
        }

        RefreshCartGrid();
        txtBarcode.Focus();
    }

    private void BtnAddNewProduct_Click(object? sender, EventArgs e)
    {
        using var form = new AddProductForm(_httpClient, _apiBaseUrl);
        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedProduct != null)
        {
            var prod = form.CreatedProduct;
            var item = new CartItem
            {
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Barcode = prod.Barcode,
                Price = prod.Price,
                Unit = prod.Unit,
                StockQuantity = 100
            };

            _customProducts[prod.Barcode] = item;

            txtBarcode.Text = prod.Barcode;
            ProcessScanBarcode(prod.Barcode);
        }
    }

    private async Task<CartItem?> FetchProductByBarcodeAsync(string barcode)
    {
        if (_customProducts.TryGetValue(barcode, out var customItem))
        {
            return new CartItem
            {
                ProductId = customItem.ProductId,
                ProductName = customItem.ProductName,
                Barcode = customItem.Barcode,
                Price = customItem.Price,
                Unit = customItem.Unit,
                StockQuantity = customItem.StockQuantity,
                Quantity = 1
            };
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/products/barcode/{barcode}");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("isSuccess").GetBoolean() && root.HasProperty("data"))
                {
                    var data = root.GetProperty("data");
                    return new CartItem
                    {
                        ProductId = data.GetProperty("productId").GetInt32(),
                        ProductName = data.GetProperty("productName").GetString() ?? string.Empty,
                        Barcode = data.GetProperty("barcode").GetString() ?? barcode,
                        Price = data.GetProperty("price").GetDecimal(),
                        Unit = data.GetProperty("unit").GetString() ?? "cái",
                        StockQuantity = 100
                    };
                }
            }
        }
        catch
        {
            // Offline / Fallback Demo Data if Backend API is not currently running
        }

        return GetFallbackDemoProduct(barcode);
    }

    private static CartItem? GetFallbackDemoProduct(string barcode)
    {
        return barcode switch
        {
            "8935001800012" => new CartItem
            {
                ProductId = 1,
                ProductName = "Nước ngọt Coca-Cola Lon 330ml",
                Barcode = "8935001800012",
                Price = 10000,
                Unit = "lon",
                StockQuantity = 150
            },
            "8934673123456" => new CartItem
            {
                ProductId = 2,
                ProductName = "Sữa tươi Vinamilk Có đường 1L",
                Barcode = "8934673123456",
                Price = 36000,
                Unit = "hộp",
                StockQuantity = 85
            },
            "8935001800099" => new CartItem
            {
                ProductId = 3,
                ProductName = "Bánh Quy Oreo Vị Vani 133g",
                Barcode = "8935001800099",
                Price = 18500,
                Unit = "gói",
                StockQuantity = 200
            },
            _ => null
        };
    }

    private void UpdateProductPreview(CartItem item)
    {
        lblProductName.Text = item.ProductName;
        lblBarcode.Text = $"Barcode: {item.Barcode}";
        lblPrice.Text = $"Đơn giá: {item.Price:N0} VNĐ / {item.Unit}";
        lblVat.Text = $"VAT (10%): {item.Price * 0.1m:N0} VNĐ";
        lblStock.Text = $"Tồn kho: {item.StockQuantity} {item.Unit}";

        // Draw simple visual thumbnail icon on PictureBox
        var bmp = new Bitmap(110, 110);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(230, 247, 255));
            g.DrawRectangle(Pens.LightBlue, 0, 0, 109, 109);
            g.DrawString("📦", new Font("Segoe UI", 36), Brushes.SteelBlue, new PointF(25, 20));
        }
        picProduct.Image = bmp;
    }

    private void RefreshCartGrid()
    {
        dgvCart.Rows.Clear();
        decimal subTotal = 0;
        decimal totalVat = 0;
        int totalItems = 0;

        for (int i = 0; i < _cartItems.Count; i++)
        {
            var item = _cartItems[i];
            dgvCart.Rows.Add(
                (i + 1).ToString(),
                item.Barcode,
                item.ProductName,
                item.Unit,
                item.Price.ToString("N0"),
                item.Quantity.ToString(),
                item.VatAmount.ToString("N0"),
                item.TotalAmount.ToString("N0")
            );

            subTotal += item.SubTotal;
            totalVat += item.VatAmount;
            totalItems += item.Quantity;
        }

        lblItemCount.Text = $"Tổng mặt hàng: {_cartItems.Count} sản phẩm ({totalItems} món)";
        lblSubTotal.Text = $"Tiền hàng (chưa VAT): {subTotal:N0} VNĐ";
        lblTotalVat.Text = $"Thuế VAT (10%): {totalVat:N0} VNĐ";
        lblGrandTotal.Text = $"{(subTotal + totalVat):N0} VNĐ";
    }

    private void BtnPlus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int index = dgvCart.CurrentRow.Index;
        if (index >= 0 && index < _cartItems.Count)
        {
            _cartItems[index].Quantity++;
            RefreshCartGrid();
        }
    }

    private void BtnMinus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int index = dgvCart.CurrentRow.Index;
        if (index >= 0 && index < _cartItems.Count)
        {
            if (_cartItems[index].Quantity > 1)
            {
                _cartItems[index].Quantity--;
                RefreshCartGrid();
            }
        }
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null || dgvCart.CurrentRow.Index < 0) return;
        int index = dgvCart.CurrentRow.Index;
        if (index >= 0 && index < _cartItems.Count)
        {
            var name = _cartItems[index].ProductName;
            _cartItems.RemoveAt(index);
            RefreshCartGrid();
            lblStatusMessage.Text = $"Đã xóa '{name}' khỏi hóa đơn.";
            lblStatusMessage.ForeColor = Color.Brown;
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2)
        {
            BtnAddNewProduct_Click(sender, e);
        }
    }

    private void Form1_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // Scanner High-Speed Keyboard Buffer Detection (Barcode.md)
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
                string code = _scanBuffer.ToString();
                _scanBuffer.Clear();
                ProcessScanBarcode(code);
            }
        }
        else
        {
            _scanBuffer.Append(e.KeyChar);
        }
    }
}

internal static class JsonElementExtensions
{
    public static bool HasProperty(this JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out _);
    }
}
