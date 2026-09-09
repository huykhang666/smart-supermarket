using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Suppliers.Repositories;

public interface IProductSupplierRepository
{
    Task<ProductSupplier?> GetLinkAsync(int productId, int supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSupplier>> GetSuppliersByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSupplier>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);
    Task AddLinkAsync(ProductSupplier productSupplier, CancellationToken cancellationToken = default);
    void UpdateLink(ProductSupplier productSupplier);
    void Unlink(ProductSupplier productSupplier);
    Task ResetOtherDefaultsAsync(int productId, int currentSupplierId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
