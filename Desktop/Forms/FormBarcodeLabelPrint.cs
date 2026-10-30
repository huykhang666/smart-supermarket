using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Desktop.Services;

namespace Desktop.Forms;

public class FormBarcodeLabelPrint : Form
{
    private ComboBox cboProducts = null!;
    private NumericUpDown numWeightKg = null!;
    private NumericUpDown numUnitPrice = null!;
    private NumericUpDown numCopies = null!;
    private TextBox txtGeneratedBarcode = null!;
    private Label lblTotalAmount = null!;
    private Button btnPreview = null!;
    private Button btnPrint = null!;
    private Button btnClose = null!;

    public class ProductItemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerKg { get; set; }
        public string BaseBarcode { get; set; } = string.Empty;

        public override string ToString() => $"{Name} ({PricePerKg:N0} đ/kg)";
    }

    private readonly List<ProductItemInfo> _sampleFreshProducts = new()
    {
        new ProductItemInfo { Id = 1, Name = "Thịt Ba Rọi Heo CP", PricePerKg = 140000, BaseBarcode = "2000001" },
        new ProductItemInfo { Id = 2, Name = "Sườn Non Heo Tươi", PricePerKg = 185000, BaseBarcode = "2000002" },
        new ProductItemInfo { Id = 3, Name = "Thịt Bò Úc Cắt Lát", PricePerKg = 290000, BaseBarcode = "2000003" },
        new ProductItemInfo { Id = 4, Name = "Cá Hồi Fillet Tươi", PricePerKg = 450000, BaseBarcode = "2000004" },
        new ProductItemInfo { Id = 5, Name = "Rau Cải Xanh VietGAP", PricePerKg = 25000, BaseBarcode = "2000005" },
        new ProductItemInfo { Id = 6, Name = "Táo Envy New Zealand", PricePerKg = 120000, BaseBarcode = "2000006" },
        new ProductItemInfo { Id = 7, Name = "Cam Sành Bến Tre", PricePerKg = 35000, BaseBarcode = "2000007" }
    };

    public FormBarcodeLabelPrint(List<ProductItemInfo>? customProducts = null)
    {
        InitializeComponent();
        if (customProducts != null && customProducts.Count > 0)
        {
            _sampleFreshProducts = customProducts;
        }
        PopulateProducts();
    }

    private void InitializeComponent()
    {
        this.Text = "In Tem Nhãn Barcode Hàng Tươi Sống Cân Ký (GS1 Label Printer)";
        this.Size = new Size(540, 420);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(14, 116, 144),
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblTitle = new Label
        {
            Text = "🏷️ IN TEM NHÃN BARCODE HÀNG CÂN KÝ (50x30mm)",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(14, 16)
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 15, 20, 15)
        };

        int y = 15;
        void AddLabel(string text)
        {
            var lbl = new Label { Text = text, Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlBody.Controls.Add(lbl);
        }

        AddLabel("Chọn Mặt Hàng Tươi Sống:");
        y += 22;
        cboProducts = new ComboBox
        {
            Location = new Point(20, y),
            Size = new Size(480, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboProducts.SelectedIndexChanged += (s, e) => OnProductChanged();
        pnlBody.Controls.Add(cboProducts);
        y += 38;

        AddLabel("Đơn Giá Bán (VNĐ / kg):");
        var lblWeight = new Label { Text = "Khối Lượng Thực Tế (kg):", Location = new Point(270, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        pnlBody.Controls.Add(lblWeight);
        y += 22;

        numUnitPrice = new NumericUpDown
        {
            Location = new Point(20, y),
            Size = new Size(220, 28),
            Maximum = 10000000,
            Increment = 5000,
            ThousandsSeparator = true
        };
        numUnitPrice.ValueChanged += (s, e) => Recalculate();
        pnlBody.Controls.Add(numUnitPrice);

        numWeightKg = new NumericUpDown
        {
            Location = new Point(270, y),
            Size = new Size(230, 28),
            DecimalPlaces = 3,
            Increment = 0.050m,
            Maximum = 100m,
            Value = 0.450m
        };
        numWeightKg.ValueChanged += (s, e) => Recalculate();
        pnlBody.Controls.Add(numWeightKg);
        y += 38;

        AddLabel("Mã Vạch Cân Ký (GS1 EAN-13):");
        var lblCopies = new Label { Text = "Số lượng bản in:", Location = new Point(340, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        pnlBody.Controls.Add(lblCopies);
        y += 22;

        txtGeneratedBarcode = new TextBox
        {
            Location = new Point(20, y),
            Size = new Size(300, 28),
            ReadOnly = true,
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            BackColor = Color.White
        };
        pnlBody.Controls.Add(txtGeneratedBarcode);

        numCopies = new NumericUpDown
        {
            Location = new Point(340, y),
            Size = new Size(160, 28),
            Minimum = 1,
            Maximum = 100,
            Value = 1
        };
        pnlBody.Controls.Add(numCopies);
        y += 42;

        lblTotalAmount = new Label
        {
            Text = "THÀNH TIỀN: 63,000 VNĐ",
            Location = new Point(20, y),
            Size = new Size(480, 28),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(217, 83, 79),
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlBody.Controls.Add(lblTotalAmount);

        // Footer buttons
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        btnPreview = new Button
        {
            Text = "👁️ Xem Trước Tem",
            Size = new Size(150, 34),
            Location = new Point(160, 10),
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPreview.Click += (s, e) => PrintLabel(direct: false);

        btnPrint = new Button
        {
            Text = "🖨️ In Ngay",
            Size = new Size(110, 34),
            Location = new Point(320, 10),
            BackColor = Color.FromArgb(16, 149, 193),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPrint.Click += (s, e) => PrintLabel(direct: true);

        btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(80, 34),
            Location = new Point(440, 10),
            BackColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => this.Close();

        pnlFooter.Controls.AddRange(new Control[] { btnPreview, btnPrint, btnClose });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private void PopulateProducts()
    {
        cboProducts.Items.Clear();
        foreach (var p in _sampleFreshProducts)
        {
            cboProducts.Items.Add(p);
        }
        if (cboProducts.Items.Count > 0)
        {
            cboProducts.SelectedIndex = 0;
        }
    }

    private void OnProductChanged()
    {
        if (cboProducts.SelectedItem is ProductItemInfo prod)
        {
            numUnitPrice.Value = prod.PricePerKg;
            Recalculate();
        }
    }

    private void Recalculate()
    {
        if (cboProducts.SelectedItem is not ProductItemInfo prod) return;

        decimal total = Math.Round(numWeightKg.Value * numUnitPrice.Value, 0);
        lblTotalAmount.Text = $"THÀNH TIỀN: {total:N0} VNĐ ({numWeightKg.Value:F3} kg x {numUnitPrice.Value:N0} đ/kg)";

        // Generate GS1-13 weighted barcode: 20 + PPPPP (5 digits) + WWWWW (grams 5 digits) + CheckDigit
        int grams = (int)Math.Round(numWeightKg.Value * 1000m, 0);
        string first12 = $"20{prod.Id:D5}{grams:D5}";
        char checkDigit = CalculateEanCheck(first12);
        txtGeneratedBarcode.Text = $"{first12}{checkDigit}";
    }

    private static char CalculateEanCheck(string first12)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = first12[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        int check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    private void PrintLabel(bool direct)
    {
        if (cboProducts.SelectedItem is not ProductItemInfo prod) return;

        var model = new BarcodeLabelPrintModel
        {
            ProductName = prod.Name,
            Barcode = txtGeneratedBarcode.Text,
            WeightKg = numWeightKg.Value,
            UnitPrice = numUnitPrice.Value,
            PackagingDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddDays(3),
            Origin = "Việt Nam",
            StoreName = "SMART SUPERMARKET"
        };

        BarcodeLabelPrinterService.PrintOrPreview(model, copies: (int)numCopies.Value, directPrint: direct);
    }
}
