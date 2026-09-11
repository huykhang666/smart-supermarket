using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Employees.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
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
                ShiftName = e.Shift != null ? e.Shift.Name : "Ca Sáng (07:00-15:00)",
                e.Department,
                e.Status
            })
            .ToListAsync();

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

        return Ok(new { status = 200, data = new { employees, shifts, attendances } });
    }
}
