using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Employees.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Features.Employees.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;

    public EmployeesController(AppDbContext context, PasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        // 1. Nạp danh sách nhân viên từ bảng Employees
        var employees = await _context.Employees
            .Include(e => e.User)
            .Include(e => e.Shift)
            .Select(e => new
            {
                e.EmployeeId,
                e.EmployeeCode,
                FullName = e.User.FullName,
                Phone = e.User.PhoneNumber ?? "0900000000",
                Role = e.User.Role.ToString(),
                ShiftName = e.Shift != null ? e.Shift.Name : "Ca Sáng (06:00-14:00)",
                e.Department,
                e.Status,
                IsOnline = e.User.IsOnline,
                CurrentMachine = e.User.CurrentMachineName ?? "N/A"
            })
            .ToListAsync();

        // Nạp thêm các tài khoản User (Staff/Manager/Admin) chưa có bản ghi Employee để hiển thị đủ
        var existingUserIds = await _context.Employees.Select(e => e.UserId).ToListAsync();
        var additionalUsers = await _context.Users
            .Where(u => !existingUserIds.Contains(u.UserId) && (u.Role == UserRole.Staff || u.Role == UserRole.Manager || u.Role == UserRole.Admin))
            .Select(u => new
            {
                EmployeeId = u.UserId,
                EmployeeCode = $"NV-{u.UserId:D4}",
                FullName = u.FullName,
                Phone = u.PhoneNumber ?? "0900000000",
                Role = u.Role.ToString(),
                ShiftName = "Ca Sáng (06:00-14:00)",
                Department = u.Role == UserRole.Manager ? "Quản Lý" : "Bán Hàng",
                Status = u.Status.ToString(),
                IsOnline = u.IsOnline,
                CurrentMachine = u.CurrentMachineName ?? "N/A"
            })
            .ToListAsync();

        var allEmployees = employees.Concat(additionalUsers).ToList();

        var shifts = await _context.Shifts.ToListAsync();
        var attendances = await _context.Attendances
            .Include(a => a.Employee)
                .ThenInclude(emp => emp.User)
            .Include(a => a.Shift)
            .Select(a => new
            {
                a.Employee.EmployeeCode,
                FullName = a.Employee.User.FullName,
                ShiftName = a.Shift != null ? a.Shift.Name : "Ca Sáng",
                CheckIn = a.CheckIn.ToString("HH:mm:ss"),
                CheckOut = a.CheckOut.HasValue ? a.CheckOut.Value.ToString("HH:mm:ss") : "--:--:--",
                WorkingHours = a.WorkingMinute > 0 ? (a.WorkingMinute / 60.0).ToString("F1") + " giờ" : "Đang làm",
                LateMinute = a.LateMinute,
                a.Status
            })
            .ToListAsync();

        return Ok(new { status = 200, data = new { employees = allEmployees, shifts, attendances } });
    }

    /// <summary>
    /// Tạo tài khoản nhân viên mới từ Quản lý / Admin
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResult<string>.Failure("Tên đăng nhập và mật khẩu không được để trống."));
        }

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(ApiResult<string>.Failure($"Tên đăng nhập '{request.Username}' đã tồn tại."));
        }

        var role = UserRole.Staff;
        if (string.Equals(request.Role, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.Manager;
        }

        // 1. Tạo bản ghi User
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.Phone,
            Email = $"{request.Username}@smartmarket.vn",
            Role = role,
            Status = UserStatus.Active,
            ManagerPin = "123456",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 2. Tìm ca làm việc nếu có chỉ định
        int? shiftId = null;
        if (!string.IsNullOrWhiteSpace(request.Shift))
        {
            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Code == request.Shift || s.Name.Contains(request.Shift));
            if (shift != null) shiftId = shift.ShiftId;
        }

        // 3. Tạo bản ghi Employee
        var employee = new Employee
        {
            UserId = user.UserId,
            EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode) ? $"NV-{user.UserId:D4}" : request.EmployeeCode,
            Department = string.IsNullOrWhiteSpace(request.Department) ? "Bán Hàng" : request.Department,
            ShiftId = shiftId,
            Salary = request.Salary > 0 ? request.Salary : 8000000m,
            Status = "Active",
            HireDate = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Ok(ApiResult<object>.Success(new
        {
            employeeId = employee.EmployeeId,
            employeeCode = employee.EmployeeCode,
            fullName = user.FullName,
            username = user.Username,
            role = user.Role.ToString()
        }, "Tạo tài khoản nhân viên thành công."));
    }
}
