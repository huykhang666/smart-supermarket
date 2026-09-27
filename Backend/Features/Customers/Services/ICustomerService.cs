using SmartSupermarket.Backend.Features.Customers.DTOs;

namespace SmartSupermarket.Backend.Features.Customers.Services;

public interface ICustomerService
{
    Task<CustomerResponseDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerLookupDto?> LookupByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerPagedResponse<CustomerListItemDto>> GetPagedAsync(
        string? keyword, byte? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerResponseDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerLoyaltyDto> GetLoyaltyPointsAsync(int id, CancellationToken cancellationToken = default);
    Task<PointHistoryListResponse> GetLoyaltyHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerPagedResponse<CustomerOrderHistoryItemDto>> GetOrderHistoryAsync(
        int id, int? branchId, byte? status, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<CustomerVoucherDto>> GetVouchersAsync(int id, string? status, CancellationToken cancellationToken = default);

    // Backward compatibility methods for Desktop POS
    Task<CustomerDto?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerDto> AddPointsAsync(int customerId, AddPointsRequest request, CancellationToken cancellationToken = default);
    Task<RedeemVoucherResponse> RedeemVoucherAsync(int customerId, RedeemVoucherRequest request, CancellationToken cancellationToken = default);
}
