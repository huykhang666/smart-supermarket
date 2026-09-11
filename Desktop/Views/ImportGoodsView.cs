using System;
using System.Drawing;
using System.Windows.Forms;

namespace Desktop.Views;

public class ImportGoodsView : UserControl
{
    private Panel pnlTop = null!;
    private DataGridView dgvImportItems = null!;

    public ImportGoodsView()
    {
        InitializeComponent();
        LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlTop);

        var lblTitle = new Label
        {
            Text = "📥 NHẬP HÀNG KHO SIÊU THỊ (IMPORT GOODS & INVOICE)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 12),
            AutoSize = true
        };

        var lblSupplier = new Label { Text = "Nhà cung cấp:", Location = new Point(15, 48), AutoSize = true, Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary };
        var cboSupplier = new ComboBox
        {
            Font = ThemeManager.BodyFont,
            Size = new Size(240, 30),
            Location = new Point(125, 44),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboSupplier.Items.AddRange(new[] { "Công ty TNHH Coca-Cola Việt Nam", "Công ty Cổ phần Sữa Vinamilk", "Công ty Nestle Việt Nam" });
        cboSupplier.SelectedIndex = 0;

        var lblInvoiceCode = new Label { Text = "Mã HD Nhập:", Location = new Point(385, 48), AutoSize = true, Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary };
        var txtInvoiceCode = new TextBox { Text = "IMP-2026-0048", Font = ThemeManager.BodyFont, Size = new Size(150, 30), Location = new Point(485, 44), BorderStyle = BorderStyle.FixedSingle };

        var btnAddImport = new Button
        {
            Text = "➕ Tạo Đơn Nhập Hàng",
            Size = new Size(190, 36),
            Location = new Point(pnlTop.Width - 210, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.ApplyPrimaryButton(btnAddImport);
        btnAddImport.Click += (s, e) => {
            var parentForm = this.FindForm();
            if (parentForm != null) AntdUI.Message.success(parentForm, "Đã lưu đơn nhập hàng kho siêu thị thành công!");
            else MessageBox.Show("Đã lưu đơn nhập hàng kho siêu thị thành công!", "Thông báo");
        };

        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(lblSupplier);
        pnlTop.Controls.Add(cboSupplier);
        pnlTop.Controls.Add(lblInvoiceCode);
        pnlTop.Controls.Add(txtInvoiceCode);
        pnlTop.Controls.Add(btnAddImport);

        dgvImportItems = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvImportItems);

        dgvImportItems.Columns.Add("Barcode", "Mã Vạch");
        dgvImportItems.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvImportItems.Columns.Add("Quantity", "Số Lượng Nhập");
        dgvImportItems.Columns.Add("CostPrice", "Giá Nhập (VNĐ)");
        dgvImportItems.Columns.Add("TotalPrice", "Tổng Giá Trị (VNĐ)");
        dgvImportItems.Columns.Add("ExpiryDate", "Hạn Sử Dụng");

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGridContainer);
        pnlGridContainer.Controls.Add(dgvImportItems);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlTop);
    }

    private void LoadSampleData()
    {
        dgvImportItems.Rows.Clear();
        dgvImportItems.Rows.Add("8935001800012", "Nước ngọt Coca-Cola Lon 330ml", "500 lon", "7.500 đ", "3.750.000 đ", "2027-03-15");
        dgvImportItems.Rows.Add("8934673123456", "Sữa tươi tiệt trùng Vinamilk 1L", "200 hộp", "29.000 đ", "5.800.000 đ", "2026-11-20");
    }
}
