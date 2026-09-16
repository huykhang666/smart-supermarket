using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Customers.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
