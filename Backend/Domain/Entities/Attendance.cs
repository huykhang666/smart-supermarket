namespace SmartSupermarket.Backend.Domain.Entities;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public int? ShiftId { get; set; }
    public DateTime CheckIn { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOut { get; set; }
    public int LateMinute { get; set; } = 0;
    public int WorkingMinute { get; set; } = 0;
    public string Status { get; set; } = "OnTime";

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Shift? Shift { get; set; }
}
