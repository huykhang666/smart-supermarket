using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class AdminUsersView : UserControl
{
    private DataGridView dgvUsers = null!;
    private Button btnAddUser = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public AdminUsersView()
    {
        InitializeComponent();
        _ = LoadUsersAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlTop);

        var lblHeader = new Label
        {
            Text = "👤 QUẢN LÝ NHÂN VIÊN & TÀI KHOẢN HỆ THỐNG",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            AutoSize = true,
            Location = new Point(15, 18)
        };

        btnAddUser = new Button
        {
            Text = "➕ Tạo Nhân Viên Mới",
            Size = new Size(180, 36),
            Location = new Point(pnlTop.Width - 195, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.ApplyPrimaryButton(btnAddUser);
        btnAddUser.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Mở form tạo tài khoản nhân viên mới...");

        pnlTop.Controls.Add(lblHeader);
        pnlTop.Controls.Add(btnAddUser);

        dgvUsers = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvUsers);

        dgvUsers.Columns.Add("UserId", "ID");
        dgvUsers.Columns.Add("Username", "Tên Đăng Nhập");
        dgvUsers.Columns.Add("FullName", "Họ Và Tên");
        dgvUsers.Columns.Add("Email", "Email");
        dgvUsers.Columns.Add("PhoneNumber", "Số Điện Thoại");
        dgvUsers.Columns.Add("Role", "Vai Trò (Role)");
        dgvUsers.Columns.Add("Branch", "Chi Nhánh");
        dgvUsers.Columns.Add("Status", "Trạng Thái");

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGridContainer);
        pnlGridContainer.Controls.Add(dgvUsers);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlTop);
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            dgvUsers.Rows.Clear();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/employees");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    foreach (var item in dataElem.EnumerateArray())
                    {
                        int id = item.TryGetProperty("id", out var idP) ? idP.GetInt32() : 0;
                        string user = item.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                        string name = item.TryGetProperty("fullName", out var n) ? n.GetString() ?? "" : "";
                        string email = item.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
                        string phone = item.TryGetProperty("phone", out var p) ? p.GetString() ?? "" : "";
                        string role = item.TryGetProperty("role", out var r) ? r.GetString() ?? "" : "Staff";
                        string status = item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "🟢 Active";

                        dgvUsers.Rows.Add(id, user, name, email, phone, role, "Chi Nhánh 01", status);
                    }
                }
            }

        }
        catch
        {
            // Graceful fallback
        }
    }
}
