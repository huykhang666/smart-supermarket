using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class AdminUsersView : UserControl
{
    private DataGridView dgvUsers = null!;
    private Button btnAddUser = null!;

    public AdminUsersView()
    {
        InitializeComponent();
        LoadUsers();
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
            Text = "👤 QUẢN LÝ NHÂN VIÊN & TÀI KHOẢN HỆ THỐNG",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(15, 16)
        };

        btnAddUser = new Button
        {
            Text = "➕ Tạo Nhân Viên Mới",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 34),
            Location = new Point(pnlTop.Width - 185, 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnAddUser.FlatAppearance.BorderSize = 0;
        btnAddUser.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Mở form tạo tài khoản nhân viên mới...");

        pnlTop.Controls.Add(lblHeader);
        pnlTop.Controls.Add(btnAddUser);

        dgvUsers = new DataGridView
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

        dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 70, 80);
        dgvUsers.EnableHeadersVisualStyles = false;

        dgvUsers.Columns.Add("UserId", "ID");
        dgvUsers.Columns.Add("Username", "Tên Đăng Nhập");
        dgvUsers.Columns.Add("FullName", "Họ Vụ Tên");
        dgvUsers.Columns.Add("Email", "Email");
        dgvUsers.Columns.Add("PhoneNumber", "Số Điện Thoại");
        dgvUsers.Columns.Add("Role", "Vai Trò (Role)");
        dgvUsers.Columns.Add("Branch", "Chi Nhánh");
        dgvUsers.Columns.Add("Status", "Trạng Thái");

        this.Controls.Add(dgvUsers);
        this.Controls.Add(pnlTop);
    }

    private void LoadUsers()
    {
        dgvUsers.Rows.Clear();
        dgvUsers.Rows.Add(1, "admin", "Nguyễn Văn An", "admin@smartmarket.vn", "0901234567", "👑 Admin", "Toàn Hệ Thống", "🟢 Active");
        dgvUsers.Rows.Add(2, "manager_01", "Trần Thị Quản Lý", "manager01@smartmarket.vn", "0912345678", "💼 Manager", "CN Quận 7", "🟢 Active");
        dgvUsers.Rows.Add(3, "staff_pos_01", "Lê Văn Thu Ngân", "thungan01@smartmarket.vn", "0933445566", "🛒 Staff", "CN Thủ Đức", "🟢 Active");
        dgvUsers.Rows.Add(4, "staff_pos_02", "Phạm Văn Kho", "kho02@smartmarket.vn", "0977889900", "📦 Staff", "CN Bình Tân", "🔴 Locked");
    }
}
