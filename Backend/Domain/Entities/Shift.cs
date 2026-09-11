namespace SmartSupermarket.Backend.Domain.Entities;

public class Shift
{
    public int ShiftId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakMinute { get; set; } = 30;
    public string Status { get; set; } = "Active";

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
