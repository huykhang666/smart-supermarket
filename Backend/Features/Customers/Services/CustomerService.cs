using SmartSupermarket.Backend.Features.Customers.DTOs;
using SmartSupermarket.Backend.Features.Customers.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Repositories;

namespace SmartSupermarket.Backend.Features.Customers.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPromotionRepository _promotionRepository;

    private static readonly Dictionary<int, (int MinPoints, decimal VoucherValue, string Label)> _tiers = new()
    {
        { 500,   (500,   20000m,  "Voucher 20.000đ") },
        { 1000,  (1000,  50000m,  "Voucher 50.000đ") },
        { 2000,  (2000,  120000m, "Voucher 120.000đ") },
        { 5000,  (5000,  350000m, "Voucher 350.000đ") },
    };

    public CustomerService(ICustomerRepository customerRepository, IPromotionRepository promotionRepository)
    {
        _customerRepository = customerRepository;
        _promotionRepository = promotionRepository;
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByPhoneAsync(phone, cancellationToken);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerPagedResult> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _customerRepository.GetPagedAsync(search, page, pageSize, cancellationToken);
        return new CustomerPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto> AddPointsAsync(int customerId, AddPointsRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {customerId}");

        if (request.PointsToAdd <= 0)
            throw new ArgumentException("Số điểm cộng phải lớn hơn 0.");

        customer.LoyaltyPoints += request.PointsToAdd;
        customer.MembershipTier = CalculateTierLevel(customer.LoyaltyPoints);

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<RedeemVoucherResponse> RedeemVoucherAsync(int customerId, RedeemVoucherRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {customerId}");

        var tierEntry = _tiers
            .Where(t => request.PointsToRedeem >= t.Key)
            .OrderByDescending(t => t.Key)
            .Select(t => t.Value)
            .FirstOrDefault();

        if (tierEntry == default)
        {
            return new RedeemVoucherResponse
            {
                IsSuccess = false,
                Message = $"Cần ít nhất 500 điểm để đổi voucher. Bạn hiện có {customer.LoyaltyPoints} điểm.",
                RemainingPoints = customer.LoyaltyPoints
            };
        }

        if (customer.LoyaltyPoints < tierEntry.MinPoints)
        {
            return new RedeemVoucherResponse
            {
                IsSuccess = false,
                Message = $"Không đủ điểm. Cần {tierEntry.MinPoints} điểm, bạn có {customer.LoyaltyPoints} điểm.",
                RemainingPoints = customer.LoyaltyPoints
            };
        }

        customer.LoyaltyPoints -= tierEntry.MinPoints;
        customer.MembershipTier = CalculateTierLevel(customer.LoyaltyPoints);

        string voucherCode = $"LOYALTY-{customer.CustomerId:D4}-{DateTime.UtcNow:MMddHHmm}";

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return new RedeemVoucherResponse
        {
            IsSuccess = true,
            Message = $"Đổi điểm thành công! {tierEntry.Label} đã được tạo.",
            VoucherCode = voucherCode,
            VoucherValue = tierEntry.VoucherValue,
            RemainingPoints = customer.LoyaltyPoints
        };
    }

    private static int CalculateTierLevel(int points)
    {
        if (points >= 5000) return 3;
        if (points >= 2000) return 2;
        if (points >= 500)  return 1;
        return 0;
    }

    private static CustomerDto MapToDto(Domain.Entities.Customer c)
    {
        string tierLabel = c.MembershipTier switch
        {
            3 => "💎 Platinum VIP",
            2 => "👑 VIP Gold",
            1 => "🥈 Silver",
            _ => "🥉 Bronze"
        };

        return new CustomerDto
        {
            CustomerId     = c.CustomerId,
            CustomerCode   = $"KH-{c.CustomerId:D4}",
            UserId         = c.UserId,
            FullName       = c.User?.FullName ?? string.Empty,
            PhoneNumber    = c.User?.PhoneNumber,
            Email          = c.User?.Email,
            LoyaltyPoints  = c.LoyaltyPoints,
            MembershipTier = tierLabel,
            MembershipTierLevel = c.MembershipTier
        };
    }
}
