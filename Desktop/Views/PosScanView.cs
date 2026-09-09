using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class PosScanView : UserControl
{
    private TextBox txtBarcode = null!;
    private Button btnScan = null!;
    private DataGridView dgvCart = null!;
    private Label lblGrandTotal = null!;
    private Button btnCheckout = null!;

    public PosScanView()
    {
        InitializeComponent();
        LoadSampleCart();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        // --- Top Bar (Barcode Input) ---
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var lblScan = new Label { Text = "📷 QUÉT MÃ VẠCH (POS SCAN):", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true, ForeColor = Color.FromArgb(9, 109, 217) };
        txtBarcode = new TextBox { Font = new Font("Segoe UI", 12f), Location = new Point(230, 16), Size = new Size(360, 34), BorderStyle = BorderStyle.FixedSingle };
        
        btnScan = new Button
        {
            Text = "THÊM VÀO GIỎ (ENTER)",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 34),
            Location = new Point(600, 16),
            Cursor = Cursors.Hand
        };
        btnScan.FlatAppearance.BorderSize = 0;
        btnScan.Click += BtnScan_Click;

        pnlTop.Controls.Add(lblScan);
        pnlTop.Controls.Add(txtBarcode);
        pnlTop.Controls.Add(btnScan);

        // --- Bottom Total Bar ---
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Color.FromArgb(0, 21, 41),
            Padding = new Padding(20)
        };

        lblGrandTotal = new Label
        {
            Text = "TỔNG THANH TOÁN: 58.000 VNĐ",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 197, 61),
            AutoSize = true,
            Location = new Point(20, 22)
        };

        btnCheckout = new Button
        {
            Text = "💳 THANH TOÁN (F5)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(56, 158, 13),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 46),
            Location = new Point(pnlBottom.Width - 250, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Thanh toán thành công! Đang in hóa đơn...");

        pnlBottom.Controls.Add(lblGrandTotal);
        pnlBottom.Controls.Add(btnCheckout);

        // --- DataGrid Cart ---
        dgvCart = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 42 },
            ColumnHeadersHeight = 44
        };

        dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 70, 80);
        dgvCart.EnableHeadersVisualStyles = false;

        dgvCart.Columns.Add("Barcode", "Mã Vạch");
        dgvCart.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvCart.Columns.Add("Price", "Đơn Giá");
        dgvCart.Columns.Add("Quantity", "Số Lượng");
        dgvCart.Columns.Add("Vat", "Thuế VAT");
        dgvCart.Columns.Add("Total", "Thành Tiền");

        this.Controls.Add(dgvCart);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlTop);
    }

    private void LoadSampleCart()
    {
        dgvCart.Rows.Clear();
        dgvCart.Rows.Add("8935001800012", "Nước ngọt Coca-Cola Lon 330ml", "10.000 đ", "2 lon", "10%", "22.000 đ");
        dgvCart.Rows.Add("8934673123456", "Sữa tươi Vinamilk Có đường 1L", "36.000 đ", "1 hộp", "10%", "39.600 đ");
    }

    private void BtnScan_Click(object? sender, EventArgs e)
    {
        string code = txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        AntdUI.Message.info(this.FindForm() ?? new Form(), $"Đã thêm sản phẩm có mã vạch: {code} vào giỏ hàng!");
        txtBarcode.Clear();
    }
}
