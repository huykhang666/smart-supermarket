using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int? BranchId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Giám sát trạm POS & Trạng thái Online của nhân viên
    public bool IsOnline { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public string? CurrentMachineName { get; set; }
    public string? CurrentIpAddress { get; set; }

    // Mã PIN 4-6 số để Quản lý (Manager/Admin) duyệt nhanh các thao tác nhạy cảm tại POS
    public string? ManagerPin { get; set; } = "123456";

    // Navigation property (1-1 relationship with Customer profile)
    public Customer? CustomerProfile { get; set; }
}
