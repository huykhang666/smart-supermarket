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
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public AdminUsersView()
    {
        InitializeComponent();
        _ = LoadUsersAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlTop);

        var lblHeader = new Label
        {
            Text = "👤 Tài Khoản Nhân Viên & Quản Trị Hệ Thống",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var pnlActionsRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        btnAddUser = new Button
        {
            Text = "➕ Tạo Nhân Viên Mới",
            Size = new Size(180, 32)
        };
        AppTheme.ApplyPrimaryButton(btnAddUser);
        btnAddUser.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Mở form tạo tài khoản nhân viên mới...");
        pnlActionsRight.Controls.Add(btnAddUser);

        pnlTop.Controls.Add(lblHeader);
        pnlTop.Controls.Add(pnlActionsRight);

        dgvUsers = new DataGridView();
        AppTheme.ApplyGridStyle(dgvUsers);

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
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlGridContainer);
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
