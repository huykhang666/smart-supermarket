namespace SmartSupermarket.Backend.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public int UserId { get; set; }
    public int LoyaltyPoints { get; set; } = 0;
    public int MembershipTier { get; set; } = 1;
    public byte? Gender { get; set; } // 1 = Nam, 2 = Nữ, 3 = Khác
    public string? Address { get; set; }
    public byte Status { get; set; } = 1; // 1 = ACTIVE, 2 = INACTIVE, 3 = BLOCKED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property (1-1 relationship back to User)
    public User User { get; set; } = null!;

    // Navigation properties for Loyalty, Vouchers & Orders
    public List<PointHistory> PointHistories { get; set; } = new();
    public List<Voucher> Vouchers { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}
