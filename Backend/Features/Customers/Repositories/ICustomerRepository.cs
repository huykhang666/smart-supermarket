using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Customers.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<Customer?> GetActiveByPhoneAsync(string phone, int? excludeCustomerId = null, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? keyword, byte? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<PointHistory>> GetPointHistoryAsync(int customerId, CancellationToken cancellationToken = default);
    Task AddPointHistoryAsync(PointHistory pointHistory, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetOrdersAsync(
        int customerId, int? branchId, byte? status, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Voucher>> GetVouchersAsync(int customerId, string? statusFilter, CancellationToken cancellationToken = default);
    Task AddVoucherAsync(Voucher voucher, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
