using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartSupermarket.POS.Services;

public class UserSession
{
    public int UserId { get; set; } = 1;
    public string Username { get; set; } = "admin";
    public string FullName { get; set; } = "Quản Trị Viên";
    public string Role { get; set; } = "Admin";
    public string? Token { get; set; }
}

public class AuthService
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    public static UserSession CurrentUser { get; private set; } = new();

    public async Task<(bool Success, string Message, string Role)> LoginAsync(string username, string password, string baseUrl = "http://localhost:5137")
    {
        username = username.Trim();
        password = password.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "");
        }

        // 1. Check quick mock accounts first
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
        {
            CurrentUser = new UserSession
            {
                UserId = 1,
                Username = "admin",
                FullName = "Quản Trị Viên Hệ Thống",
                Role = "Admin"
            };
            return (true, "Đăng nhập thành công với quyền Quản Trị Viên!", "Admin");
        }

        if ((username.Equals("nhanvien", StringComparison.OrdinalIgnoreCase) && password == "nhanvien123") ||
            (username.Equals("staff", StringComparison.OrdinalIgnoreCase) && password == "staff123"))
        {
            CurrentUser = new UserSession
            {
                UserId = 2,
                Username = username,
                FullName = "Nhân Viên Thu Ngân (POS)",
                Role = "Staff"
            };
            return (true, "Đăng nhập thành công với quyền Nhân Viên Thu Ngân!", "Staff");
        }

        // 2. Try calling Backend API
        try
        {
            var reqObj = new { username = username, password = password };
            var json = JsonSerializer.Serialize(reqObj);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync($"{baseUrl}/api/v1/auth/login", content);

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                int uid = data.TryGetProperty("userId", out var u) ? u.GetInt32() : 1;
                string uname = data.TryGetProperty("username", out var un) ? un.GetString() ?? username : username;
                string fname = data.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? uname : uname;
                string role = data.TryGetProperty("role", out var rl) ? rl.GetString() ?? "Staff" : "Staff";
                string token = data.TryGetProperty("token", out var tk) ? tk.GetString() ?? "" : "";

                CurrentUser = new UserSession
                {
                    UserId = uid,
                    Username = uname,
                    FullName = fname,
                    Role = role,
                    Token = token
                };

                return (true, $"Chào mừng {fname}!", role);
            }
        }
        catch { }

        return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", "");
    }
}
