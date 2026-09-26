namespace SmartSupermarket.Backend.Features.Customers.DTOs;

public class CreateCustomerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public byte? Gender { get; set; }
    public string? Address { get; set; }
}

public class UpdateCustomerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public byte? Gender { get; set; }
    public string? Address { get; set; }
    public byte? Status { get; set; }
}

public class CustomerResponseDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public byte? Gender { get; set; }
    public string? Address { get; set; }
    public int LoyaltyPoints { get; set; }
    public byte Status { get; set; }
}

public class CustomerListItemDto
{
    public int CustomerId { get; set; }
    public string CustomerCode => $"KH-{CustomerId:D4}";
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int LoyaltyPoints { get; set; }
    public int Points => LoyaltyPoints;
    public byte Status { get; set; }
    public string Tier => LoyaltyPoints >= 5000 ? "💎 Platinum VIP"
        : LoyaltyPoints >= 2000 ? "👑 VIP Gold"
        : LoyaltyPoints >= 500 ? "🥈 Silver"
        : "🥉 Bronze";
    public int VouchersCount { get; set; } = 0;
    public string TotalSpent { get; set; } = "0";
}

public class CustomerPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public IEnumerable<T> Items { get; set; } = new List<T>();
}

public class CustomerLoyaltyDto
{
    public int CustomerId { get; set; }
    public int LoyaltyPoints { get; set; }
}

public class PointHistoryItemDto
{
    public int PointHistoryId { get; set; }
    public int? OrderId { get; set; }
    public int PointChange { get; set; }
    public byte Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PointHistoryListResponse
{
    public List<PointHistoryItemDto> Items { get; set; } = new();
}

public class CustomerOrderHistoryItemDto
{
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public byte Status { get; set; }
}

public class CustomerVoucherDto
{
    public int VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class CustomerLookupDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
    public byte Status { get; set; }
}

// ==========================================
// Backward compatibility DTOs for Desktop POS
// ==========================================
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int LoyaltyPoints { get; set; }
    public string MembershipTier { get; set; } = string.Empty;
    public int MembershipTierLevel { get; set; }
    public byte Status { get; set; } = 1;
}

public class CustomerPagedResult
{
    public IEnumerable<CustomerDto> Items { get; set; } = new List<CustomerDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AddPointsRequest
{
    public int PointsToAdd { get; set; }
    public string? Note { get; set; }
}

public class RedeemVoucherRequest
{
    public int PointsToRedeem { get; set; }
}

public class RedeemVoucherResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? VoucherCode { get; set; }
    public decimal VoucherValue { get; set; }
    public int RemainingPoints { get; set; }
}
