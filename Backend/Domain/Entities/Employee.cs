namespace SmartSupermarket.Backend.Domain.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? DepartmentId { get; set; }
    public int? ShiftId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public string Department { get; set; } = "Bán Hàng";
    public string Status { get; set; } = "Active";

    // Navigation properties
    public User User { get; set; } = null!;
    public Shift? Shift { get; set; }
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
