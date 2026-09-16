namespace SmartSupermarket.Backend.Features.Customers.DTOs;

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
