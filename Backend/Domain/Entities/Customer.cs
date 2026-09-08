namespace SmartSupermarket.Backend.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public int UserId { get; set; }
    public int LoyaltyPoints { get; set; } = 0;
    public int MembershipTier { get; set; } = 1;

    // Navigation property (1-1 relationship back to User)
    public User User { get; set; } = null!;
}
