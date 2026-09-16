using SmartSupermarket.Backend.Features.Customers.DTOs;

namespace SmartSupermarket.Backend.Features.Customers.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerPagedResult> GetPagedAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerDto> AddPointsAsync(int customerId, AddPointsRequest request, CancellationToken cancellationToken = default);
    Task<RedeemVoucherResponse> RedeemVoucherAsync(int customerId, RedeemVoucherRequest request, CancellationToken cancellationToken = default);
}
