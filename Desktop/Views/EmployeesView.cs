using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class EmployeesView : UserControl
{
    private TabControl tabControl = null!;
    private TabPage tabEmployees = null!;
    private TabPage tabLiveWorkstations = null!;
    private TabPage tabShifts = null!;
    private TabPage tabAttendance = null!;

    private DataGridView dgvEmployees = null!;
    private DataGridView dgvLiveWorkstations = null!;
    private DataGridView dgvShifts = null!;
    private DataGridView dgvAttendance = null!;
    private Button btnRefresh = null!;

    public EmployeesView()
    {
        InitializeComponent();
        _ = LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Nhân Viên, Ca Trực & Giám Sát Trạm POS Realtime",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 6),
            AutoSize = true
        };

        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnAddEmployee = new Button
        {
            Text = "➕ Cấp Tài Khoản Nhân Viên",
            Size = new Size(220, 32),
            Cursor = Cursors.Hand
        };
        AppTheme.ApplyPrimaryButton(btnAddEmployee);
        btnAddEmployee.Click += async (s, e) => {
            var form = new AddEmployeeForm(new HttpClient(), AppTheme.ApiBaseUrl);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await LoadSampleData(); // Reload grid
            }
        };

        btnRefresh = new Button
        {
            Text = "🔄 Làm Mới",
            Size = new Size(110, 32),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        AppTheme.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += async (s, e) => await LoadSampleData();

        pnlHeaderRight.Controls.Add(btnAddEmployee);
        pnlHeaderRight.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(pnlHeaderRight);

        // TabControl
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        tabLiveWorkstations = new TabPage("🖥️ Giám Sát Trạm POS & Nhân Viên Trực Tuyến");
        tabEmployees = new TabPage("📋 Danh Sách Nhân Viên");
        tabShifts = new TabPage("⏰ Quản Lý Ca Trực (Shift)");
        tabAttendance = new TabPage("⏱️ Bảng Chấm Công (Attendance)");

        // Grid 1: Live Workstations
        dgvLiveWorkstations = CreateStyledGrid();
        dgvLiveWorkstations.Columns.Add("Status", "Trạng Thái");
        dgvLiveWorkstations.Columns.Add("FullName", "Nhân Viên");
        dgvLiveWorkstations.Columns.Add("Role", "Chức Vụ");
        dgvLiveWorkstations.Columns.Add("Machine", "Trạm POS / Máy Tính");
        dgvLiveWorkstations.Columns.Add("IP", "Địa Chỉ IP");
        dgvLiveWorkstations.Columns.Add("LoginTime", "Giờ Đăng Nhập / Vào Ca");
        dgvLiveWorkstations.Columns.Add("ShiftName", "Ca Đang Trực");
        dgvLiveWorkstations.Columns.Add("OrdersCount", "Số Đơn Đã Bán");
        dgvLiveWorkstations.Columns.Add("Revenue", "Doanh Thu Ca Realtime");
        tabLiveWorkstations.Controls.Add(dgvLiveWorkstations);

        // Grid 2: Employees
        dgvEmployees = CreateStyledGrid();
        dgvEmployees.Columns.Add("Code", "Mã NV");
        dgvEmployees.Columns.Add("FullName", "Họ & Tên");
        dgvEmployees.Columns.Add("Phone", "Số Điện Thoại");
        dgvEmployees.Columns.Add("Role", "Chức Vụ / Quyền");
        dgvEmployees.Columns.Add("Shift", "Ca Trực Phân Công");
        dgvEmployees.Columns.Add("Department", "Phòng Ban");
        dgvEmployees.Columns.Add("Status", "Trạng Thái");
        tabEmployees.Controls.Add(dgvEmployees);

        // Grid 3: Shifts
        dgvShifts = CreateStyledGrid();
        dgvShifts.Columns.Add("Code", "Mã Ca");
        dgvShifts.Columns.Add("Name", "Tên Ca Trực");
        dgvShifts.Columns.Add("StartTime", "Giờ Bắt Đầu");
        dgvShifts.Columns.Add("EndTime", "Giờ Kết Thúc");
        dgvShifts.Columns.Add("Break", "Thời Gian Nghỉ");
        dgvShifts.Columns.Add("Status", "Trạng Thái");
        tabShifts.Controls.Add(dgvShifts);

        // Grid 4: Attendance
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

        tabControl.TabPages.Add(tabLiveWorkstations);
        tabControl.TabPages.Add(tabEmployees);
        tabControl.TabPages.Add(tabShifts);
        tabControl.TabPages.Add(tabAttendance);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlHeader);
    }

    private DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView();
        AppTheme.ApplyGridStyle(grid);
        return grid;
    }

    private async Task LoadSampleData()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            dgvEmployees.Rows.Clear();
            dgvShifts.Rows.Clear();
            dgvAttendance.Rows.Clear();
            dgvLiveWorkstations.Rows.Clear();

            // 1. Tải danh sách nhân viên, ca trực và chấm công
            var response = await client.GetAsync($"{AppTheme.ApiBaseUrl}/api/employees");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("employees", out var empArray) && empArray.ValueKind == JsonValueKind.Array)
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

                    if (data.TryGetProperty("shifts", out var shiftArray) && shiftArray.ValueKind == JsonValueKind.Array)
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

                    if (data.TryGetProperty("attendances", out var attArray) && attArray.ValueKind == JsonValueKind.Array)
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
                }
            }

            // 2. Tải danh sách trạm POS & Nhân viên Online Realtime
            var wsRes = await client.GetAsync($"{AppTheme.ApiBaseUrl}/api/v1/shifts/sessions/online-workstations?branchId=1");
            if (wsRes.IsSuccessStatusCode)
            {
                var wsJson = await wsRes.Content.ReadAsStringAsync();
                using var wsDoc = JsonDocument.Parse(wsJson);
                if (wsDoc.RootElement.TryGetProperty("data", out var wsArray) && wsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ws in wsArray.EnumerateArray())
                    {
                        bool isOnline = ws.GetProperty("isOnline").GetBoolean();
                        string status = isOnline ? "🟢 ĐANG BÁN HÀNG" : "⚪ OFFLINE";
                        string name = ws.GetProperty("fullName").GetString() ?? "";
                        string role = ws.GetProperty("role").GetString() ?? "";
                        string machine = ws.GetProperty("workstationName").GetString() ?? "N/A";
                        string ip = ws.TryGetProperty("ipAddress", out var ipP) && ipP.ValueKind == JsonValueKind.String ? ipP.GetString() ?? "127.0.0.1" : "127.0.0.1";
                        string loginTime = ws.TryGetProperty("lastLoginAt", out var llP) && llP.ValueKind == JsonValueKind.String 
                            ? (DateTime.TryParse(llP.GetString(), out var dt) ? dt.ToLocalTime().ToString("HH:mm:ss dd/MM") : "--:--") 
                            : "--:--";
                        string shiftName = ws.TryGetProperty("activeShiftName", out var sNP) && sNP.ValueKind == JsonValueKind.String ? sNP.GetString() ?? "Chưa mở ca" : "Chưa mở ca";
                        int ordCount = ws.GetProperty("currentSessionOrdersCount").GetInt32();
                        decimal rev = ws.GetProperty("currentSessionRevenue").GetDecimal();

                        dgvLiveWorkstations.Rows.Add(
                            status,
                            name,
                            role,
                            machine,
                            ip,
                            loginTime,
                            shiftName,
                            $"{ordCount} hóa đơn",
                            $"{rev:N0} VNĐ"
                        );
                    }
                }
            }
        }
        catch
        {
            // API Offline Fallback
        }
    }
}
