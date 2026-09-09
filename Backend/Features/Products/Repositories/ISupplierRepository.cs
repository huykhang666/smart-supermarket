using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsPhoneOrEmailAsync(string? phoneNumber, string? email, int? excludeSupplierId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    void Update(Supplier supplier);
    void Delete(Supplier supplier);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
