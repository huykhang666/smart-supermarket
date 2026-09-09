using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class CategoriesView : UserControl
{
    private TreeView tvCategories = null!;
    private Panel pnlRightDetail = null!;
    private TextBox txtCategoryName = null!;
    private TextBox txtSlug = null!;
    private TextBox txtDescription = null!;
    private Button btnSave = null!;
    private Button btnDelete = null!;
    private Button btnAddChild = null!;

    public CategoriesView()
    {
        InitializeComponent();
        LoadCategoryTree();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        // --- Left Tree Panel ---
        var pnlLeftTree = new Panel
        {
            Dock = DockStyle.Left,
            Width = 380,
            BackColor = Color.White,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 15, 0)
        };

        var lblTreeHeader = new Label
        {
            Text = "📁 CÂY DANH MỤC SẢN PHẨM",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            Dock = DockStyle.Top,
            Height = 35
        };

        tvCategories = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None
        };
        tvCategories.AfterSelect += TvCategories_AfterSelect;

        pnlLeftTree.Controls.Add(tvCategories);
        pnlLeftTree.Controls.Add(lblTreeHeader);

        // --- Right Detail Panel ---
        pnlRightDetail = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(25)
        };

        var lblDetailHeader = new Label
        {
            Text = "CHI TIẾT DANH MỤC CHỌN",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 21, 41),
            AutoSize = true,
            Location = new Point(25, 20)
        };

        var lblName = new Label { Text = "Tên danh mục:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(25, 75), AutoSize = true };
        txtCategoryName = new TextBox { Font = new Font("Segoe UI", 10.5f), Location = new Point(25, 100), Size = new Size(420, 32), BorderStyle = BorderStyle.FixedSingle };

        var lblSlug = new Label { Text = "Slug (SEO):", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(25, 150), AutoSize = true };
        txtSlug = new TextBox { Font = new Font("Segoe UI", 10.5f), Location = new Point(25, 175), Size = new Size(420, 32), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, BackColor = Color.FromArgb(245, 247, 250) };

        var lblDesc = new Label { Text = "Mô tả chi tiết:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(25, 225), AutoSize = true };
        txtDescription = new TextBox { Font = new Font("Segoe UI", 10f), Location = new Point(25, 250), Size = new Size(420, 80), Multiline = true, BorderStyle = BorderStyle.FixedSingle };

        btnSave = new Button
        {
            Text = "💾 Lưu Thay Đổi",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 38),
            Location = new Point(25, 350),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã lưu thay đổi danh mục!");

        btnAddChild = new Button
        {
            Text = "➕ Thêm Danh Mục Con",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(56, 158, 13),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 38),
            Location = new Point(165, 350),
            Cursor = Cursors.Hand
        };
        btnAddChild.FlatAppearance.BorderSize = 0;
        btnAddChild.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng nhập thông tin danh mục con mới.");

        btnDelete = new Button
        {
            Text = "🗑️ Xóa Danh Mục",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(207, 19, 34),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 38),
            Location = new Point(345, 350),
            Cursor = Cursors.Hand
        };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Chức năng xóa yêu cầu xác nhận Admin.");

        pnlRightDetail.Controls.Add(lblDetailHeader);
        pnlRightDetail.Controls.Add(lblName);
        pnlRightDetail.Controls.Add(txtCategoryName);
        pnlRightDetail.Controls.Add(lblSlug);
        pnlRightDetail.Controls.Add(txtSlug);
        pnlRightDetail.Controls.Add(lblDesc);
        pnlRightDetail.Controls.Add(txtDescription);
        pnlRightDetail.Controls.Add(btnSave);
        pnlRightDetail.Controls.Add(btnAddChild);
        pnlRightDetail.Controls.Add(btnDelete);

        this.Controls.Add(pnlRightDetail);
        this.Controls.Add(pnlLeftTree);
    }

    private void LoadCategoryTree()
    {
        tvCategories.Nodes.Clear();

        var root1 = new TreeNode("Nước Giải Khát & Đồ Uống") { Tag = "nuoc-giai-khat" };
        root1.Nodes.Add(new TreeNode("Nước Ngọt Có Ga") { Tag = "nuoc-ngot-co-ga" });
        root1.Nodes.Add(new TreeNode("Nước Tinh Khiết & Khoáng") { Tag = "nuoc-tinh-khiet" });
        root1.Nodes.Add(new TreeNode("Trà & Cà Phê Đóng Chai") { Tag = "tra-ca-phe" });

        var root2 = new TreeNode("Sữa & Sản Phẩm Từ Sữa") { Tag = "sua-che-pham" };
        root2.Nodes.Add(new TreeNode("Sữa Tươi Tiệt Trùng") { Tag = "sua-tuoi" });
        root2.Nodes.Add(new TreeNode("Sữa Chua & Váng Sữa") { Tag = "sua-chua" });

        var root3 = new TreeNode("Rau Củ Quả Tươi Sạch") { Tag = "rau-cu-qua" };
        root3.Nodes.Add(new TreeNode("Rau Ăn Lá Đóng Gói") { Tag = "rau-an-la" });
        root3.Nodes.Add(new TreeNode("Trái Cây Nội Địa") { Tag = "trai-cay-noi-dia" });

        tvCategories.Nodes.Add(root1);
        tvCategories.Nodes.Add(root2);
        tvCategories.Nodes.Add(root3);
        tvCategories.ExpandAll();
    }

    private void TvCategories_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node != null)
        {
            txtCategoryName.Text = e.Node.Text;
            txtSlug.Text = e.Node.Tag?.ToString() ?? "";
            txtDescription.Text = $"Danh mục {e.Node.Text} thuộc hệ thống Smart Supermarket";
        }
    }
}
