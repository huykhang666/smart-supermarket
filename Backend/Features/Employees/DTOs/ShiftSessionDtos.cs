namespace SmartSupermarket.Backend.Features.Employees.DTOs;

public class OpenShiftSessionRequest
{
    public int EmployeeId { get; set; }
    public int? ShiftId { get; set; }
    public int BranchId { get; set; } = 1;
    public string WorkstationName { get; set; } = "POS-01";
    public string? IpAddress { get; set; }
    public decimal InitialCash { get; set; }
}

public class CloseShiftSessionRequest
{
    public int SessionId { get; set; }
    public decimal ActualCash { get; set; }
    public string? DiscrepancyReason { get; set; }
    public string? ManagerPin { get; set; }
    public string? ManagerUsername { get; set; }
    public string? ManagerPassword { get; set; }
    public string? ManagerNote { get; set; }
}

public class HeartbeatRequest
{
    public int UserId { get; set; }
    public string? WorkstationName { get; set; }
    public string? IpAddress { get; set; }
}

public class VerifyManagerPinRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class ShiftSessionResponse
{
    public int SessionId { get; set; }
    public int? ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string WorkstationName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    public decimal InitialCash { get; set; }
    public int TotalOrdersCount { get; set; }
    public decimal CashSalesAmount { get; set; }
    public decimal QrSalesAmount { get; set; }
    public decimal CardSalesAmount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string? DiscrepancyReason { get; set; }
    
    public string Status { get; set; } = "Open";
    public string? ManagerApprovedByName { get; set; }
    public string? ManagerNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OnlineWorkstationResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? WorkstationName { get; set; }
    public string? IpAddress { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int? ActiveSessionId { get; set; }
    public string? ActiveShiftName { get; set; }
    public decimal CurrentSessionRevenue { get; set; }
    public int CurrentSessionOrdersCount { get; set; }
}

public class CreateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Staff";
    public string Department { get; set; } = "Bán Hàng";
    public string? Shift { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public decimal Salary { get; set; } = 0;
}

public class ShiftOrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = "Khách Lẻ";
    public string PaymentMethodName { get; set; } = "Tiền mặt";
    public int PaymentMethod { get; set; } = 1;
    public decimal TotalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = "Completed";
}
