using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class SuppliersView : UserControl
{
    private DataGridView dgvSuppliers = null!;
    private Button btnAddSupplier = null!;
    private Button btnImportSupplier = null!;

    public SuppliersView()
    {
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var lblHeader = new Label
        {
            Text = "🏢 QUẢN LÝ NHÀ CUNG CẤP & ĐỐI TÁC",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(15, 16)
        };

        btnAddSupplier = new Button
        {
            Text = "➕ Thêm Nhà Cung Cấp",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 34),
            Location = new Point(pnlTop.Width - 360, 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnAddSupplier.FlatAppearance.BorderSize = 0;
        btnAddSupplier.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Mở form thêm mới nhà cung cấp...");

        btnImportSupplier = new Button
        {
            Text = "📥 Import CSV/JSON",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(56, 158, 13),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 34),
            Location = new Point(pnlTop.Width - 185, 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnImportSupplier.FlatAppearance.BorderSize = 0;
        btnImportSupplier.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Chọn file CSV để import nhà cung cấp...");

        pnlTop.Controls.Add(lblHeader);
        pnlTop.Controls.Add(btnAddSupplier);
        pnlTop.Controls.Add(btnImportSupplier);

        dgvSuppliers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 40 },
            ColumnHeadersHeight = 42
        };

        dgvSuppliers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvSuppliers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvSuppliers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 70, 80);
        dgvSuppliers.EnableHeadersVisualStyles = false;

        dgvSuppliers.Columns.Add("SupplierId", "ID");
        dgvSuppliers.Columns.Add("SupplierCode", "Mã NCC");
        dgvSuppliers.Columns.Add("SupplierName", "Tên Nhà Cung Cấp");
        dgvSuppliers.Columns.Add("ContactPerson", "Người Liên Hệ");
        dgvSuppliers.Columns.Add("PhoneNumber", "Số Điện Thoại");
        dgvSuppliers.Columns.Add("Email", "Email");
        dgvSuppliers.Columns.Add("Rating", "Đánh Giá (Rating)");
        dgvSuppliers.Columns.Add("Status", "Trạng Thái");

        this.Controls.Add(dgvSuppliers);
        this.Controls.Add(pnlTop);
    }

    private void LoadData()
    {
        dgvSuppliers.Rows.Clear();
        dgvSuppliers.Rows.Add(1, "SUP00001", "Công ty TNHH Coca-Cola Việt Nam", "Nguyễn Văn A", "0901234567", "cocacola@supplier.com", "⭐ 4.8 / 5.0", "🟢 Hoạt động");
        dgvSuppliers.Rows.Add(2, "SUP00002", "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", "Trần Thị B", "0912345678", "vinamilk@supplier.com", "⭐ 4.9 / 5.0", "🟢 Hoạt động");
        dgvSuppliers.Rows.Add(3, "SUP00003", "Công ty TNHH Nông Nghiệp Sạch Đà Lạt", "Lê Văn C", "0933445566", "dalatfarm@supplier.com", "⭐ 4.6 / 5.0", "🟢 Hoạt động");
        dgvSuppliers.Rows.Add(4, "SUP00004", "Công ty Cổ phần Thực phẩm Kinh Đô", "Phạm Thị D", "0977889900", "kinhdo@supplier.com", "⭐ 4.5 / 5.0", "🔴 Tạm dừng");
    }
}
