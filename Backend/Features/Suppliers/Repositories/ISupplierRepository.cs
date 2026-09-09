using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Suppliers.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(string? search, byte? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> GetDropdownAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsNameAsync(string supplierName, int? excludeSupplierId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string supplierCode, int? excludeSupplierId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSupplier>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    void Update(Supplier supplier);
    void Delete(Supplier supplier);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
