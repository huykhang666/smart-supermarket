using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class EmployeesView : UserControl
{
    private TabControl tabControl = null!;
    private TabPage tabEmployees = null!;
    private TabPage tabShifts = null!;
    private TabPage tabAttendance = null!;

    private DataGridView dgvEmployees = null!;
    private DataGridView dgvShifts = null!;
    private DataGridView dgvAttendance = null!;

    public EmployeesView()
    {
        InitializeComponent();
        LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 244, 248);
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var lblTitle = new Label
        {
            Text = "👥 QUẢN LÝ NHÂN VIÊN, CA TRỰC & CHẤM CÔNG (EMPLOYEE & ATTENDANCE)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 37, 69),
            Location = new Point(15, 12),
            AutoSize = true
        };

        var btnAddEmployee = new IconButton
        {
            Text = " + Tạo Tài Khoản Nhân Viên",
            IconChar = IconChar.UserPlus,
            IconColor = Color.White,
            IconSize = 18,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0, 168, 204),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 36),
            Location = new Point(15, 36),
            Cursor = Cursors.Hand
        };
        btnAddEmployee.FlatAppearance.BorderSize = 0;
        btnAddEmployee.Click += (s, e) => {
            var parentForm = this.FindForm();
            if (parentForm != null) AntdUI.Message.info(parentForm, "Form tạo tài khoản nhân viên mới (Admin cấp Mã NV, Username, Mật khẩu & Phân ca).");
            else MessageBox.Show("Form tạo tài khoản nhân viên mới (Admin cấp Mã NV, Username, Mật khẩu & Phân ca).", "Thông báo");
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnAddEmployee);

        // TabControl
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };

        tabEmployees = new TabPage("📋 Danh Sách Nhân Viên");
        tabShifts = new TabPage("⏰ Quản Lý Ca Trực (Shift)");
        tabAttendance = new TabPage("⏱️ Bảng Chấm Công (Attendance)");

        // Grid Employees
        dgvEmployees = CreateStyledGrid();
        dgvEmployees.Columns.Add("Code", "Mã NV");
        dgvEmployees.Columns.Add("FullName", "Họ & Tên");
        dgvEmployees.Columns.Add("Phone", "Số Điện Thoại");
        dgvEmployees.Columns.Add("Role", "Chức Vụ / Quyền");
        dgvEmployees.Columns.Add("Shift", "Ca Trực Phân Công");
        dgvEmployees.Columns.Add("Department", "Phòng Ban");
        dgvEmployees.Columns.Add("Status", "Trạng Thái");
        tabEmployees.Controls.Add(dgvEmployees);

        // Grid Shifts
        dgvShifts = CreateStyledGrid();
        dgvShifts.Columns.Add("Code", "Mã Ca");
        dgvShifts.Columns.Add("Name", "Tên Ca Trực");
        dgvShifts.Columns.Add("StartTime", "Giờ Bắt Đầu");
        dgvShifts.Columns.Add("EndTime", "Giờ Kết Thúc");
        dgvShifts.Columns.Add("Break", "Thời Gian Nghỉ");
        dgvShifts.Columns.Add("Status", "Trạng Thái");
        tabShifts.Controls.Add(dgvShifts);

        // Grid Attendance
        dgvAttendance = CreateStyledGrid();
        dgvAttendance.Columns.Add("Code", "Mã NV");
        dgvAttendance.Columns.Add("Name", "Nhân Viên");
        dgvAttendance.Columns.Add("Shift", "Ca Trực");
        dgvAttendance.Columns.Add("CheckIn", "Giờ Check-in");
        dgvAttendance.Columns.Add("CheckOut", "Giờ Check-out");
        dgvAttendance.Columns.Add("WorkingHours", "Giờ Làm");
        dgvAttendance.Columns.Add("Late", "Đi Trễ (Phút)");
        dgvAttendance.Columns.Add("Status", "Trạng Thái");
        tabAttendance.Controls.Add(dgvAttendance);

        tabControl.TabPages.Add(tabEmployees);
        tabControl.TabPages.Add(tabShifts);
        tabControl.TabPages.Add(tabAttendance);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlHeader);
    }

    private DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView();
        ThemeManager.ApplyGridStyle(grid);
        return grid;
    }

    private async void LoadSampleData()
    {
        dgvEmployees.Rows.Clear();
        dgvShifts.Rows.Clear();
        dgvAttendance.Rows.Clear();

        using var client = new System.Net.Http.HttpClient();
        try
        {
            var response = await client.GetAsync("http://localhost:5137/api/employees");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("employees", out var empArray) && empArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var emp in empArray.EnumerateArray())
                        {
                            dgvEmployees.Rows.Add(
                                emp.GetProperty("employeeCode").GetString() ?? "",
                                emp.GetProperty("fullName").GetString() ?? "",
                                emp.GetProperty("phone").GetString() ?? "",
                                emp.GetProperty("role").GetString() ?? "",
                                emp.GetProperty("shiftName").GetString() ?? "",
                                emp.GetProperty("department").GetString() ?? "",
                                "🟢 " + (emp.GetProperty("status").GetString() ?? "Active")
                            );
                        }
                    }

                    if (data.TryGetProperty("shifts", out var shiftArray) && shiftArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var s in shiftArray.EnumerateArray())
                        {
                            dgvShifts.Rows.Add(
                                s.GetProperty("code").GetString() ?? "",
                                s.GetProperty("name").GetString() ?? "",
                                s.GetProperty("startTime").GetString() ?? "",
                                s.GetProperty("endTime").GetString() ?? "",
                                s.GetProperty("breakMinute").GetInt32() + " phút",
                                s.GetProperty("status").GetString() ?? "Active"
                            );
                        }
                    }

                    if (data.TryGetProperty("attendances", out var attArray) && attArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var att in attArray.EnumerateArray())
                        {
                            dgvAttendance.Rows.Add(
                                att.GetProperty("employeeCode").GetString() ?? "",
                                att.GetProperty("fullName").GetString() ?? "",
                                att.GetProperty("shiftName").GetString() ?? "",
                                att.GetProperty("checkIn").GetString() ?? "",
                                att.GetProperty("checkOut").GetString() ?? "",
                                att.GetProperty("workingHours").GetString() ?? "",
                                att.GetProperty("lateMinute").GetInt32() + " phút",
                                att.GetProperty("status").GetString() ?? "🟢 Đúng Giờ"
                            );
                        }
                    }
                    return;
                }
            }
        }
        catch
        {
            // API Offline Fallback
        }

        dgvEmployees.Rows.Add("NV-0001", "Quản trị viên Hệ thống", "0900000000", "Admin", "Ca Sáng (07:00-15:00)", "Quản Lý Siêu Thị", "🟢 Active");
        dgvEmployees.Rows.Add("NV-0002", "Trần Thị Thu Ngân", "0904445566", "Staff", "Ca Sáng (07:00-15:00)", "Thu Ngân POS", "🟢 Active");

        dgvShifts.Rows.Add("S1", "Ca Sáng (Morning)", "07:00:00", "15:00:00", "30 phút", "Active");
        dgvShifts.Rows.Add("S2", "Ca Chiều (Afternoon)", "15:00:00", "23:00:00", "30 phút", "Active");

        dgvAttendance.Rows.Add("NV-0002", "Trần Thị Thu Ngân", "Ca Sáng (S1)", "06:58:12", "15:02:10", "8.0 giờ", "0 phút", "🟢 Đúng Giờ");
    }
}
