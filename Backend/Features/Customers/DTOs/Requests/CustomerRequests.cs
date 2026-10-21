using System;

namespace SmartSupermarket.Backend.Features.Customers.DTOs.Requests;

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

public class AddPointsRequest
{
    public int PointsToAdd { get; set; }
    public string? Note { get; set; }
}

public class RedeemVoucherRequest
{
    public int PointsToRedeem { get; set; }
}
