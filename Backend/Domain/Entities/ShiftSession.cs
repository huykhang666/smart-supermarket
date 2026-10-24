using System.ComponentModel.DataAnnotations;

namespace SmartSupermarket.Backend.Domain.Entities;

/// <summary>
/// Đại diện cho phiên làm việc ca và két tiền của nhân viên thu ngân (Cash Balancing & Shift Session)
/// </summary>
public class ShiftSession
{
    [Key]
    public int SessionId { get; set; }
    
    // Liên kết ca làm việc chuẩn (Ca sáng, Ca chiều...)
    public int? ShiftId { get; set; }
    
    // UserId của nhân viên thu ngân mở ca
    public int EmployeeId { get; set; }
    
    public int BranchId { get; set; } = 1;
    
    // Tên máy trạm POS / Workstation (VD: POS-QUAY-01, DESKTOP-CASHIER1)
    public string WorkstationName { get; set; } = string.Empty;
    
    public string? IpAddress { get; set; }
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    
    // ==========================================
    // SỐ LIỆU TÀI CHÍNH KÉT TIỀN (CASH BALANCING)
    // ==========================================
    
    // Tiền mặt bàn giao đầu ca (Floating cash)
    public decimal InitialCash { get; set; } = 0;
    
    // Tổng số đơn hàng xuất trong ca
    public int TotalOrdersCount { get; set; } = 0;
    
    // Doanh thu theo từng phương thức thanh toán
    public decimal CashSalesAmount { get; set; } = 0;
    public decimal QrSalesAmount { get; set; } = 0;
    public decimal CardSalesAmount { get; set; } = 0;
    public decimal TotalRevenue { get; set; } = 0;
    
    // Tiền mặt sổ sách lý thuyết = InitialCash + CashSalesAmount
    public decimal ExpectedCash { get; set; } = 0;
    
    // Tiền mặt thực tế thu ngân đếm được khi kết ca
    public decimal ActualCash { get; set; } = 0;
    
    // Chênh lệch = ActualCash - ExpectedCash (+: Thừa, -: Thiếu, 0: Khớp)
    public decimal DifferenceAmount { get; set; } = 0;
    
    // Lý do giải trình bắt buộc khi DifferenceAmount != 0
    public string? DiscrepancyReason { get; set; }
    
    // Trạng thái: "Open" (Đang trực ca), "Closed" (Đã chốt & bàn giao)
    public string Status { get; set; } = "Open";
    
    // Quản lý phê duyệt chốt ca & giải trình chênh lệch
    public int? ManagerApprovedByUserId { get; set; }
    public string? ManagerNote { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Shift? Shift { get; set; }
    public User? Employee { get; set; }
    public User? ManagerApprovedBy { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
